using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.ViewModels;

namespace QuestBoard.Service.Controllers
{
    public class DungeonMasterController(IDungeonMasterService service, IMapper mapper) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token = default)
        {
            var dms = await service.GetAllAsync(token);
            return View(dms);
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

            await service.AddAsync(mapper.Map<DungeonMaster>(model), token);

            return RedirectToAction("Index", "DungeonMaster");
        }
    }
}