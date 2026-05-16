using Google.Cloud.Firestore;
using System;
using System.Text.Json.Serialization;

namespace NutriFoodAPI.Models
{
    [FirestoreData]
    public class AlimentoValidado
    {
        [FirestoreProperty]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty]
        [JsonPropertyName("name")]
        public string Nome { get; set; } = string.Empty;

        [FirestoreProperty]
        [JsonPropertyName("calories")]
        public double Calorias { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("serving_size_g")]
        public double PorcaoGramas { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("fat_total_g")]
        public double GorduraTotal { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("fat_saturated_g")]
        public double GorduraSaturada { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("protein_g")]
        public double Proteina { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("sodium_mg")]
        public double Sodio { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("potassium_mg")]
        public double Potassio { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("cholesterol_mg")]
        public double Colesterol { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("carbohydrates_total_g")]
        public double Carboidratos { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("fiber_g")]
        public double Fibras { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("sugar_g")]
        public double Acucar { get; set; }

        [FirestoreProperty]
        public DateTime DataValidacao { get; set; } = DateTime.UtcNow;
    }
}