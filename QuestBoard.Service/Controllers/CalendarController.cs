using Microsoft.AspNetCore.Mvc;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Service.ViewModels.CalendarViewModels;

namespace QuestBoard.Service.Controllers;

public class CalendarController(IQuestService questService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? year = null, int? month = null, CancellationToken token = default)
    {
        // Default to current month if not specified
        var currentDate = DateTime.Now;
        var selectedYear = year ?? currentDate.Year;
        var selectedMonth = month ?? currentDate.Month;

        // Get all quests with their proposed dates
        var allQuests = await questService.GetQuestsWithSignupsAsync(token);

        // Create calendar model
        var calendarModel = new CalendarViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            Quests = [.. allQuests]
        };

        return View(calendarModel);
    }
}