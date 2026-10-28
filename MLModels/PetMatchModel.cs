using Microsoft.ML.Data;

namespace PetWAMS.Web.MLModels
{
    public class PetMatchInput
    {
        public string PreferredCategory { get; set; } = string.Empty;

        public string PreferredBreed { get; set; } = string.Empty;

        public string PreferredGender { get; set; } = string.Empty;

        public float MinimumAge { get; set; }

        public float MaximumAge { get; set; }

        public string PetCategory { get; set; } = string.Empty;

        public string PetBreed { get; set; } = string.Empty;

        public string PetGender { get; set; } = string.Empty;

        public float PetAge { get; set; }

        public string PetStatus { get; set; } = string.Empty;

        public bool Label { get; set; }
    }

    public class PetMatchPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }
    }
}