using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.ViewModels.GuildMembersViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers
{
    [Authorize]
    public class GuildMembersController(IUserService service) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token = default)
        {
            var viewModel = new GuildMembersIndexViewModel
            {
                DungeonMasters = await service.GetAllDungeonMastersAsync(token),
                Players = await service.GetAllPlayersAsync(token)
            };
            
            return View(viewModel);
        }

    }
}