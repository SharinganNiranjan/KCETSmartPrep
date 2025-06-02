namespace KcetPrep1.Models
{
    public class UserAnswer
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int QuestionId { get; set; }
        public string SelectedAnswer { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

}
