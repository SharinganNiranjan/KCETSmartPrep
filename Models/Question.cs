namespace KcetPrep1.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Subject { get; set; } // Phy, Che, Math, Bio
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectAnswer { get; set; } // A, B, C, or D
    }

}
