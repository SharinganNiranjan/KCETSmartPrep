namespace KcetPrep1.Models
{
    public class DocumentTemplate
    {
        public int Id { get; set; }

        public string DocumentName { get; set; } // e.g., 10th Marks Card
        public string FileName { get; set; }     // original filename
        public string FilePath { get; set; }     // path for download/view

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

}
