using IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data;

public class IdentityContext : DbContext
{
    public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PreApprovedStudent> PreApprovedStudents => Set<PreApprovedStudent>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.NationalId).HasMaxLength(9);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.HasIndex(x => x.NationalId).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<PreApprovedStudent>(entity =>
        {
            entity.Property(x => x.NationalId).HasMaxLength(9);
            entity.HasIndex(x => x.NationalId).IsUnique();
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(100);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<Admin>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Employer>(entity =>
        {
            entity.Property(x => x.CompanyName).HasMaxLength(100);
            entity.Property(x => x.ContactPhone).HasMaxLength(15);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<Employer>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(64);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(64);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
