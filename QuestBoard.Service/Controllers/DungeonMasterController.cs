using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Service.Filters;
using QuestBoard.Service.Models.DungeonMaster;

namespace QuestBoard.Service.Controllers
{
    public class DungeonMasterController(IDungeonMasterService service, IMapper mapper) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([Bind()] DungeonMasterFilter filter, bool async = false, string orderBy = "Name", int? maxRows = null, int? page = null, bool filterChanged = false, bool clearFilter = false, CancellationToken token = default)
        {
            filter = await HttpContext.EvaluateFilter(filter, orderBy, maxRows, page, async, filterChanged, clearFilter);

            var dms = await service.GetAllAsync(token);

            var model = new DungeonMasterIndexModel()
            {
                Async = async,
                Rows = mapper.Map<List<DungeonMasterListModel>>(dms).ToList(),
                OrderBy = filter.SortOrder,
                Filter = filter
            };

            if (!async)
            {
                return View(model);
            }
            else
            {
                return PartialView(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            return View();
        }
    }
}