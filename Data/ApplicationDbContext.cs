using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KcetPrep1.Models;

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

            // ─── Configure Identity types for SQLite ─────────────────────
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                modelBuilder.Entity<IdentityRole>(b =>
                {
                    b.Property(r => r.Id).HasColumnType("TEXT");
                    b.Property(r => r.Name).HasMaxLength(256).HasColumnType("TEXT");
                    b.Property(r => r.NormalizedName).HasMaxLength(256).HasColumnType("TEXT");
                    b.Property(r => r.ConcurrencyStamp).HasColumnType("TEXT");
                });

                modelBuilder.Entity<ApplicationUser>(b =>
                {
                    b.Property(u => u.Id).HasColumnType("TEXT");
                    b.Property(u => u.UserName).HasMaxLength(256).HasColumnType("TEXT");
                    b.Property(u => u.NormalizedUserName).HasMaxLength(256).HasColumnType("TEXT");
                    b.Property(u => u.Email).HasMaxLength(256).HasColumnType("TEXT");
                    b.Property(u => u.NormalizedEmail).HasMaxLength(256).HasColumnType("TEXT");
                    b.Property(u => u.PasswordHash).HasColumnType("TEXT");
                    b.Property(u => u.SecurityStamp).HasColumnType("TEXT");
                    b.Property(u => u.ConcurrencyStamp).HasColumnType("TEXT");
                    b.Property(u => u.PhoneNumber).HasColumnType("TEXT");
                    // Add your custom ApplicationUser properties here if needed
                });
            }

            // ─── Configure Custom Entities ──────────────────────────────

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Subject)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(50)");
                entity.Property(q => q.QuestionText)
                      .IsRequired()
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionA).IsRequired().HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionB).IsRequired().HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionC).IsRequired().HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionD).IsRequired().HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.CorrectAnswer)
                      .IsRequired()
                      .HasMaxLength(1)
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(1)");
            });

            modelBuilder.Entity<Test>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Subject)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(50)");
                entity.Property(t => t.TestName)
                      .IsRequired()
                      .HasMaxLength(100)
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(100)");
            });

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

            modelBuilder.Entity<UserAnswer>(entity =>
            {
                entity.HasKey(ua => ua.Id);
                entity.Property(ua => ua.UserId)
                      .IsRequired()
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(max)");
                entity.Property(ua => ua.SelectedAnswer)
                      .IsRequired()
                      .HasMaxLength(1)
                      .HasColumnType(IsSqlite() ? "TEXT" : "nvarchar(1)");
                entity.Property(ua => ua.SubmittedAt)
                      .IsRequired()
                      .HasColumnType(IsSqlite() ? "TEXT" : "datetime2");

                entity.HasOne<Question>()
                      .WithMany()
                      .HasForeignKey(ua => ua.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Optionally configure College, CutoffFile, DocumentTemplate if needed
        }

        private bool IsSqlite()
        {
            return Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        }
    }
}
