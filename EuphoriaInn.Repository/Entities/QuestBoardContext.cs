using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository.Entities;

public class QuestBoardContext(DbContextOptions<QuestBoardContext> options) : IdentityDbContext<UserEntity, IdentityRole<int>, int>(options)
{
    public DbSet<QuestEntity> Quests { get; set; }

    public DbSet<PlayerSignupEntity> PlayerSignups { get; set; }

    public DbSet<UserEntity> UserEntities { get; set; }

    public DbSet<ShopItemEntity> ShopItems { get; set; }

    public DbSet<UserTransactionEntity> UserTransactions { get; set; }

    public DbSet<TradeItemEntity> TradeItems { get; set; }

    public DbSet<CharacterEntity> Characters { get; set; }

    public DbSet<CharacterClassEntity> CharacterClasses { get; set; }

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

        // Shop entity relationships
        modelBuilder.Entity<ShopItemEntity>()
            .HasOne(si => si.CreatedByDm)
            .WithMany()
            .HasForeignKey(si => si.CreatedByDmId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ShopItemEntity>()
            .HasMany(si => si.Transactions)
            .WithOne(t => t.ShopItem)
            .HasForeignKey(t => t.ShopItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserTransactionEntity>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TradeItemEntity>()
            .HasOne(ti => ti.OfferedByPlayer)
            .WithMany()
            .HasForeignKey(ti => ti.OfferedByPlayerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Character entity relationships
        modelBuilder.Entity<CharacterEntity>()
            .HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CharacterEntity>()
            .HasMany(c => c.Classes)
            .WithOne(cc => cc.Character)
            .HasForeignKey(cc => cc.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CharacterEntity>()
            .HasMany(c => c.PlayerSignups)
            .WithOne(ps => ps.Character)
            .HasForeignKey(ps => ps.CharacterId)
            .OnDelete(DeleteBehavior.NoAction);

        // Player signup can optionally have a character
        modelBuilder.Entity<PlayerSignupEntity>()
            .HasOne(ps => ps.Character)
            .WithMany(c => c.PlayerSignups)
            .HasForeignKey(ps => ps.CharacterId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}