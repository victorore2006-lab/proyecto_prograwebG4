using ProyectoQ3Backend.DTOs;
using ProyectoQ3Backend.Models;

namespace ProyectoQ3Backend.Services;

public class NoteService
{
    private readonly FirebaseService _firebaseService;

    public NoteService(FirebaseService firebaseService)
    {
       _firebaseService = firebaseService; 
    }

    public async Task<Note> Create(NoteDto dto, string userId)
    {
        
        // Armando la nota
        var note = new Note
        {
            Id = Guid.NewGuid().ToString(),
            Title = dto.Title,
            Content =  dto.Content,
            Tag = dto.Tag,
            UserId = userId,
            CreatedAt = DateTime.UtcNow 
        };
        
        // Guardando la nota
        await _firebaseService.GetCollection("notes")
            .Document(note.Id)
            .SetAsync(new Dictionary<string, object>
            {
                { "taId", note.Id },
                { "Title", note.Title },
                { "Content", note.Content },
                { "Tag", note.Tag },
                { "UserId", note.UserId },
                { "CreatedAt", note.CreatedAt }
            });
        return note;
    }

    public async Task<List<Note>> GetByUser(string userId)
    {
        var snapshot = await _firebaseService.GetCollection("notes")
            .WhereEqualTo("UserId", userId)
            .GetSnapshotAsync();

        var notes = new List<Note>();

        foreach (var doc in snapshot.Documents)
        {
            var data = doc.ToDictionary();
            
            notes.Add(new Note
            {
                Id = data["Id"].ToString()!,
                Title = data["Title"].ToString()!,
                Content = data["Content"].ToString()!,
                Tag = data["Tag"].ToString()!,
                UserId = data["UserId"].ToString()!,
                CreatedAt = ((Google.Cloud.Firestore.Timestamp)data["CreatedAt"]).ToDateTime()
            });
        }
        return notes;
    }
}