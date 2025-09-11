using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.ViewModels.ShopViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.Shop;

[Authorize]
public class ShopController(IShopService shopService, IUserService userService, IMapper mapper) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(ItemType? type = null, CancellationToken token = default)
    {
        var items = type.HasValue
            ? await shopService.GetItemsByTypeAsync(type.Value, token)
            : await shopService.GetPublishedItemsAsync(token);

        var viewModel = new ShopIndexViewModel
        {
            Items = mapper.Map<IList<ShopItemViewModel>>(items),
            SelectedType = type
        };

        // Load user purchase history if authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await userService.GetUserAsync(User);
            if (currentUser != null)
            {
                var userTransactions = await shopService.GetUserTransactionsAsync(currentUser.Id, token);
                var mappedTransactions = mapper.Map<IList<UserTransactionViewModel>>(userTransactions.OrderByDescending(t => t.TransactionDate));
                
                // Calculate remaining quantities for purchase transactions
                foreach (var transaction in mappedTransactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Purchase))
                {
                    // Find how much has been returned/sold from this transaction
                    var existingReturns = userTransactions
                        .Where(t => t.TransactionType == Domain.Enums.TransactionType.Sell && 
                                   t.OriginalTransactionId == transaction.Id)
                        .Sum(t => t.Quantity);
                    
                    transaction.RemainingQuantity = transaction.Quantity - existingReturns;
                }
                
                viewModel.UserPurchases = mappedTransactions;
            }
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, bool isModal = false, CancellationToken token = default)
    {
        var item = await shopService.GetItemWithDetailsAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var viewModel = mapper.Map<ShopItemDetailsViewModel>(item);

        if (isModal)
        {
            ViewBag.IsModal = true;
            return PartialView(viewModel);
        }

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Purchase(int id, int quantity = 1, CancellationToken token = default)
    {
        try
        {
            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var transaction = await shopService.PurchaseItemAsync(id, quantity, currentUser, token);
            
            TempData["Success"] = $"Successfully purchased {quantity}x {transaction.ShopItem?.Name ?? "item"} for {transaction.Price} gp!";
            
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while processing your purchase. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Sell(int id, int quantity = 1, CancellationToken token = default)
    {
        try
        {
            var currentUser = await userService.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var transaction = await shopService.ReturnOrSellItemAsync(id, quantity, currentUser, token);
            
            // Calculate if it was a return or sell based on the refund amount
            var originalTransaction = await shopService.GetUserTransactionsAsync(currentUser.Id, token);
            var original = originalTransaction.FirstOrDefault(t => t.Id == id);
            
            if (original != null)
            {
                var originalUnitPrice = original.Price / original.Quantity;
                var expectedReturnPrice = originalUnitPrice * quantity;
                var isReturn = Math.Abs(transaction.Price - expectedReturnPrice) < 0.01m;
                
                var actionType = isReturn ? "returned" : "sold";
                TempData["Success"] = $"Successfully {actionType} {quantity}x {original.ShopItem?.Name ?? "item"} for {transaction.Price} gp!";
            }
            else
            {
                TempData["Success"] = $"Item processed successfully for {transaction.Price} gp!";
            }
            
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while processing your request. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
}