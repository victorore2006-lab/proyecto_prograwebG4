using Google.Cloud.Firestore;

namespace ProyectoQ3Backend.Models;

[FirestoreData]
public class AppUser
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty]
    public string DisplayName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Username { get; set; } = string.Empty;

    [FirestoreProperty]
    public string PhoneNumber { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime BirthDate { get; set; }

    [FirestoreProperty]
    public string Country { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Bio { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Role { get; set; } = "Usuario";

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;
}
