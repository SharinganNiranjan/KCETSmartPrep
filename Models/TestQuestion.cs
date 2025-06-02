namespace KcetPrep1.Models
{
    public class TestQuestion
    {
        public int TestId { get; set; }
        public Test Test { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
    }
}