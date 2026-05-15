using NutriFoodAPI.Data;
using NutriFoodAPI.Service;

/// Cria o construtor da aplicação
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FirestoreContext>();

/// Configuração do HttpClient para o FirestoreService,
/// permitindo injeção de dependência
builder.Services.AddHttpClient<FirestoreService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();