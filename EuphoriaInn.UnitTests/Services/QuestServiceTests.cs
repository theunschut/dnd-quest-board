using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Domain.Services;
using NSubstitute;

namespace EuphoriaInn.UnitTests.Services;

public class QuestServiceTests
{
    private readonly IQuestRepository _repository;
    private readonly IPlayerSignupRepository _playerSignupRepository;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly QuestService _sut;

    public QuestServiceTests()
    {
        _repository = Substitute.For<IQuestRepository>();
        _playerSignupRepository = Substitute.For<IPlayerSignupRepository>();
        _emailService = Substitute.For<IEmailService>();
        _mapper = Substitute.For<IMapper>();

        _sut = new QuestService(_repository, _playerSignupRepository, _emailService, _mapper);
    }

    // Helper: create a quest with specified signups
    private static Quest MakeQuest(int id, IList<PlayerSignup>? signups = null) =>
        new()
        {
            Id = id,
            Title = "Test Quest",
            Description = "A quest",
            DungeonMaster = new User { Id = 1, Name = "DM Dave", Email = "dm@example.com" },
            PlayerSignups = signups ?? [],
            ProposedDates = []
        };

    private static PlayerSignup MakeSignup(int id, string email, SignupRole role = SignupRole.Player, bool isSelected = true) =>
        new()
        {
            Id = id,
            Role = role,
            IsSelected = isSelected,
            Player = new User { Id = id + 10, Name = $"Player {id}", Email = email },
            Quest = new Quest { Id = 1, Title = "T", Description = "D" }
        };

    // ---------------------------------------------------------------------------
    // FinalizeQuestAsync — CTRL-01 / EMAIL-04
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task FinalizeQuestAsync_WhenQuestReFetchReturnsNull_SendsNoEmails()
    {
        // Arrange
        _repository.FinalizeQuestAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<IList<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.GetQuestWithDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Quest?)null);

        // Act
        await _sut.FinalizeQuestAsync(1, DateTime.UtcNow, [42]);

        // Assert
        await _emailService.DidNotReceive().SendQuestFinalizedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task FinalizeQuestAsync_WithMixedSelectedAndSpectatorSignups_SendsToBoth()
    {
        // Arrange: signup 1 is selected player, signup 2 is spectator (auto-included), signup 3 is unselected player
        var signups = new List<PlayerSignup>
        {
            MakeSignup(1, "player1@x.com", SignupRole.Player),
            MakeSignup(2, "spectator@x.com", SignupRole.Spectator),
            MakeSignup(3, "player3@x.com", SignupRole.Player) // NOT in selectedPlayerIds
        };
        var quest = MakeQuest(1, signups);

        _repository.FinalizeQuestAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<IList<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.GetQuestWithDetailsAsync(1, Arg.Any<CancellationToken>())
            .Returns(quest);

        var selectedIds = new List<int> { 1 }; // Only signup id=1; spectator id=2 is auto-included

        // Act
        await _sut.FinalizeQuestAsync(1, DateTime.UtcNow, selectedIds);

        // Assert: emails sent to signup 1 (selected) and signup 2 (spectator), NOT signup 3
        await _emailService.Received(2).SendQuestFinalizedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
        await _emailService.Received(1).SendQuestFinalizedEmailAsync(
            "player1@x.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
        await _emailService.Received(1).SendQuestFinalizedEmailAsync(
            "spectator@x.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
        await _emailService.DidNotReceive().SendQuestFinalizedEmailAsync(
            "player3@x.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
    }

    // ---------------------------------------------------------------------------
    // UpdateQuestPropertiesWithNotificationsAsync — CTRL-03
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateQuestPropertiesWithNotificationsAsync_WithNoAffectedPlayers_ReturnsOkZero()
    {
        // Arrange
        _repository.UpdateQuestPropertiesWithNotificationsAsync(
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IList<DateTime>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        // Act
        var result = await _sut.UpdateQuestPropertiesWithNotificationsAsync(
            1, "Title", "Desc", 5, 4, false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(0);
        await _emailService.DidNotReceive().SendQuestDateChangedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateQuestPropertiesWithNotificationsAsync_WithAffectedPlayers_SendsEmailAndReturnsCount()
    {
        // Arrange
        var affectedPlayers = new List<User>
        {
            new() { Id = 1, Name = "Alice", Email = "alice@x.com" },
            new() { Id = 2, Name = "Bob", Email = "bob@x.com" },
            new() { Id = 3, Name = "NoEmail", Email = "" } // should be skipped
        };

        _repository.UpdateQuestPropertiesWithNotificationsAsync(
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IList<DateTime>?>(),
                Arg.Any<CancellationToken>())
            .Returns(affectedPlayers);

        var quest = MakeQuest(1);
        _repository.GetQuestWithDetailsAsync(1, Arg.Any<CancellationToken>())
            .Returns(quest);

        // Act
        var result = await _sut.UpdateQuestPropertiesWithNotificationsAsync(
            1, "Title", "Desc", 5, 4, false);

        // Assert: only players with non-empty email get emailed
        result.Success.Should().BeTrue();
        result.Data.Should().Be(2, "only Alice and Bob have non-empty emails");

        await _emailService.Received(2).SendQuestDateChangedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _emailService.Received(1).SendQuestDateChangedEmailAsync(
            "alice@x.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _emailService.Received(1).SendQuestDateChangedEmailAsync(
            "bob@x.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _emailService.DidNotReceive().SendQuestDateChangedEmailAsync(
            "", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
