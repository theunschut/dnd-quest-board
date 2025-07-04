using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Service.ViewModels.GuildMembersViewModels;

namespace QuestBoard.Service.Controllers
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