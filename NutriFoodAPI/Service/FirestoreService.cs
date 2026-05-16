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

        /// <summary>
        /// Obtém todos os alimentos validados cadastrados na coleção do Firestore.
        /// </summary>
        public async Task<List<AlimentoValidado>> ObterTodos()
        {
            try
            {
                // Consulta a coleção "AlimentosValidados" no Firestore
                Query colecao = _contexto.Database.Collection("AlimentosValidados");
                QuerySnapshot snapshot = await colecao.GetSnapshotAsync();

                List<AlimentoValidado> listaAlimentos = new List<AlimentoValidado>();

                // Percorre os documentos retornados e converte cada um para o modelo AlimentoValidado
                foreach (DocumentSnapshot documento in snapshot.Documents)
                {
                    if (documento.Exists)
                    {
                        AlimentoValidado alimento = documento.ConvertTo<AlimentoValidado>();
                        listaAlimentos.Add(alimento);
                    }
                }

                return listaAlimentos;
            }
            catch (Exception ex)
            {
                throw new
                    Exception($"Erro ao buscar lista de alimentos no Firestore: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca um alimento validado no Firestore pelo seu ID Sequencial.
        /// </summary>
        public async Task<AlimentoValidado?> ObterPorId(string id)
        {
            try
            {
                // Aponta diretamente para o documento específico dentro da coleção
                DocumentReference docRef = _contexto.Database
                    .Collection("AlimentosValidados")
                    .Document(id);

                // Busca o snapshot do documento de forma assíncrona
                DocumentSnapshot snapshot = await 
                    docRef.GetSnapshotAsync();

                // Se o documento não existir no Firestore,
                // retorna null para a Controller tratar como 404
                if (!snapshot.Exists)
                    return null;

                // Converte os campos do documento para a classe AlimentoValidado
                return snapshot.ConvertTo<AlimentoValidado>();
            }
            catch (Exception ex)
            {                
                throw new 
                    Exception($"Erro ao buscar o ID {id} no Firestore: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca alimentos validados no Firestore cujo nome seja igual ou contenha 
        /// o termo pesquisado (Case-Insensitive aproximado).
        /// </summary>
        public async Task<List<AlimentoValidado>> ObterAlimentosPorNome(string nome)
        {
            try
            {
                // Acessa a coleção no Firestore
                CollectionReference colecao = _contexto.Database.Collection("AlimentosValidados");

                // Faz uma busca exata pelo nome cadastrado no banco e traz os resultados idênticos               
                Query consulta = colecao.WhereEqualTo("Nome", nome);
                QuerySnapshot snapshot = await
                    consulta.GetSnapshotAsync();

                List<AlimentoValidado> listaAlimentos = new List<AlimentoValidado>();

                foreach (DocumentSnapshot documento in snapshot.Documents)
                {
                    if (documento.Exists)
                    {
                        // Converte o documento do Firestore para a nossa model
                        var alimento = documento.ConvertTo<AlimentoValidado>();
                        listaAlimentos.Add(alimento);
                    }
                }

                // Se não achar por busca exata, podemos fazer uma busca por filtro: "Começa com"
                // Isso ajuda caso o usuário digite "Arroz" e queira achar "Arroz Integral" ou "Arroz Branco"
                if (listaAlimentos.Count == 0)
                {
                    // O '\uf8ff' faz o Firestore entender que queremos tudo que começa com aquele texto
                    Query consultaAproximada = colecao
                        .WhereGreaterThanOrEqualTo("Nome", nome)
                        .WhereLessThanOrEqualTo("Nome", nome + "\uf8ff");

                    QuerySnapshot snapshotAproximado = await consultaAproximada.GetSnapshotAsync();

                    foreach (DocumentSnapshot documento in snapshotAproximado.Documents)
                    {
                        if (documento.Exists)
                        {
                            var alimento = documento.ConvertTo<AlimentoValidado>();
                            listaAlimentos.Add(alimento);
                        }
                    }
                }

                return listaAlimentos;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao buscar alimento por nome no Firestore: {ex.Message}");
            }
        }

        /// <summary>
        /// Exclui um alimento validado do Firestore utilizando o ID Sequencial.
        /// Retorna true se a exclusão foi feita ou false se o documento não existia.
        /// </summary>
        public async Task<bool> ExcluirAlimento(string id)
        {
            try
            {
                // Aponta para o documento específico dentro da coleção
                DocumentReference documentoRef = _contexto.Database
                    .Collection("AlimentosValidados")
                    .Document(id);

                // Busca o snapshot para garantir que o documento realmente existe antes de apagar
                // Isso evita que tentemos deletar um documento que não existe, o que poderia
                // causar confusão
                DocumentSnapshot snapshot = await documentoRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return false; // Retorna falso para avisar o Controller que o ID não foi achado

                // Deleta o documento do Firestore
                await documentoRef.DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new 
                    Exception($"Erro ao excluir alimento no Firestore: {ex.Message}");
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