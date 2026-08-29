using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Cloud.Firestore;
using Microsoft.IdentityModel.Tokens;
using ProyectoQ3Backend.DTOs;
using ProyectoQ3Backend.Models;

namespace ProyectoQ3Backend.Services;

public class AuthService
{
    private readonly FirebaseService _firebaseService;
    private readonly IConfiguration _configuration;
    
    public AuthService(FirebaseService firebaseService, IConfiguration configuration)
    {
        _firebaseService = firebaseService;
        _configuration = configuration;
    }
    
    public async Task<User> Register(RegisterDto registerDto)
    {
        // Buscar correo similar
        var collection = _firebaseService.GetCollection("user");
        var existing = await collection
            .WhereEqualTo("Email", registerDto.Email)
            .GetSnapshotAsync();
        
        // Validar que no exista alguien con el mismo correo
        if (existing.Count > 0)
            throw new Exception("Ya existe un usuario con ese correo");
        
        // Crear un objeto
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            FullName = registerDto.FullName,
            Email =  registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            Role = "user",
            CreatedAt =  DateTime.UtcNow
        };
        
        // Guardar en Firestore
        // Dictionary o Firebase Properties
        
        await collection.Document(user.Id).SetAsync(new Dictionary<string, object>
        {
            { "Id", user.Id },
            { "FullName", user.FullName },
            { "Email", user.Email },
            { "PasswordHash", user.PasswordHash },
            { "Role", user.Role },
            { "CreatedAt", user.CreatedAt },
            
        });
        return user;
    }

    public async Task<string> Login(LoginDto loginDto)
    {
        var collection = _firebaseService.GetCollection("user");
        var snapshot = await collection
            .WhereEqualTo("Email", loginDto.Email)
            .GetSnapshotAsync();
        
        if (snapshot.Count == 0)
            throw new Exception("Credenciales son invalidas");

        var doc = snapshot.Documents[0];
        var data = doc.ToDictionary();

        var user = new User
        {
            Id = data["Id"].ToString()!,
            FullName = data["FullName"].ToString()!,
            Email = data["Email"].ToString()!,
            PasswordHash = data["PasswordHash"].ToString()!,
            Role = data["Role"].ToString()!,
            CreatedAt = ((Google.Cloud.Firestore.Timestamp)data["CreatedAt"]).ToDateTime()

        };
        
        if(!VerifyPasswordHash(loginDto.Password, user.PasswordHash))
            throw new Exception("Credenciales Invalidas");
        
        return GenerateToken(user);
        
    }

    private string GenerateToken(User user)
    {
        // Claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );
        
        var creds = new SigningCredentials(
            key, 
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPasswordHash(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}