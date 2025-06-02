namespace KcetPrep1.Models
{
    public class CollegePredictionResult
    {
        public string CollegeName { get; set; }
        public string Branch { get; set; }
        public string Category { get; set; }
        public int ClosingRank { get; set; }
        public string Chance { get; set; }
        public string Years { get; set; } // Years contributing to the prediction
    }

}
