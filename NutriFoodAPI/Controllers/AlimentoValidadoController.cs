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

        // O ASP.NET Core injeta automaticamente o serviço que você registrou na Program.cs
        public AlimentoValidadoController(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        /// <summary>
        /// Recebe um alimento e o persiste no Google Firestore.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AlimentoValidado alimento)
        {
            try
            {
                if (alimento == null)
                {
                    return BadRequest("Os dados do alimento não podem ser nulos.");
                }

                // Chama o método de salvamento do Service
                await _firestoreService.SalvarAlimentoValidado(alimento);

                return Created("", new { mensagem = $"Alimento {alimento.Nome} salvo com sucesso!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno ao salvar no Firebase: {ex.Message}");
            }
        }
    }
}