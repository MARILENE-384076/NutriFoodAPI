using Google.Cloud.Firestore;
using NutriFoodAPI.Data;
using NutriFoodAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFoodAPI.Service
{
    public class FirestoreService
    {
        private readonly FirestoreContext _contexto;
        private readonly HttpClient _http;
        private readonly string _chaveApi;

        public FirestoreService(FirestoreContext contexto, HttpClient http, IConfiguration configuration)
        {
            _contexto = contexto;
            _http = http;

            _chaveApi = configuration["ApiConfigs:NinjaApiKey"] ?? throw new
                Exception("Chave 'NinjaApiKey' não encontrada no bloco 'ApiConfigs' do appsettings.json.");

            _http.DefaultRequestHeaders.Add("X-Api-Key", _chaveApi);
        }

        /// <summary>
        /// Recebe os dados externos, consulta a API oficial, mescla as informações e salva.
        /// </summary>
        public async Task<AlimentoValidado?> SalvarAlimentoValidado(AlimentoConsultaExterna alimentoExterno)
        {
            try
            {
                // Consulta à API externa para obter os dados oficiais do alimento
                var dadosOficiais = await ConsultarApiNutricional(alimentoExterno.Name);

                if (dadosOficiais == null)
                    return null;

                // Transação para obter o ID Sequencial do Firestore
                int novoId = await GerarProximoIdSequencial();

                // Cria o objeto final convertendo os JsonElements de forma segura em um só lugar
                var alimento = new AlimentoValidado
                {
                    Id = novoId.ToString(),
                    Nome = dadosOficiais.Name, 
                    Calorias = ConverterSeguro(dadosOficiais.Calories),
                    PorcaoGramas = ConverterSeguro(dadosOficiais.ServingSizeG),
                    GorduraTotal = ConverterSeguro(dadosOficiais.FatTotalG),
                    GorduraSaturada = ConverterSeguro(dadosOficiais.FatSaturatedG),
                    Proteina = ConverterSeguro(dadosOficiais.ProteinG),
                    Sodio = ConverterSeguro(dadosOficiais.SodiumMg),
                    Potassio = ConverterSeguro(dadosOficiais.PotassiumMg),
                    Colesterol = ConverterSeguro(dadosOficiais.CholesterolMg),
                    Carboidratos = ConverterSeguro(dadosOficiais.CarbohydratesTotalG),
                    Fibras = ConverterSeguro(dadosOficiais.FiberG),
                    Acucar = ConverterSeguro(dadosOficiais.SugarG),
                    DataValidacao = DateTime.UtcNow
                };

                await SalvarNoFirestore(alimento);

                return alimento;
            }
            catch (Exception ex)
            {
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

        private async Task<AlimentoConsultaExterna?> ConsultarApiNutricional(string nome)
        {
            var response = await 
                _http.GetAsync($"https://api.api-ninjas.com/v1/nutrition?query={Uri.EscapeDataString(nome)}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var lista = JsonSerializer.Deserialize<List<AlimentoConsultaExterna>>(json, options);

            return lista?.FirstOrDefault();
        }

        private async Task SalvarNoFirestore(AlimentoValidado alimento)
        {
            await _contexto.Database
                .Collection("AlimentosValidados")
                .Document(alimento.Id)
                .SetAsync(alimento);
        }

        /// <summary>
        /// Método auxiliar para converter JsonElement de forma segura, tratando casos de string e número.
        /// </summary>
        private double ConverterSeguro(JsonElement elemento)
        {
            if (elemento.ValueKind == JsonValueKind.Number)
                return elemento.GetDouble();

            if (elemento.ValueKind == JsonValueKind.String && double.TryParse(elemento.GetString(), out double resultado))
                return resultado;

            return 0; 
        }
    }
}