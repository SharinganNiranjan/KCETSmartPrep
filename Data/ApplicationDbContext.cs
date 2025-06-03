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

            // ─── Override Identity column types for SQLite ─────────────────────────────────
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // AspNetRoles table
                modelBuilder.Entity<IdentityRole>(b =>
                {
                    b.Property(r => r.Id)
                     .HasColumnType("TEXT");
                    b.Property(r => r.Name)
                     .HasMaxLength(256)
                     .HasColumnType("TEXT");
                    b.Property(r => r.NormalizedName)
                     .HasMaxLength(256)
                     .HasColumnType("TEXT");
                    b.Property(r => r.ConcurrencyStamp)
                     .HasColumnType("TEXT");
                });

                // AspNetUsers table (ApplicationUser inherits from IdentityUser<string>)
                modelBuilder.Entity<ApplicationUser>(b =>
                {
                    b.Property(u => u.Id)
                     .HasColumnType("TEXT");
                    b.Property(u => u.UserName)
                     .HasMaxLength(256)
                     .HasColumnType("TEXT");
                    b.Property(u => u.NormalizedUserName)
                     .HasMaxLength(256)
                     .HasColumnType("TEXT");
                    b.Property(u => u.Email)
                     .HasMaxLength(256)
                     .HasColumnType("TEXT");
                    b.Property(u => u.NormalizedEmail)
                     .HasMaxLength(256)
                     .HasColumnType("TEXT");
                    b.Property(u => u.PasswordHash)
                     .HasColumnType("TEXT");
                    b.Property(u => u.SecurityStamp)
                     .HasColumnType("TEXT");
                    b.Property(u => u.ConcurrencyStamp)
                     .HasColumnType("TEXT");
                    b.Property(u => u.PhoneNumber)
                     .HasColumnType("TEXT");
                    // If you extended ApplicationUser with additional string properties:
                    // b.Property(u => u.FullName).HasColumnType("TEXT").HasMaxLength(100);
                });

                // AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims, etc.
                // will pick up TEXT for their foreign‐key columns because they reference the TEXT‐typed PK columns above.
            }

            // ─── Configure your own entities ──────────────────────────────────────────────────
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Subject)
                      .IsRequired()
                      .HasMaxLength(50)
                      // For SQLite, set TEXT; for SQL Server, EF defaults to nvarchar(50)
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(50)");
                entity.Property(q => q.QuestionText)
                      .IsRequired()
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionA)
                      .IsRequired()
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionB)
                      .IsRequired()
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionC)
                      .IsRequired()
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.OptionD)
                      .IsRequired()
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(max)");
                entity.Property(q => q.CorrectAnswer)
                      .IsRequired()
                      .HasMaxLength(1)
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(1)");
            });

            modelBuilder.Entity<Test>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Subject)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(50)");
                entity.Property(t => t.TestName)
                      .IsRequired()
                      .HasMaxLength(100)
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(100)");
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
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(max)");
                entity.Property(ua => ua.SelectedAnswer)
                      .IsRequired()
                      .HasMaxLength(1)
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "nvarchar(1)");
                entity.Property(ua => ua.SubmittedAt)
                      .IsRequired()
                      .HasColumnType(Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                                     ? "TEXT" : "datetime2");

                entity.HasOne<Question>()
                      .WithMany()
                      .HasForeignKey(ua => ua.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // If you have other entities (College, CutoffFile, etc.), and they contain string properties,
            // you should also explicitly call .HasColumnType(...) for each string so that SQLite uses TEXT.
        }
    }
}
