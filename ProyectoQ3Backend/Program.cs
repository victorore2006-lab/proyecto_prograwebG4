using Microsoft.AspNetCore.Authentication;
using ProyectoQ3Backend.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddHttpClient<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<NoteService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = FirebaseAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = FirebaseAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
        FirebaseAuthenticationHandler.SchemeName,
        _ => { });
    
    builder.Services.AddAuthorization();

    builder.Services.AddCors(options => 
    {
      options.AddPolicy("AllowAll", policy =>
        {
          policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
        });
     });  
   
    var app = builder.Build();
    
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("UserHub API")
                .AddPreferredSecuritySchemes(["Bearer"])
                .AddHttpAuthentication("Bearer", bearer =>
                {
                    bearer.Token = "";
                });
        });
    }
    app.UseCors("AllowAll");

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
