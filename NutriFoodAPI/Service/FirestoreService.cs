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

        /// <summary>
        /// Construtor corrigido para mapear a estrutura exata do seu appsettings.json
        /// </summary>
        public FirestoreService(FirestoreContext contexto, HttpClient http, IConfiguration configuration)
        {
            _contexto = contexto;
            _http = http;

            // Buscando a chave dentro do bloco "ApiConfigs" -> "NinjaApiKey"
            _chaveApi = configuration["ApiConfigs:NinjaApiKey"] ?? throw new
                Exception("Chave 'NinjaApiKey' não encontrada no bloco 'ApiConfigs' do appsettings.json.");

            _http.DefaultRequestHeaders.Add("X-Api-Key", _chaveApi);
        }

        public async Task<AlimentoValidado?> SalvarAlimentoValidado(AlimentoValidado alimento)
        {
            try
            {
                // Consulta à API externa para obter os dados oficiais do alimento
                var dadosOficiais = await ConsultarApiNutricional(alimento.Nome);

                if (dadosOficiais == null)
                    return null;

                // Transação para obter o ID Sequencial do Firestore
                int novoId = await GerarProximoIdSequencial();

                // Convertendo os JsonElements em double
                alimento.Id = novoId.ToString();
                alimento.Nome = dadosOficiais.Name; // Mapeia string
                alimento.Calorias = ConverterSeguro(dadosOficiais.Calories);
                alimento.PorcaoGramas = ConverterSeguro(dadosOficiais.ServingSizeG);
                alimento.GorduraTotal = ConverterSeguro(dadosOficiais.FatTotalG);
                alimento.GorduraSaturada = ConverterSeguro(dadosOficiais.FatSaturatedG);
                alimento.Proteina = ConverterSeguro(dadosOficiais.ProteinG);
                alimento.Sodio = ConverterSeguro(dadosOficiais.SodiumMg);
                alimento.Potassio = ConverterSeguro(dadosOficiais.PotassiumMg);
                alimento.Colesterol = ConverterSeguro(dadosOficiais.CholesterolMg);
                alimento.Carboidratos = ConverterSeguro(dadosOficiais.CarbohydratesTotalG);
                alimento.Fibras = ConverterSeguro(dadosOficiais.FiberG);
                alimento.Acucar = ConverterSeguro(dadosOficiais.SugarG);
                alimento.DataValidacao = DateTime.UtcNow;

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

                transaction.Set(contadorRef, new 
                { ultimoId = proximoId }, SetOptions.MergeAll);

                return proximoId;
            });
        }

        /// <summary>
        /// Altera o retorno para 'AlimentoConsultaExterna' para aceitar os tipos dinâmicos da API externa
        /// </summary>
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

            /// A API retorna uma lista, mas como estamos consultando por um alimento específico,
            /// pegamos o primeiro resultado (ou null se a lista estiver vazia)
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
        /// Tratamento humano para converter com total segurança JsonElement em double
        /// </summary>
        private double ConverterSeguro(JsonElement elemento)
        {
            if (elemento.ValueKind == JsonValueKind.Number)
                return elemento.GetDouble();

            if (elemento.ValueKind == JsonValueKind.String && double.TryParse(elemento.GetString(), out double resultado))
                return resultado;

            return 0; // Se nulo ou formato inadequado.
        }
    }
}