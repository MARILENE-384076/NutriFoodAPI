using Google.Cloud.Firestore;
using NutriFoodAPI.Data;
using NutriFoodAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NutriFoodAPI.Service
{
    public class FirestoreService
    {
        private readonly FirestoreContext _contexto;
        private readonly HttpClient _http;
        private readonly string _chaveApi;

        public FirestoreService(FirestoreContext contexto, HttpClient http)
        {
            _contexto = contexto;
            _http = http;

            // Configuração para ler a chave direto da pasta config_API
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("config_API/ninja-api-Key.json")
                .Build();

            _chaveApi = config["ApiKey"] ?? throw new Exception("Chave da API Ninjas não encontrada no arquivo config_API/ninja-api-Key.json");
        }

        public async Task<AlimentoValidado?> SalvarAlimentoValidado(AlimentoValidado alimento)
        {
            try
            {
                // 1. Validação Externa (API Ninjas)
                // DICA: No Swagger, envie o nome em INGLÊS (ex: "rice") para obter resultados
                var dadosOficiais = await ConsultarApiNutricional(alimento.Nome);

                if (dadosOficiais == null) return null;

                // 2. Gerar ID Sequencial (Lógica de transação no Firestore)
                int novoId = await GerarProximoIdSequencial();

                // 3. Enriquecimento/Mapeamento dos dados
                alimento.Id = novoId.ToString();
                alimento.Nome = dadosOficiais.Nome; // Garante o nome correto retornado pela API
                alimento.Calorias = dadosOficiais.Calorias;
                alimento.GorduraTotal = dadosOficiais.GorduraTotal;
                alimento.Proteina = dadosOficiais.Proteina;
                alimento.Carboidratos = dadosOficiais.Carboidratos;
                alimento.Fibras = dadosOficiais.Fibras;
                alimento.Acucar = dadosOficiais.Acucar;
                alimento.PorcaoGramas = dadosOficiais.PorcaoGramas;
                alimento.DataValidacao = DateTime.UtcNow;

                // 4. Persistência
                await SalvarNoFirestore(alimento);

                return alimento;
            }
            catch (Exception ex)
            {
                // Captura erros de rede ou de permissão do banco
                throw new Exception($"Erro NutriFoodAPI: {ex.Message}");
            }
        }

        private async Task<int> GerarProximoIdSequencial()
        {
            DocumentReference contadorRef = _contexto.Database
                .Collection("configuracoes")
                .Document("contador_alimentos");

            return await _contexto.Database.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(contadorRef);

                int idAtual = snapshot.Exists && snapshot.TryGetValue("ultimoId", out int id) ? id : 0;
                int proximoId = idAtual + 1;

                transaction.Set(contadorRef, new { ultimoId = proximoId }, SetOptions.MergeAll);

                return proximoId;
            });
        }

        private async Task<AlimentoValidado?> ConsultarApiNutricional(string nome)
        {
            /// Configurações de cabeçalho para autenticação na API Ninjas
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("X-Api-Key", _chaveApi);

            /// Consulta à API Ninjas usando o nome do alimento
            var response = await 
                _http.GetAsync($"https://api.api-ninjas.com/v1/nutrition?query={nome}");

            if (!response.IsSuccessStatusCode) 
                return null;

            /// A API Ninjas retorna uma lista, mesmo que haja apenas um resultado.
            /// Por isso, deserializamos para uma lista e pegamos o primeiro item.
            var json = await response.Content.ReadAsStringAsync();

            /// Configurações para deserialização           
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                /// Permite ler números mesmo que estejam em formato string 
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            /// A API Ninjas retorna uma lista de alimentos.
            var lista = JsonSerializer.Deserialize<List<AlimentoValidado>>(json, options);

            return lista?.FirstOrDefault();
        }

        private async Task SalvarNoFirestore(AlimentoValidado alimento)
        {
            await _contexto.Database
                .Collection("AlimentosValidados")
                .Document(alimento.Id)
                .SetAsync(alimento);
        }
    }
}