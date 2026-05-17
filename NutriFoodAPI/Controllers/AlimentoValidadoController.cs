using Google.Api;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NutriFoodAPI.Models;
using NutriFoodAPI.Service;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NutriFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlimentoValidadoController : ControllerBase
    {
        private readonly FirestoreService _firestoreService;

        //Declaração do logger para rastreabilidade e monitoramento de erros
        private readonly ILogger<AlimentoValidadoController> _logger;

        // Injeção de dependências do FirestoreService e do Logger
        public AlimentoValidadoController(
            FirestoreService firestoreService,
            ILogger<AlimentoValidadoController> logger)
        {
            _firestoreService = firestoreService;
            _logger = logger;
        }

        /// <summary>
        /// Recebe os dados da Squad 1, delega a validação ao Service e persiste no Firebase.
        /// </summary>
        /// <remarks>
        /// Envia o nome do alimento para uma API nutricional externa, mescla com as regras de negócio 
        /// e salva o registro com um ID sequencial seguro no Firestore.        
        /// </remarks>
        /// <param name="alimentoExterno">Objeto contendo o nome do alimento a ser consultado e validado.</param>
        /// <response code="201">Alimento validado e cadastrado com sucesso no banco de dados.</response>
        /// <response code="400">Dados de requisição inválidos ou malformados.</response>
        /// <response code="404">O alimento informado não foi encontrado na API nutricional externa.</response>
        /// <response code="502">A API nutricional externa (API-Ninjas) está fora do ar ou indisponível.</response>
        /// <response code="500">Erro interno no servidor ou falha de comunicação com o Firestore.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Post([FromBody] AlimentoConsultaExterna alimentoExterno)
        {
            try
            {
                if (alimentoExterno == null || string.IsNullOrWhiteSpace(alimentoExterno.Name))
                {
                    _logger.LogWarning("Tentativa de cadastro com dados ou nome inválidos/vazios.");
                    return
                        BadRequest("O nome do alimento é obrigatório e não pode estar vazio.");
                }

                _logger.LogInformation("Iniciando validação e persistência do alimento:" +
                    " {NomeAlimento}", alimentoExterno.Name);

                // Passa o objeto enviado pela Squad 1 para o Service
                var resultado = await _firestoreService.SalvarAlimentoValidado(alimentoExterno);

                // Verifica se o alimento não foi encontrado na API Ninjas
                if (resultado == null)
                {
                    _logger.LogWarning("Alimento '{NomeAlimento}' " +
                        "não foi localizado na API externa.", alimentoExterno.Name);
                    return NotFound(new
                    {
                        mensagem = $"O alimento '{alimentoExterno.Name}' não foi localizado na base nutricional oficial."
                    });
                }

                _logger.LogInformation("Alimento '{NomeAlimento}' salvo com sucesso " +
                    "com o ID Sequencial: {IdSequencial}", resultado.Nome, resultado.Id);

                // Retorna o alimento com ID Sequencial gerado pelo Service
                return CreatedAtAction(nameof(Post), new { id = resultado.Id }, new
                {
                    mensagem = "Alimento validado e salvo com sucesso!",
                    idSequencial = resultado.Id,
                    dados = resultado
                });
            }
            catch (HttpRequestException)
            {
                _logger.LogError( "Falha de rede ou timeout ao consultar a API externa " +
                    "para o alimento: {NomeAlimento}", alimentoExterno?.Name);

                return StatusCode(502,
                    "O serviço de validação nutricional externo está temporariamente indisponível.");
            }
            catch (Exception)
            {
                _logger.LogError("Erro crítico no endpoint POST ao processar " +
                    "o alimento: {NomeAlimento}", alimentoExterno?.Name);

                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao processar e salvar o alimento. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Obtém a lista completa de todos os alimentos validados.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza uma busca segura no Firestore. Se não houver registros, 
        /// retorna uma lista vazia.
        /// </remarks>
        /// <response code="200">Retorna a lista de alimentos validados com sucesso
        /// podendo ser um array vazio [].</response>
        /// <response code="500">Erro interno no servidor ao tentar acessar o banco de dados.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<AlimentoValidado>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("Solicitando a listagem completa de alimentos validados.");

                var alimentos = await _firestoreService.ObterTodos();

                _logger.LogInformation("Listagem realizada com sucesso. " +
                    "Total de registros: {Quantidade}", alimentos?.Count ?? 0);

                return Ok(alimentos);
            }
            catch (Exception)
            {
                _logger.LogError("Erro crítico no endpoint GET ao listar todos os alimentos.");
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao processar a listagem dos alimentos. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }
        /// <summary>
        /// Obtém um alimento validado específico através do seu ID Sequencial.
        /// </summary>
        /// <remarks>
        /// Realiza uma busca direta no Firestore utilizando o ID fornecido na URL.
        /// </remarks>
        /// <param name="id">ID sequencial do alimento armazenado no banco de dados.</param>
        /// <response code="200">Alimento encontrado com sucesso.</response>
        /// <response code="404">Nenhum alimento foi encontrado com o ID informado.</response>
        /// <response code="500">Erro interno no servidor ao tentar acessar o banco de dados.</response
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AlimentoValidado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AlimentoValidado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                _logger.LogInformation("Buscando alimento específico pelo ID: {IdAlimento}", id);
                var alimento = await _firestoreService.ObterPorId(id);

                ///Se o objeto específico não existe no banco, retorna 404
                
                if (alimento == null)
                {
                    _logger.LogWarning("Alimento com ID '{IdAlimento}' não foi localizado no Firestore.", id);
                    return NotFound(new
                    {
                        mensagem = $"Alimento com o ID '{id}' não foi encontrado no sistema."
                    });
                }

                _logger.LogInformation("Alimento com ID '{IdAlimento}' retornado com sucesso.", id);
                return Ok(alimento);
            }
            catch (Exception)
            {
                _logger.LogError("Erro crítico no endpoint GET por ID ao buscar o identificador: {IdAlimento}", id);
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao processar a busca do alimento. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }
        /// <summary>
        /// Obtém um alimento validado específico através do seu nome exato.
        /// </summary>
        /// <remarks>
        /// Realiza uma busca filtrada na coleção do Firestore utilizando o nome fornecido na URL.
        /// </remarks>
        /// <param name="nome">Nome exato do alimento armazenado no banco de dados.</param>
        /// <response code="200">Alimento encontrado com sucesso.</response>
        /// <response code="404">Nenhum alimento foi encontrado com o nome informado.</response>
        /// <response code="500">Erro interno no servidor ao tentar acessar o banco de dados.</response>
        [HttpGet("buscar-por-nome/{nome}")]
        [ProducesResponseType(typeof(AlimentoValidado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByName(string nome)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nome))
                {
                    _logger.LogWarning("Tentativa de busca por nome com parâmetro vazio.");
                    return
                       BadRequest("O nome para busca não pode estar vazio.");
                }

                // Remove espaços extras nas pontas para evitar erros de digitação
                var termoBusca = nome.Trim();
                _logger.LogInformation("Iniciando busca de alimentos contendo o termo: '{Termo}'", termoBusca);

                var alimentos = await
                            _firestoreService.ObterAlimentosPorNome(termoBusca);

                // Se a lista voltar vazia, retorna 404 informando que nada foi achado no banco
                if (alimentos == null || alimentos.Count == 0)
                {
                    _logger.LogWarning("Nenhum registro correspondente ao termo '{Termo}' foi achado.", termoBusca);
                    return NotFound(new
                    {
                        mensagem = $"Nenhum alimento contendo '{termoBusca}' " +
                    $"foi encontrado no histórico do banco."
                    });
                }

                _logger.LogInformation("Busca por nome finalizada com sucesso. Encontrados {Quantidade} registros.", alimentos.Count);
                return Ok(alimentos);
            }
            catch (Exception)
            {
                _logger.LogError("Erro crítico no endpoint GET por Nome ao buscar o termo: '{Termo}'", nome);
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao processar a busca do alimento pelo nome. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Atualiza as informações de um alimento validado existente.
        /// </summary>
        /// <remarks>
        /// Substitui integralmente os dados do documento correspondente ao ID informado na URL no Firestore.
        /// </remarks>
        /// <param name="id">ID sequencial do alimento que será atualizado.</param>
        /// <param name="alimentoAtualizado">Objeto contendo os novos dados do alimento.</param>
        /// <response code="200">Alimento atualizado com sucesso.</response>
        /// <response code="400">ID da URL não condiz com o ID do corpo da requisição ou dados malformados.</response>
        /// <response code="404">Nenhum alimento foi encontrado com o ID informado para atualização.</response>
        /// <response code="500">Erro interno no servidor ao tentar atualizar os dados no banco.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Put(string id, [FromBody] AlimentoValidado alimentoAtualizado)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || alimentoAtualizado == null)
                {
                    _logger.LogWarning("Tentativa de atualização com parâmetros nulos ou inválidos.");
                    return BadRequest("Os dados da requisição e o ID são obrigatórios.");
                }

                // Garante que o ID da URL seja o mesmo ID do objeto sendo salvo
                if (id != alimentoAtualizado.Id)
                {
                    _logger.LogWarning("Divergência de IDs identificada. " +
                        "URL: '{IdUrl}', Body: '{IdBody}'", id, alimentoAtualizado.Id);
                    return 
                        BadRequest("O ID fornecido na URL não corresponde ao " +
                        "ID do objeto enviado no corpo da requisição.");
                }

                _logger.LogInformation("Iniciando processo de atualização para o alimento " +
                    "ID: {IdAlimento}", id);

                // Delega a atualização para o Service, que irá verificar a existência do ID
                // e realizar a substituição dos dados
                bool atualizadoComSucesso = await 
                    _firestoreService.AtualizarAlimento(alimentoAtualizado);

                if (!atualizadoComSucesso)
                {
                    _logger.LogWarning("Falha ao atualizar. Alimento ID '{IdAlimento}' " +
                        "não existe na base de dados.", id);
                    return NotFound(new
                    {
                        mensagem = $"Não foi possível atualizar. Alimento com ID '{id}' " +
                        $"não foi localizado no sistema."
                    });
                }

                _logger.LogInformation("Alimento ID '{IdAlimento}' atualizado com sucesso" +
                    " no Firestore.", id);
                return Ok(new
                {
                    mensagem = $"Alimento com ID '{id}' foi atualizado com sucesso!",
                    dados = alimentoAtualizado
                });
            }
            catch (Exception)
            {
                _logger.LogError("Erro crítico no endpoint PUT ao tentar atualizar" +
                    " o ID: {IdAlimento}", id);
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao tentar atualizar o alimento. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Remove um alimento validado do sistema através do seu ID Sequencial.
        /// </summary>
        /// <param name="id">O ID Sequencial do alimento a ser removido.</param>
        /// <returns>Retorna uma mensagem de sucesso ou o erro correspondente.</returns>
        /// <response code="200">Alimento removido com sucesso do banco de dados.</response>
        /// <response code="400">O ID fornecido é inválido, nulo ou vazio.</response>
        /// <response code="404">O ID do alimento não foi localizado no sistema.</response>
        /// <response code="500">Erro interno no servidor ao processar a exclusão de forma segura.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("O ID fornecido é inválido ou vazio.");
                }

                _logger.LogInformation("Iniciando processo de remoção do alimento ID: {IdAlimento}", id);
                // Delega a exclusão para o Service
                bool excluidoComSucesso = await _firestoreService.ExcluirAlimento(id);

                // Se o Service retornar falso, significa que o ID não existe no banco
                if (!excluidoComSucesso)
                {
                    _logger.LogWarning("Falha ao excluir. Alimento ID '{IdAlimento}' não existe na base.", id);
                    return NotFound(new
                    {
                        mensagem = $"Não foi possível excluir." +
                    $" Alimento com ID '{id}' não foi localizado no sistema."
                    });
                }

                _logger.LogInformation("Alimento ID '{IdAlimento}' removido com sucesso do Firestore.", id);
                // Retorna sucesso, confirmando a remoção
                return Ok(new
                {
                    mensagem = $"Alimento com ID '{id}' foi removido " +
                    $"com sucesso do banco de dados!"
                });
            }
            catch (Exception)
            {
                _logger.LogError("Erro crítico no endpoint DELETE ao tentar apagar o ID: {IdAlimento}", id);
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao tentar excluir o alimento. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }
    }
}