using System.Text.Json;
using System.Text.Json.Serialization;

namespace NutriFoodAPI.Models
{
    public class AlimentoConsultaExterna
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("calories")]
        public JsonElement Calories { get; set; }

        [JsonPropertyName("serving_size_g")]
        public JsonElement ServingSizeG { get; set; }

        [JsonPropertyName("fat_total_g")]
        public JsonElement FatTotalG { get; set; }

        [JsonPropertyName("fat_saturated_g")]
        public JsonElement FatSaturatedG { get; set; }

        [JsonPropertyName("protein_g")]
        public JsonElement ProteinG { get; set; }

        [JsonPropertyName("sodium_mg")]
        public JsonElement SodiumMg { get; set; }

        [JsonPropertyName("potassium_mg")]
        public JsonElement PotassiumMg { get; set; }

        [JsonPropertyName("cholesterol_mg")]
        public JsonElement CholesterolMg { get; set; }

        [JsonPropertyName("carbohydrates_total_g")]
        public JsonElement CarbohydratesTotalG { get; set; }

        [JsonPropertyName("fiber_g")]
        public JsonElement FiberG { get; set; }

        [JsonPropertyName("sugar_g")]
        public JsonElement SugarG { get; set; }
    }
}