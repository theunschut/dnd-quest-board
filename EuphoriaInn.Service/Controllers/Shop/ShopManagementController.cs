using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Service.ViewModels.ShopViewModels;

namespace EuphoriaInn.Service.Controllers.Shop;

[Authorize(Policy = "DungeonMasterOnly")]
public class ShopManagementController(
    IShopService shopService,
    IUserService userService,
    IMapper mapper
    ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token = default)
    {
        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var myItems = await shopService.GetItemsByDmAsync(currentUser.Id, token);
        var draftItems = await shopService.GetItemsByStatusAsync(ItemStatus.Draft, token);

        var viewModel = new ShopManagementIndexViewModel
        {
            MyItems = mapper.Map<IList<ShopItemViewModel>>(myItems),
            ItemsForReview = mapper.Map<IList<ShopItemViewModel>>(draftItems.Where(i => i.CreatedByDmId != currentUser.Id).ToList())
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken token = default)
    {
        var viewModel = new CreateShopItemViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShopItemViewModel viewModel, CancellationToken token = default)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var shopItem = new ShopItem
        {
            Name = viewModel.Name,
            Description = viewModel.Description,
            Type = viewModel.Type,
            Rarity = viewModel.Rarity,
            Price = await shopService.CalculateItemPriceAsync(viewModel.Rarity, token),
            Quantity = viewModel.Quantity,
            ReferenceUrl = viewModel.ReferenceUrl,
            Status = ItemStatus.Draft,
            CreatedByDmId = currentUser.Id,
            AvailableFrom = viewModel.AvailableFrom,
            AvailableUntil = viewModel.AvailableUntil
        };

        await shopService.AddAsync(shopItem, token);
        
        TempData["Success"] = "Item created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken token = default)
    {
        var item = await shopService.GetItemWithDetailsAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null || item.CreatedByDmId != currentUser.Id)
        {
            return Forbid();
        }

        var viewModel = mapper.Map<EditShopItemViewModel>(item);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditShopItemViewModel viewModel, CancellationToken token = default)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var item = await shopService.GetByIdAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null || item.CreatedByDmId != currentUser.Id)
        {
            return Forbid();
        }

        // Only allow editing if item is still in draft status
        if (item.Status != ItemStatus.Draft)
        {
            TempData["Error"] = "Cannot edit items that have been published.";
            return RedirectToAction(nameof(Index));
        }

        item.Name = viewModel.Name;
        item.Description = viewModel.Description;
        item.Type = viewModel.Type;
        item.Rarity = viewModel.Rarity;
        item.Price = await shopService.CalculateItemPriceAsync(viewModel.Rarity, token);
        item.Quantity = viewModel.Quantity;
        item.ReferenceUrl = viewModel.ReferenceUrl;
        item.AvailableFrom = viewModel.AvailableFrom;
        item.AvailableUntil = viewModel.AvailableUntil;

        await shopService.UpdateAsync(item, token);
        
        TempData["Success"] = "Item updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken token = default)
    {
        var item = await shopService.GetByIdAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null || item.CreatedByDmId != currentUser.Id)
        {
            return Forbid();
        }

        if (item.Status != ItemStatus.Draft)
        {
            TempData["Error"] = "This item is already published or archived.";
            return RedirectToAction(nameof(Index));
        }

        await shopService.PublishItemAsync(id, token);
        
        TempData["Success"] = "Item published successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, CancellationToken token = default)
    {
        var item = await shopService.GetByIdAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null || item.CreatedByDmId != currentUser.Id)
        {
            return Forbid();
        }

        await shopService.ArchiveItemAsync(id, token);
        
        TempData["Success"] = "Item archived successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id, CancellationToken token = default)
    {
        var item = await shopService.GetByIdAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null || item.CreatedByDmId != currentUser.Id)
        {
            return Forbid();
        }

        if (item.Status != ItemStatus.Archived)
        {
            TempData["Error"] = "Only archived items can be reopened.";
            return RedirectToAction(nameof(Index));
        }

        await shopService.PublishItemAsync(id, token);
        
        TempData["Success"] = "Item reopened successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken token = default)
    {
        var item = await shopService.GetByIdAsync(id, token);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = await userService.GetUserAsync(User);
        if (currentUser == null || item.CreatedByDmId != currentUser.Id)
        {
            return Forbid();
        }

        // Only allow deletion if item is still in draft status
        if (item.Status != ItemStatus.Draft)
        {
            TempData["Error"] = "Cannot delete items that have been published.";
            return RedirectToAction(nameof(Index));
        }

        await shopService.RemoveAsync(item, token);
        
        TempData["Success"] = "Item deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}