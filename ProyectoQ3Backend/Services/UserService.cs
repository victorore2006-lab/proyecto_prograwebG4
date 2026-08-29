using Google.Cloud.Firestore;
using ProyectoQ3Backend.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProyectoQ3Backend.Services;

public class UserService
{
    private readonly FirebaseService _firebaseService;

    public UserService(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    public async Task UpdateAsync(string userId, UpdateUserDto dto)
    {
        ValidateText(dto.DisplayName, nameof(dto.DisplayName));
        ValidateText(dto.Username, nameof(dto.Username));
        ValidateText(dto.PhoneNumber, nameof(dto.PhoneNumber));
        ValidateText(dto.Country, nameof(dto.Country));
        ValidateText(dto.Bio, nameof(dto.Bio));

        if (dto.BirthDate is null)
        {
            throw new InvalidOperationException("BirthDate es obligatorio.");
        }

        ValidateBirthDate(dto.BirthDate.Value);

        var document = await GetExistingDocumentAsync(userId);
        await document.UpdateAsync(new Dictionary<string, object>
        {
            ["DisplayName"] = dto.DisplayName.Trim(),
            ["Username"] = dto.Username.Trim(),
            ["PhoneNumber"] = dto.PhoneNumber.Trim(),
            ["BirthDate"] = ToUtc(dto.BirthDate.Value),
            ["Country"] = dto.Country.Trim(),
            ["Bio"] = dto.Bio.Trim()
        });
    }

    public async Task PatchAsync(string userId, PatchUserDto dto)
    {
        var updates = new Dictionary<string, object>();

        AddTextUpdate(updates, "DisplayName", dto.DisplayName);
        AddTextUpdate(updates, "Username", dto.Username);
        AddTextUpdate(updates, "PhoneNumber", dto.PhoneNumber);
        AddTextUpdate(updates, "Country", dto.Country);
        AddTextUpdate(updates, "Bio", dto.Bio);

        if (dto.BirthDate is not null)
        {
            ValidateBirthDate(dto.BirthDate.Value);
            updates["BirthDate"] = ToUtc(dto.BirthDate.Value);
        }

        if (updates.Count == 0)
        {
            throw new InvalidOperationException("Debes enviar al menos un campo para actualizar.");
        }

        var document = await GetExistingDocumentAsync(userId);
        await document.UpdateAsync(updates);
    }

    public async Task DeleteAsync(string userId)
    {
        var document = await GetExistingDocumentAsync(userId);

        await _firebaseService.Auth.DeleteUserAsync(userId);
        await document.DeleteAsync();
    }

    private async Task<DocumentReference> GetExistingDocumentAsync(string userId)
    {
        var document = _firebaseService.GetCollection("users").Document(userId);
        var snapshot = await document.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            throw new KeyNotFoundException("No existe el perfil del usuario autenticado.");
        }

        return document;
    }

    private static void AddTextUpdate(
        IDictionary<string, object> updates,
        string field,
        string? value)
    {
        if (value is null)
        {
            return;
        }

        ValidateText(value, field);
        updates[field] = value.Trim();
    }

    private static void ValidateText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{field} no puede estar vacío.");
        }
    }

    private static void ValidateBirthDate(DateTime birthDate)
    {
        if (birthDate.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException("BirthDate no puede estar en el futuro.");
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