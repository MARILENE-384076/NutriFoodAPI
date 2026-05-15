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
        /// Recebe dados da Squad 1, valida na API Ninjas e persiste no Firebase com ID Sequencial.
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
                // 1. Validação de segurança
                if (alimento == null || string.IsNullOrWhiteSpace(alimento.Nome))
                {
                    return BadRequest("O nome do alimento é obrigatório e não pode estar vazio.");
                }

                /// Chama o Service para realizar a Transação do ID Sequencial e a consulta na API Ninjas
                var resultado = await _firestoreService.SalvarAlimentoValidado(alimento);

                /// Verifica se o alimento não foi encontrado na API Ninjas
                if (resultado == null)
                {
                    return NotFound(new
                    {
                        mensagem = $"O alimento '{alimento.Nome}' " +
                        $"não foi localizado na base nutricional oficial."
                    });
                }

                /// Retorna o alimento com ID Sequencial.              
                return CreatedAtAction(nameof(Post), new { id = resultado.Id }, new
                {
                    mensagem = "Alimento validado e salvo com sucesso!",
                    idSequencial = resultado.Id,
                    dados = resultado
                });
            }
            /// Tratamento específico para falhas na comunicação com a API Ninjas
            catch (HttpRequestException)
            {
                return StatusCode(502, 
                    "O serviço de validação nutricional externo está temporariamente indisponível.");
            }
            /// Tratamento genérico
            catch (Exception ex)
            {
                return StatusCode(500, 
                    $"Erro interno no servidor: {ex.Message}");
            }
        }
    }
}