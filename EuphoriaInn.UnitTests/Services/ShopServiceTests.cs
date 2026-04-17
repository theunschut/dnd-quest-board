using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Domain.Services;
using NSubstitute;

namespace EuphoriaInn.UnitTests.Services;

public class ShopServiceTests
{
    [Fact]
    public async Task GetUserTransactionsWithRemainingAsync_ReturnsOnlyPurchaseTransactions()
    {
        var shopRepo = Substitute.For<IShopRepository>();
        var txRepo = Substitute.For<IUserTransactionRepository>();
        var mapper = Substitute.For<IMapper>();

        var purchase = new UserTransaction { Id = 1, Quantity = 3, TransactionType = TransactionType.Purchase };
        var sell = new UserTransaction { Id = 2, Quantity = 1, TransactionType = TransactionType.Sell, OriginalTransactionId = 1 };
        txRepo.GetTransactionsByUserAsync(10, Arg.Any<CancellationToken>())
              .Returns(new List<UserTransaction> { purchase, sell });

        var service = new ShopService(shopRepo, txRepo, mapper);
        var result = await service.GetUserTransactionsWithRemainingAsync(10);

        result.Should().ContainSingle("only Purchase transactions should be included");
        result[0].Transaction.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetUserTransactionsWithRemainingAsync_PurchaseWithPartialReturn_ReturnsCorrectRemaining()
    {
        var shopRepo = Substitute.For<IShopRepository>();
        var txRepo = Substitute.For<IUserTransactionRepository>();
        var mapper = Substitute.For<IMapper>();

        var purchase = new UserTransaction { Id = 1, Quantity = 5, TransactionType = TransactionType.Purchase };
        var sell = new UserTransaction { Id = 2, Quantity = 2, TransactionType = TransactionType.Sell, OriginalTransactionId = 1 };
        txRepo.GetTransactionsByUserAsync(42, Arg.Any<CancellationToken>())
              .Returns(new List<UserTransaction> { purchase, sell });

        var service = new ShopService(shopRepo, txRepo, mapper);
        var result = await service.GetUserTransactionsWithRemainingAsync(42);

        result.Should().ContainSingle();
        result[0].Transaction.Id.Should().Be(1);
        result[0].RemainingQuantity.Should().Be(3);
    }

    [Fact]
    public async Task GetUserTransactionsWithRemainingAsync_PurchaseWithNoReturns_RemainingEqualsQuantity()
    {
        var shopRepo = Substitute.For<IShopRepository>();
        var txRepo = Substitute.For<IUserTransactionRepository>();
        var mapper = Substitute.For<IMapper>();

        var purchase = new UserTransaction { Id = 5, Quantity = 4, TransactionType = TransactionType.Purchase };
        txRepo.GetTransactionsByUserAsync(99, Arg.Any<CancellationToken>())
              .Returns(new List<UserTransaction> { purchase });

        var service = new ShopService(shopRepo, txRepo, mapper);
        var result = await service.GetUserTransactionsWithRemainingAsync(99);

        result.Should().ContainSingle();
        result[0].RemainingQuantity.Should().Be(4, "no returns means remaining equals original quantity");
    }

    [Fact]
    public void ShopServiceSource_UsesSharedCalculateRemainingQuantity()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "EuphoriaInn.Domain", "Services", "ShopService.cs");
        var source = File.ReadAllText(path);
        var occurrences = System.Text.RegularExpressions.Regex.Matches(source, @"CalculateRemainingQuantity\(").Count;
        occurrences.Should().BeGreaterThanOrEqualTo(2,
            "CTRL-04: helper must be used by both GetUserTransactionsWithRemainingAsync and ReturnOrSellItemAsync");
    }
}
