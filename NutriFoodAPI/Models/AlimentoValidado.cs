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
        public decimal Calorias { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("serving_size_g")]
        public decimal PorcaoGramas { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("fat_total_g")]
        public decimal GorduraTotal { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("fat_saturated_g")]
        public decimal GorduraSaturada { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("protein_g")]
        public decimal Proteina { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("sodium_mg")]
        public decimal Sodio { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("potassium_mg")]
        public decimal Potassio { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("cholesterol_mg")]
        public decimal Colesterol { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("carbohydrates_total_g")]
        public decimal Carboidratos { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("fiber_g")]
        public decimal Fibras { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("sugar_g")]
        public decimal Acucar { get; set; }

        [FirestoreProperty]
        public DateTime DataValidacao { get; set; }
    }
}