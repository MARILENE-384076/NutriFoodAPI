using NutriFoodAPI.Data;
using NutriFoodAPI.Service;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Configuração de CORS para permitir a conexão com o front-end.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTudo",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuração para a documentação Swagger.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Validação Nutricional",
        Version = "v1",
        Description = "API para validação nutricional integrada ao Firestore."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

/// Registro do Firestore como Singleton para garantir uma única instância durante toda a aplicação
builder.Services.AddSingleton<FirestoreContext>();


builder.Services.AddHttpClient();
builder.Services.AddHttpClient<FirestoreService>(); 

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Validação Nutricional de alimentos v1");
});

app.UseCors("PermitirTudo");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();