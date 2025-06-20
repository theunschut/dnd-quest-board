using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Service.Controllers
{
    public class DungeonMasterController(IDungeonMasterRepositorry repositorry, IMapper mapper) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token = default)
        {
            var dmEntities = await repositorry.GetAllAsync(token);
            var dms = mapper.Map<List<DungeonMaster>>(dmEntities);

            return View(dms);
        }
    }
}
