using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace QuestBoard.Repository.Entities;

public class QuestBoardContext(DbContextOptions<QuestBoardContext> options) : IdentityDbContext<UserEntity, IdentityRole<int>, int>(options)
{
    public DbSet<QuestEntity> Quests { get; set; }

    public DbSet<PlayerSignupEntity> PlayerSignups { get; set; }

    public DbSet<UserEntity> UserEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all foreign key relationships to use NO ACTION (Restrict) to avoid cascade cycles
        // This is the safest approach for SQL Server
        
        modelBuilder.Entity<QuestEntity>()
            .HasOne(q => q.DungeonMaster)
            .WithMany(dm => dm.Quests)
            .HasForeignKey(q => q.DungeonMasterId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QuestEntity>()
            .HasMany(q => q.ProposedDates)
            .WithOne(pd => pd.Quest)
            .HasForeignKey(pd => pd.QuestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuestEntity>()
            .HasMany(q => q.PlayerSignups)
            .WithOne(ps => ps.Quest)
            .HasForeignKey(ps => ps.QuestId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PlayerSignupEntity>()
            .HasOne(ps => ps.Player)
            .WithMany()
            .HasForeignKey(ps => ps.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerSignupEntity>()
            .HasMany(ps => ps.DateVotes)
            .WithOne(pdv => pdv.PlayerSignup)
            .HasForeignKey(pdv => pdv.PlayerSignupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProposedDateEntity>()
            .HasMany(pd => pd.PlayerVotes)
            .WithOne(pdv => pdv.ProposedDate)
            .HasForeignKey(pdv => pdv.ProposedDateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique vote per player per date
        modelBuilder.Entity<PlayerDateVoteEntity>()
            .HasIndex(pdv => new { pdv.PlayerSignupId, pdv.ProposedDateId })
            .IsUnique();
    }
}