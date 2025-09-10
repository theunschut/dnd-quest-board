using EuphoriaInn.Domain.Enums;

namespace EuphoriaInn.Service.ViewModels.ShopViewModels;

public class UserTransactionViewModel
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
}