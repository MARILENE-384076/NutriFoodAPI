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

// Registro do Firestore como Singleton para garantir uma única instância durante toda a aplicação
builder.Services.AddSingleton<FirestoreContext>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<FirestoreService>();

var app = builder.Build();

// CORREÇÃO 1: Swagger configurado com o prefixo padrão (/swagger) para evitar conflitos no IIS
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Validação Nutricional de alimentos v1");
});

// CORREÇÃO 2: Redirecionamento amigável. Se entrares na raiz do site, ele manda-te para o Swagger
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseCors("PermitirTudo");

// CORREÇÃO 3: Comentado temporariamente. O proxy reverso do MonsterASP gerencia o HTTPS. 
// Deixar esta linha ativa em servidores gratuitos costuma derrubar a conexão (Connection Reset).
// app.UseHttpsRedirection(); 

app.UseAuthorization();
app.MapControllers();

app.Run();