namespace EuphoriaInn.Domain.Models.Shop;

public record TransactionWithRemaining(UserTransaction Transaction, int RemainingQuantity);
