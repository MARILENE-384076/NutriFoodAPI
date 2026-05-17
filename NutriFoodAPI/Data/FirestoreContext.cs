using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace NutriFoodAPI.Data
{
    public class FirestoreContext
    {
        /// <summary>
        /// Propriedade usada pelo FirestoreService para realizar as operações no banco.
        /// </summary>
        public FirestoreDb Database { get; private set; }

        public FirestoreContext(IConfiguration configuration)
        {
            try
            {
                // Busca os dados do appsettings.json                 
                string idProjeto = configuration["FirebaseConfig:ProjectId"]
                    ?? throw new 
                    Exception("A chave 'ProjectId' não foi encontrada no appsettings.json.");

                string nomeArquivo = configuration["FirebaseConfig:JsonPath"]
                    ?? throw new 
                    Exception("A chave 'JsonPath' não foi encontrada no appsettings.json.");

                // Mapeamento dinâmico compatível com o IIS do MonsterASP
                string baseDirectory = AppContext.BaseDirectory;
                string caminhoCompleto = Path.Combine(baseDirectory, "config_API", nomeArquivo);
                
                if (!File.Exists(caminhoCompleto))
                {
                    caminhoCompleto = Path.Combine(Directory.GetCurrentDirectory(), 
                        "config_API", nomeArquivo);
                }

                // Validação do arquivo de credenciais
                if (!File.Exists(caminhoCompleto))
                {
                    throw new 
                        FileNotFoundException($"Arquivo de credenciais do Firebase não encontrado no " +
                        $"caminho verificado: {caminhoCompleto}");
                }

                // Lê o conteúdo do arquivo JSON e passa direto na memória.
                
                string jsonConteudo = File.ReadAllText(caminhoCompleto);

                // Inicialização segura do FirestoreDb injetando as credenciais diretamente
                Database = new FirestoreDbBuilder
                {
                    ProjectId = idProjeto,
                    JsonCredentials = jsonConteudo
                }.Build();
            }
            catch (Exception ex)
            {
                throw new 
                    Exception($"Erro Crítico ao inicializar FirestoreContext: {ex.Message}", ex);
            }
        }
    }
}