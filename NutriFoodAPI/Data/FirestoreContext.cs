using Google.Apis.Auth.OAuth2; // Adicionado para usar o GoogleCredential
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Google.Apis.Auth.OAuth2;

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
                string idProjeto = configuration["FirebaseConfig:ProjectId"]
                    ?? throw new Exception("A chave 'ProjectId' não foi encontrada no appsettings.json.");

                string nomeArquivo = configuration["FirebaseConfig:JsonPath"];

                /// Monta o caminho considerando a pasta 'config_API'
                string caminhoCompleto = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "config_API", nomeArquivo);

                /// Validações para garantir que as configurações estão corretas
                if (string.IsNullOrEmpty(nomeArquivo))
                {
                    throw new Exception("A chave 'JsonPath' não foi encontrada no appsettings.json.");
                }

                if (!File.Exists(caminhoCompleto))
                {
                    throw new 
                        FileNotFoundException($"Arquivo de credenciais do Firebase não encontrado: {caminhoCompleto}");
                }

                var credential = GoogleCredential.FromFile(caminhoCompleto);

                Database = new FirestoreDbBuilder
                {
                    ProjectId = idProjeto,
                    Credential = credential
                }.Build();
              }
            catch (Exception ex)
            {
                throw new Exception($"Erro Crítico ao inicializar FirestoreContext: {ex.Message}");
            }
        }
    }
}