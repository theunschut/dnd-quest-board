using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.ViewModels.DungeonMasterViewModels;

namespace QuestBoard.Service.Controllers
{
    [Authorize(Policy = "DungeonMasterOnly")]
    public class DungeonMasterController(IUserService service, IMapper mapper) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token = default)
        {
            var viewModel = new DungeonMasterIndexViewModel
            {
                DungeonMasters = await service.GetAllDungeonMastersAsync(token),
                Players = await service.GetAllPlayersAsync(token)
            };
            
            return View(viewModel);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dm = await service.GetByIdAsync(id);

            if (dm == null)
            {
                return NotFound();
            }

            await service.RemoveAsync(dm);

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDungeonMasterViewModel model, CancellationToken token = default)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index", "DungeonMaster");
            }

            await service.AddAsync(mapper.Map<User>(model), token);

            return RedirectToAction("Index", "DungeonMaster");
        }
    }
}