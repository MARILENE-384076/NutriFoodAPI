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

            // CORREÇÃO: Buscando a chave dentro do bloco "ApiConfigs" -> "NinjaApiKey"
            _chaveApi = configuration["ApiConfigs:NinjaApiKey"] ?? throw new
                Exception("Chave 'NinjaApiKey' não encontrada no bloco 'ApiConfigs' do appsettings.json.");

            _http.DefaultRequestHeaders.Add("X-Api-Key", _chaveApi);
        }

        public async Task<AlimentoValidado?> SalvarAlimentoValidado(AlimentoValidado alimento)
        {
            try
            {
                var dadosOficiais = await ConsultarApiNutricional(alimento.Nome);

                if (dadosOficiais == null)
                    return null;

                int novoId = await GerarProximoIdSequencial();

                alimento.Id = novoId.ToString();
                alimento.Nome = dadosOficiais.Nome;
                alimento.Calorias = dadosOficiais.Calorias;
                alimento.GorduraTotal = dadosOficiais.GorduraTotal;
                alimento.Proteina = dadosOficiais.Proteina;
                alimento.Carboidratos = dadosOficiais.Carboidratos;
                alimento.Fibras = dadosOficiais.Fibras;
                alimento.Acucar = dadosOficiais.Acucar;
                alimento.PorcaoGramas = dadosOficiais.PorcaoGramas;
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

                transaction.Set(contadorRef, new { ultimoId = proximoId }, SetOptions.MergeAll);

                return proximoId;
            });
        }

        private async Task<AlimentoValidado?> ConsultarApiNutricional(string nome)
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