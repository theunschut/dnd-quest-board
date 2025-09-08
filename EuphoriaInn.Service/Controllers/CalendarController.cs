using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.ViewModels.CalendarViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers;

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
        var allQuests = await questService.GetQuestsWithDetailsAsync(token);

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