using Microsoft.EntityFrameworkCore;
using QuestBoard.Models;

namespace QuestBoard.Data;

public class QuestBoardContext(DbContextOptions<QuestBoardContext> options) : DbContext(options)
{
    public DbSet<Quest> Quests { get; set; }
    public DbSet<ProposedDate> ProposedDates { get; set; }
    public DbSet<PlayerSignup> PlayerSignups { get; set; }
    public DbSet<PlayerDateVote> PlayerDateVotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Quest relationships
        modelBuilder.Entity<Quest>()
            .HasMany(q => q.ProposedDates)
            .WithOne(pd => pd.Quest)
            .HasForeignKey(pd => pd.QuestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quest>()
            .HasMany(q => q.PlayerSignups)
            .WithOne(ps => ps.Quest)
            .HasForeignKey(ps => ps.QuestId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlayerSignup relationships
        modelBuilder.Entity<PlayerSignup>()
            .HasMany(ps => ps.DateVotes)
            .WithOne(pdv => pdv.PlayerSignup)
            .HasForeignKey(pdv => pdv.PlayerSignupId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProposedDate relationships
        modelBuilder.Entity<ProposedDate>()
            .HasMany(pd => pd.PlayerVotes)
            .WithOne(pdv => pdv.ProposedDate)
            .HasForeignKey(pdv => pdv.ProposedDateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique vote per player per date
        modelBuilder.Entity<PlayerDateVote>()
            .HasIndex(pdv => new { pdv.PlayerSignupId, pdv.ProposedDateId })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}