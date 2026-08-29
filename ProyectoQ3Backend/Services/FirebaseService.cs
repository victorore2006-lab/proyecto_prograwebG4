using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

namespace ProyectoQ3Backend.Services;

public class FirebaseService
{
    private readonly FirestoreDb _firestoreDb;
    public FirebaseAuth Auth { get; }

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

        var firebaseApp = FirebaseApp.Create(new AppOptions
        {
            Credential = credential,
            ProjectId = projectId
        });

        Auth = FirebaseAuth.GetAuth(firebaseApp);

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
