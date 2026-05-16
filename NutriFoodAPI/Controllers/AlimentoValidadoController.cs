using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NutriFoodAPI.Models;
using NutriFoodAPI.Service;
using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace NutriFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlimentoValidadoController : ControllerBase
    {
        private readonly FirestoreService _firestoreService;

        public AlimentoValidadoController(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
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
                    return 
                        BadRequest("O nome do alimento é obrigatório e não pode estar vazio.");
                }

                // Passa o objeto enviado pela Squad 1 para o Service
                var resultado = await _firestoreService.SalvarAlimentoValidado(alimentoExterno);

                // Verifica se o alimento não foi encontrado na API Ninjas
                if (resultado == null)
                {
                    return NotFound(new
                    {
                        mensagem = $"O alimento '{alimentoExterno.Name}' não foi localizado na base nutricional oficial."
                    });
                }

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
                return StatusCode(502, 
                    "O serviço de validação nutricional externo está temporariamente indisponível.");
            }
            catch (Exception)
            {
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
                var alimentos = await _firestoreService.ObterTodos();
                
                return Ok(alimentos);
            }
            catch (Exception)
            {                
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao processar a listagem dos alimentos. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }
        /// <summary>
        /// Obtém um alimento validado específico através do seu ID sequencial.
        /// </summary>
        /// <remarks>
        /// Realiza uma busca direta no Firestore utilizando o ID fornecido na URL.
        /// </remarks>
        /// <param name="id">ID sequencial do alimento armazenado no banco de dados.</param>
        /// <response code="200">Alimento encontrado com sucesso.</response>
        /// <response code="404">Nenhum alimento foi encontrado com o ID informado.</response>
        /// <response code="500">Erro interno no servidor ao tentar acessar o banco de dados.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AlimentoValidado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AlimentoValidado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {                
                var alimento = await _firestoreService.ObterPorId(id);

                ///Se o objeto específico não existe no banco, retorna 404
                if (alimento == null)
                {
                    return NotFound(new
                    {
                        mensagem = $"Alimento com o ID '{id}' não foi encontrado no sistema."
                    });
                }
                
                return Ok(alimento);
            }
            catch (Exception)
            {                
                return StatusCode(500, new
                {
                    mensagem = "Ocorreu um erro interno ao processar a busca do alimento. " +
                    "Por favor, tente novamente mais tarde."
                });
            }
        }
    }
}