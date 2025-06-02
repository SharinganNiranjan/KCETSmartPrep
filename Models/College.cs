using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace KcetPrep1.Models
{
    public class College
    {
        public int Id { get; set; }
        public string CETCode { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Branch { get; set; }

        // Stores the JSON-serialized dictionary in the database
        [Column(TypeName = "nvarchar(max)")]
        public string CutoffRanksJson { get; set; }

        // Used in the application as a dictionary (not stored directly in DB)
        [NotMapped]
        public Dictionary<string, int> CutoffRanks
        {
            get => string.IsNullOrEmpty(CutoffRanksJson)
                ? new Dictionary<string, int>()
                : JsonSerializer.Deserialize<Dictionary<string, int>>(CutoffRanksJson) ?? new Dictionary<string, int>();
            set => CutoffRanksJson = JsonSerializer.Serialize(value);
        }
    }
}
