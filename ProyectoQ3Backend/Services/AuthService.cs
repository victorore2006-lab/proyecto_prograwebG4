using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FirebaseAdmin.Auth;
using Microsoft.IdentityModel.Tokens;
using ProyectoQ3Backend.DTOs;
using ProyectoQ3Backend.Models;

namespace ProyectoQ3Backend.Services;

public class AuthService
{
    private const int PasswordIterations = 100_000;
    private readonly FirebaseService _firebaseService;
    private readonly IConfiguration _configuration;

    public AuthService(FirebaseService firebaseService, IConfiguration configuration)
    {
        _firebaseService = firebaseService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        ValidateProfile(dto);
        var email = dto.Email.Trim().ToLowerInvariant();
        UserRecord firebaseUser;

        try
        {
            firebaseUser = await _firebaseService.Auth.CreateUserAsync(new UserRecordArgs
            {
                Email = email,
                Password = dto.Password,
                DisplayName = dto.DisplayName.Trim()
            });
        }
        catch (FirebaseAuthException exception)
        {
            throw new InvalidOperationException(
                $"Firebase Authentication no pudo crear el usuario: {exception.Message}",
                exception);
        }

        var user = new AppUser
        {
            Id = firebaseUser.Uid,
            Email = email,
            DisplayName = dto.DisplayName.Trim(),
            Username = dto.Username.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            BirthDate = ToUtc(dto.BirthDate!.Value),
            Country = dto.Country.Trim(),
            Bio = dto.Bio.Trim(),
            Role = "Usuario",
            CreatedAt = DateTime.UtcNow,
            UserId = firebaseUser.Uid
        };

        try
        {
            await _firebaseService.GetCollection("users")
                .Document(user.Id)
                .CreateAsync(new Dictionary<string, object>
                {
                    ["Id"] = user.Id,
                    ["Email"] = user.Email,
                    ["DisplayName"] = user.DisplayName,
                    ["Username"] = user.Username,
                    ["PhoneNumber"] = user.PhoneNumber,
                    ["BirthDate"] = user.BirthDate,
                    ["Country"] = user.Country,
                    ["Bio"] = user.Bio,
                    ["Role"] = user.Role,
                    ["CreatedAt"] = user.CreatedAt,
                    ["UserId"] = user.UserId,
                    ["PasswordHash"] = HashPassword(dto.Password)
                });
        }
        catch
        {
            await _firebaseService.Auth.DeleteUserAsync(firebaseUser.Uid);
            throw;
        }

        return CreateAuthResponse(user.Id, user.Email, user.Role);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var snapshot = await _firebaseService.GetCollection("users")
            .WhereEqualTo("Email", email)
            .Limit(1)
            .GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            snapshot = await _firebaseService.GetCollection("user")
                .WhereEqualTo("Email", dto.Email.Trim())
                .Limit(1)
                .GetSnapshotAsync();
        }

        if (snapshot.Count == 0)
        {
            throw new InvalidOperationException("Credenciales inválidas.");
        }

        var data = snapshot.Documents[0].ToDictionary();
        var passwordHash = GetRequiredValue(data, "PasswordHash");

        if (!VerifyPassword(dto.Password, passwordHash))
        {
            throw new InvalidOperationException("Credenciales inválidas.");
        }

        var userId = data.TryGetValue("UserId", out var storedUserId)
            ? storedUserId.ToString()!
            : GetRequiredValue(data, "Id");
        var storedEmail = GetRequiredValue(data, "Email");
        var role = data.TryGetValue("Role", out var storedRole)
            ? storedRole.ToString()!
            : "Usuario";

        return CreateAuthResponse(userId, storedEmail, role);
    }

    private AuthResponseDto CreateAuthResponse(string userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("user_id", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("No se configuró Jwt:Key.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            IdToken = new JwtSecurityTokenHandler().WriteToken(token),
            LocalId = userId,
            Email = email
        };
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            32);

        return $"{PasswordIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');

        if (parts.Length == 3 && int.TryParse(parts[0], out var iterations))
        {
            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var expectedHash = Convert.FromBase64String(parts[2]);
                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        var legacyHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacyHash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static string GetRequiredValue(
        IReadOnlyDictionary<string, object> data,
        string field)
    {
        if (!data.TryGetValue(field, out var value) || value is null)
        {
            throw new InvalidOperationException($"El perfil no contiene el campo {field}.");
        }

        return value.ToString()!;
    }

    private static void ValidateProfile(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName) ||
            string.IsNullOrWhiteSpace(dto.Username) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
            string.IsNullOrWhiteSpace(dto.Country) ||
            string.IsNullOrWhiteSpace(dto.Bio) ||
            dto.BirthDate is null)
        {
            throw new InvalidOperationException("Todos los campos del perfil son obligatorios.");
        }

        if (dto.BirthDate.Value.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException("La fecha de nacimiento no puede estar en el futuro.");
        }
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
