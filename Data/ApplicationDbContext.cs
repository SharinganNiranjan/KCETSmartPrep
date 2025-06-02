using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Identity;

namespace KcetPrep1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<College> Colleges { get; set; }
        public DbSet<CutoffFile> CutoffFiles { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestQuestion> TestQuestions { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Question entity
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Subject).IsRequired().HasMaxLength(50);
                entity.Property(q => q.QuestionText).IsRequired();
                entity.Property(q => q.OptionA).IsRequired();
                entity.Property(q => q.OptionB).IsRequired();
                entity.Property(q => q.OptionC).IsRequired();
                entity.Property(q => q.OptionD).IsRequired();
                entity.Property(q => q.CorrectAnswer).IsRequired().HasMaxLength(1);
            });

            // Configure Test entity
            modelBuilder.Entity<Test>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Subject).IsRequired().HasMaxLength(50);
                entity.Property(t => t.TestName).IsRequired().HasMaxLength(100);
            });

            // Configure TestQuestion entity
            modelBuilder.Entity<TestQuestion>(entity =>
            {
                entity.HasKey(tq => new { tq.TestId, tq.QuestionId });

                entity.HasOne(tq => tq.Test)
                      .WithMany()
                      .HasForeignKey(tq => tq.TestId);

                entity.HasOne(tq => tq.Question)
                      .WithMany()
                      .HasForeignKey(tq => tq.QuestionId);
            });

            // Configure UserAnswer entity
            modelBuilder.Entity<UserAnswer>(entity =>
            {
                entity.HasKey(ua => ua.Id);
                entity.Property(ua => ua.UserId).IsRequired();
                entity.Property(ua => ua.SelectedAnswer).IsRequired().HasMaxLength(1);
                entity.Property(ua => ua.SubmittedAt).IsRequired();

                entity.HasOne<Question>()
                      .WithMany()
                      .HasForeignKey(ua => ua.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}