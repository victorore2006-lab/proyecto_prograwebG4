using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProyectoQ3Backend.DTOs;
using ProyectoQ3Backend.Models;

namespace ProyectoQ3Backend.Services;

public class AuthService
{
    private const string AuthBaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts";
    private readonly HttpClient _httpClient;
    private readonly FirebaseService _firebaseService;
    private readonly string _apiKey;

    public AuthService(
        HttpClient httpClient,
        FirebaseService firebaseService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _firebaseService = firebaseService;
        _apiKey = configuration["Firebase:ApiKey"]
            ?? throw new InvalidOperationException("No se configuró Firebase:ApiKey.");

        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "API_KEY")
        {
            throw new InvalidOperationException(
                "Configura Firebase:ApiKey con la Web API Key del proyecto Firebase.");
        }
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        ValidateProfile(
            dto.DisplayName,
            dto.Username,
            dto.PhoneNumber,
            dto.BirthDate,
            dto.Country,
            dto.Bio);

        var authResult = await SendAuthRequestAsync(
            "signUp",
            new
            {
                email = dto.Email.Trim().ToLowerInvariant(),
                password = dto.Password,
                returnSecureToken = true
            });

        var user = new AppUser
        {
            Id = authResult.LocalId,
            Email = authResult.Email,
            DisplayName = dto.DisplayName.Trim(),
            Username = dto.Username.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            BirthDate = ToUtc(dto.BirthDate!.Value),
            Country = dto.Country.Trim(),
            Bio = dto.Bio.Trim(),
            Role = "Usuario",
            CreatedAt = DateTime.UtcNow,
            UserId = authResult.LocalId
        };

        try
        {
            await _firebaseService
                .GetCollection("users")
                .Document(authResult.LocalId)
                .CreateAsync(user);
        }
        catch
        {
            await DeleteAccountWithTokenAsync(authResult.IdToken);
            throw;
        }

        return authResult;
    }

    public Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        return SendAuthRequestAsync(
            "signInWithPassword",
            new
            {
                email = dto.Email.Trim().ToLowerInvariant(),
                password = dto.Password,
                returnSecureToken = true
            });
    }

    private async Task<AuthResponseDto> SendAuthRequestAsync(string action, object payload)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{AuthBaseUrl}:{action}?key={Uri.EscapeDataString(_apiKey)}",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await GetFirebaseErrorAsync(response));
        }

        var result = await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>()
            ?? throw new InvalidOperationException("Firebase devolvió una respuesta vacía.");

        return new AuthResponseDto
        {
            IdToken = result.IdToken,
            LocalId = result.LocalId,
            Email = result.Email
        };
    }

    private async Task DeleteAccountWithTokenAsync(string idToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{AuthBaseUrl}:delete?key={Uri.EscapeDataString(_apiKey)}",
            new { idToken });
    }

    private static async Task<string> GetFirebaseErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        try
        {
            using var document = JsonDocument.Parse(json);
            var code = document.RootElement
                .GetProperty("error")
                .GetProperty("message")
                .GetString();

            return code switch
            {
                "EMAIL_EXISTS" => "Ya existe un usuario con ese correo.",
                "EMAIL_NOT_FOUND" => "Credenciales inválidas.",
                "INVALID_LOGIN_CREDENTIALS" => "Credenciales inválidas.",
                "INVALID_PASSWORD" => "Credenciales inválidas.",
                "USER_DISABLED" => "La cuenta está deshabilitada.",
                "OPERATION_NOT_ALLOWED" => "Habilita Email/Password en Firebase Authentication.",
                "WEAK_PASSWORD : Password should be at least 6 characters" =>
                    "La contraseña debe tener al menos 6 caracteres.",
                _ => $"Firebase Authentication rechazó la solicitud: {code ?? "error desconocido"}."
            };
        }
        catch (JsonException)
        {
            return $"Firebase Authentication respondió con el estado {(int)response.StatusCode}.";
        }
    }

    private static void ValidateProfile(
        string displayName,
        string username,
        string phoneNumber,
        DateTime? birthDate,
        string country,
        string bio)
    {
        if (string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(country) ||
            string.IsNullOrWhiteSpace(bio) ||
            birthDate is null)
        {
            throw new InvalidOperationException("Todos los campos del perfil son obligatorios.");
        }

        if (birthDate.Value.Date > DateTime.UtcNow.Date)
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

    private sealed class FirebaseAuthResponse
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
