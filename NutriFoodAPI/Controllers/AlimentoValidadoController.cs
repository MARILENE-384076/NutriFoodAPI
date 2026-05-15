using Microsoft.AspNetCore.Mvc;
using NutriFoodAPI.Models;
using NutriFoodAPI.Service;

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
        /// Recebe dados da Squad 1, valida na API Ninjas e persiste no Firebase.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Post([FromBody] AlimentoValidado alimento)
        {
            try
            {
                /// Validação campos obrigatórios (ex: Nome)
                if (alimento == null || string.IsNullOrWhiteSpace(alimento.Nome))
                {
                    return BadRequest("O nome do alimento é obrigatório e não pode estar vazio.");
                }

                /// Valida o alimento na API Ninjas e salva no Firestore
                var resultado = await _firestoreService.SalvarAlimentoValidado(alimento);

                /// Se a API Ninjas não encontrar o alimento, retorna 404 Not Found
                if (resultado == null)
                {
                    return NotFound(new 
                    { mensagem = $"O alimento '{alimento.Nome}' " +
                    $"não foi localizado na base nutricional oficial." });
                }

                /// Retorna 201 Created com os dados do alimento validado
                return Created("", new
                {
                    mensagem = "Alimento validado e salvo com sucesso!",
                    dados = resultado
                });
            }

            /// Tratamento específico para falhas na comunicação com a API Ninjas
            catch (HttpRequestException)
            {           
                return StatusCode(502,
                    "O serviço de validação nutricional externo está temporariamente indisponível.");
            }
            /// Tratamento genérico para outras exceções, como falhas no Firestore ou erros inesperados
            catch (Exception ex)
            {                
                return StatusCode(500,
                    $"Erro interno no servidor: {ex.Message}");
            }
        }
    }
}