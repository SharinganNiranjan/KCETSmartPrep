namespace KcetPrep1.Models
{
    public class CutoffFile
    {
        public int Id { get; set; }

        public string FileName { get; set; } // original filename
        public string FilePath { get; set; } // stored path
        public int Year { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

}
