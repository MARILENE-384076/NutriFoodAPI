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
                    return BadRequest("O nome do alimento é obrigatório e não pode estar vazio.");
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
            catch (Exception ex)
            {
                return StatusCode(500, 
                    $"Erro interno no servidor: {ex.Message}");
            }
        }
        /// <summary>
        ///  Obtém a lista completa de todos os alimentos validados.
        /// </summary>
        /// <returns></returns>
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
    }
}