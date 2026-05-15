using Google.Cloud.Firestore;
using System;
using System.Text.Json.Serialization;

namespace NutriFoodAPI.Models
{
    public class Alimento
    {
        [JsonPropertyName("name")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("calories")]
        public double Calorias { get; set; }

        [JsonPropertyName("serving_size_g")]
        public double PorcaoGramas { get; set; }

        [JsonPropertyName("fat_total_g")]
        public double GorduraTotal { get; set; }

        [JsonPropertyName("fat_saturated_g")]
        public double GorduraSaturada { get; set; }

        [JsonPropertyName("protein_g")]
        public double Proteina { get; set; }

        [JsonPropertyName("sodium_mg")]
        public double Sodio { get; set; }

        [JsonPropertyName("potassium_mg")]
        public double Potassio { get; set; }

        [JsonPropertyName("cholesterol_mg")]
        public double Colesterol { get; set; }

        [JsonPropertyName("carbohydrates_total_g")]
        public double Carboidratos { get; set; }

        [JsonPropertyName("fiber_g")]
        public double Fibras { get; set; }

        [JsonPropertyName("sugar_g")]
        public double Acucar { get; set; }
    }
}