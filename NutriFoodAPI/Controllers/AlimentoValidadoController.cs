using Microsoft.AspNetCore.Mvc;
using NutriFoodAPI.Models;
using NutriFoodAPI.Service;
using System;
using System.Text.Json;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Post([FromBody] AlimentoConsultaExterna alimentoExterno)
        {
            try
            {
                
                if (alimentoExterno == null || string.IsNullOrWhiteSpace(alimentoExterno.Name))
                {
                    return BadRequest("O nome do alimento é obrigatório e não pode estar vazio.");
                }

                // Transforma os JsonElements instáveis em doubles
                var alimentoProntoParaBanco = new AlimentoValidado
                {
                    Nome = alimentoExterno.Name,
                    Calorias = ConverterSeguro(alimentoExterno.Calories),
                    PorcaoGramas = ConverterSeguro(alimentoExterno.ServingSizeG),
                    GorduraTotal = ConverterSeguro(alimentoExterno.FatTotalG),
                    GorduraSaturada = ConverterSeguro(alimentoExterno.FatSaturatedG),
                    Proteina = ConverterSeguro(alimentoExterno.ProteinG),
                    Sodio = ConverterSeguro(alimentoExterno.SodiumMg),
                    Potassio = ConverterSeguro(alimentoExterno.PotassiumMg),
                    Colesterol = ConverterSeguro(alimentoExterno.CholesterolMg),
                    Carboidratos = ConverterSeguro(alimentoExterno.CarbohydratesTotalG),
                    Fibras = ConverterSeguro(alimentoExterno.FiberG),
                    Acucar = ConverterSeguro(alimentoExterno.SugarG),
                    DataValidacao = DateTime.UtcNow
                };

                // Chama o Service passando a entidade oficial tipada com double
                var resultado = await _firestoreService.SalvarAlimentoValidado(alimentoProntoParaBanco);

                // Verifica se o alimento não foi encontrado na API Ninjas
                if (resultado == null)
                {
                    return NotFound(new
                    {
                        mensagem = $"O alimento '{alimentoExterno.Name}' " +
                        $"não foi localizado na base nutricional oficial."
                    });
                }

                // Retorna o alimento com ID Sequencial.              
                return CreatedAtAction(nameof(Post), new { id = resultado.Id }, new
                {
                    mensagem = "Alimento validado e salvo com sucesso!",
                    idSequencial = resultado.Id,
                    dados = resultado
                });
            }
            // Tratamento específico para falhas na comunicação com a API Ninjas
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
        /// Método auxiliar para extrair e converter JsonElement para double com segurança total.
        /// </summary>
        private double ConverterSeguro(JsonElement elemento)
        {
            if (elemento.ValueKind == JsonValueKind.Number)
                return elemento.GetDouble();

            if (elemento.ValueKind == JsonValueKind.String && double.TryParse(elemento.GetString(), out double resultado))
                return resultado;

            return 0; // Valor padrão caso venha nulo, vazio ou inválido da API
        }
    }
}