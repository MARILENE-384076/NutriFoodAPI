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
                /// Busca os dados do appsettings.json                 
                string idProjeto = configuration["FirebaseConfig:ProjectId"];
                string nomeArquivo = configuration["FirebaseConfig:JsonPath"];

                /// Monta o caminho considerando a pasta 'config_API'
                string caminhoCompleto = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "config_API", nomeArquivo);

                /// Validações para garantir que as configurações estão corretas
                if (string.IsNullOrEmpty(nomeArquivo))
                {
                    throw new 
                        Exception("A chave 'JsonPath' não foi encontrada no appsettings.json.");
                }

                if (!File.Exists(caminhoCompleto))
                {
                    throw new 
                        FileNotFoundException($"Arquivo de credenciais não encontrado no caminho: " +
                        $"{caminhoCompleto}");
                }

                /// Define a variável de ambiente para o SDK do Google Cloud, apontando para o arquivo JSON
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", caminhoCompleto);

                /// Inicializa a conexão com o Firestore usando o ID do projeto
                Database = FirestoreDb.Create(idProjeto);
            }
            
            catch (Exception ex)
            {
                
                throw new 
                    Exception($"Erro Crítico ao inicializar FirestoreContext: {ex.Message}");
            }
        }
    }
}