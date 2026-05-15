using Google.Cloud.Firestore;
using NutriFoodAPI.Data;
using NutriFoodAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NutriFoodAPI.Service
{
    public class FirestoreService
    {
        private readonly FirestoreContext _contexto;
        private readonly HttpClient _http;
        private readonly string _chaveApi;

        public FirestoreService(FirestoreContext contexto, HttpClient http, IConfiguration config)
        {
            _contexto = contexto;
            _http = http;
            _chaveApi = config["ApiConfigs:NinjaApiKey"];
        }

        public async Task<AlimentoValidado?> SalvarAlimentoValidado(AlimentoValidado alimento)
        {
            try
            {
                // 1. Validação Externa
                var dadosOficiais = await ConsultarApiNutricional(alimento.Nome);
                if (dadosOficiais == null) return null;

                // 2. ID Sequencial Humano
                int novoId = await GerarProximoIdSequencial();

                // 3. Enriquecimento
                alimento.Id = novoId.ToString();
                alimento.Calorias = dadosOficiais.Calorias;
                alimento.DataValidacao = DateTime.UtcNow;

                // 4. Persistência
                await PersistirNoFirestore(alimento);

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

        private async Task<AlimentoValidado?> ConsultarApiNutricional(string nome)
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("X-Api-Key", _chaveApi);
            var response = await _http.GetAsync($"https://api.api-ninjas.com/v1/nutrition?query={nome}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AlimentoValidado>>(json)?.FirstOrDefault();
        }

        private async Task PersistirNoFirestore(AlimentoValidado alimento)
        {
            await _contexto.Database
                .Collection("AlimentosValidados")
                .Document(alimento.Id)
                .SetAsync(alimento);
        }
    }
}