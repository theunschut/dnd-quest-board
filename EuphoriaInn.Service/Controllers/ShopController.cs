using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Service.ViewModels.ShopViewModels;

namespace EuphoriaInn.Service.Controllers;

public class ShopController(
    IShopService shopService,
    IUserService userService,
    IMapper mapper
    ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token = default)
    {
        var publishedItems = await shopService.GetPublishedItemsAsync(token);
        var viewModel = new ShopIndexViewModel
        {
            Items = mapper.Map<IList<ShopItemViewModel>>(publishedItems)
        };
        
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken token = default)
    {
        var item = await shopService.GetItemWithDetailsAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var viewModel = mapper.Map<ShopItemDetailsViewModel>(item);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Equipment(CancellationToken token = default)
    {
        var equipmentItems = await shopService.GetItemsByTypeAsync(ItemType.Equipment, token);
        var viewModel = new ShopCategoryViewModel
        {
            Title = "Equipment",
            Items = mapper.Map<IList<ShopItemViewModel>>(equipmentItems)
        };
        
        return View("Category", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MagicItems(CancellationToken token = default)
    {
        var magicItems = await shopService.GetItemsByTypeAsync(ItemType.MagicItem, token);
        var viewModel = new ShopCategoryViewModel
        {
            Title = "Magic Items",
            Items = mapper.Map<IList<ShopItemViewModel>>(magicItems)
        };
        
        return View("Category", viewModel);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Purchase(int id, int quantity = 1, CancellationToken token = default)
    {
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var item = await shopService.GetItemWithDetailsAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        if (item.Status != ItemStatus.Published)
        {
            TempData["Error"] = "This item is not available for purchase.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (item.Quantity > 0 && item.Quantity < quantity)
        {
            TempData["Error"] = $"Only {item.Quantity} items available in stock.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // TODO: Implement actual purchase logic with inventory management
        // For now, just show success message
        TempData["Success"] = $"Successfully purchased {quantity}x {item.Name}!";
        
        return RedirectToAction(nameof(Details), new { id });
    }
}