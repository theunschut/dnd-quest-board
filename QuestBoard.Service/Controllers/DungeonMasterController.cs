using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;

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

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DungeonMaster model, CancellationToken token = default)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index", "DungeonMaster");
            }

            await service.AddAsync(model, token);

            return RedirectToAction("Index", "DungeonMaster");
        }
    }
}