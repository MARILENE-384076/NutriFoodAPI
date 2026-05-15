using Google.Cloud.Firestore;
using NutriFoodAPI.Data;
using NutriFoodAPI.Models;
using System;
using System.Threading.Tasks;

namespace NutriFoodAPI.Service
{
    public class FirestoreService
    {
        private readonly FirestoreContext _conexaoBanco;    

        /// <summary>
        /// Inicializa uma nova instância da classe FirestoreService utilizando 
        /// o contexto do Firestore especificado.
        /// </summary>
        /// <param name="conexaoBanco">A instância de FirestoreContext usada 
        /// para operações no banco de dados. Não pode ser nula.</param>
        public FirestoreService(FirestoreContext conexaoBanco)
        {
            _conexaoBanco = conexaoBanco;
        }

        /// <summary>
        /// Salva de forma assíncrona um idtem do AlimentoValidado no banco de dados Firestore.
        /// </summary>
        /// <param name="alimento">O item de alimento validado a ser persistido. Não pode ser nulo. 
        /// A propriedade Id do item é usada como o identificador do documento.</param>
        /// <returns>Uma tarefa que representa a operação de salvamento assíncrona.</returns>
        /// <exception cref="Exception">Lançada se ocorrer um erro ao tentar salvar o item no Firestore.
        /// </exception>
        public async Task SalvarAlimentoValidado(AlimentoValidado alimento)
        {
            try
            {
                // Acessa a coleção "AlimentosValidados" usando a instância do Database
                // Se a coleção não existir, o Firestore cria.
                DocumentReference documento = _conexaoBanco.Database
                    .Collection("AlimentosValidados")
                    .Document(alimento.Id);

                await documento.SetAsync(alimento);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar no Firestore: {ex.Message}");
            }
        }
    }
}