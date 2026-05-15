using Google.Cloud.Firestore;

namespace NutriFoodAPI.Data
{
    public class FirestoreContext
    {
        /// <summary>
        /// A propriedade 'Database' será usada pelo FirestoreService para comandos CRUD
        /// </summary>
        public FirestoreDb Database { get; private set; }

        public FirestoreContext(string idProjeto, string caminhoConfig)
        {
            try
            {
                /// Caminho para o arquivo JSON                
                string caminhoCompleto = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, caminhoConfig);

                /// Verifica se o arquivo existe para evitar erros de autenticação
                if (!File.Exists(caminhoCompleto))
                {
                    throw new
                        FileNotFoundException($"Arquivo de credenciais não encontrado em: {caminhoCompleto}");
                }

                /// Configura a variável exigida pelo SDK do Google
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", caminhoCompleto);

                /// Inicializa a conexão com o Firestore
                Database = FirestoreDb.Create(idProjeto);
            }
            catch (Exception ex)
            {
                /// Log de erro básico para facilitar o debug no servidor
                throw new Exception($"Erro ao inicializar FirestoreContext: {ex.Message}");
            }
        }
    }
}