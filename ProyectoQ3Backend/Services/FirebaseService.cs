using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

namespace ProyectoQ3Backend.Services;

public class FirebaseService
{
    private readonly FirestoreDb _firestoreDb;

    public FirebaseService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var projectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("No se configuró Firebase:ProjectId.");
        var configuredPath = configuration["Firebase:CredentialsPath"]
            ?? throw new InvalidOperationException("No se configuró Firebase:CredentialsPath.");

        var credentialPath = Path.GetFullPath(
            configuredPath,
            environment.ContentRootPath);

        if (!File.Exists(credentialPath))
        {
            throw new FileNotFoundException(
                $"No se encontró el archivo de credenciales de Firebase: {credentialPath}",
                credentialPath);
        }

        var credential = CredentialFactory
            .FromFile<ServiceAccountCredential>(credentialPath)
            .ToGoogleCredential();

        _firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = projectId,
            Credential = credential
        }.Build();
    }

    public CollectionReference GetCollection(string collectionName)
    {
        return _firestoreDb.Collection(collectionName);
    }
}
