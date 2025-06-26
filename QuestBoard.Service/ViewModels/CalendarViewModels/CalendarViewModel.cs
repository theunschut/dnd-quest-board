using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels.CalendarViewModels;

public class CalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<Quest> Quests { get; set; } = new();

    public DateTime FirstDayOfMonth => new(Year, Month, 1);
    public DateTime LastDayOfMonth => FirstDayOfMonth.AddMonths(1).AddDays(-1);

    public string MonthName => FirstDayOfMonth.ToString("MMMM yyyy");

    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

    public DayOfWeek FirstDayOfWeek => FirstDayOfMonth.DayOfWeek;

    public List<CalendarDay> GetCalendarDays()
    {
        var days = new List<CalendarDay>();

        // Add empty days for the start of the month
        var firstDayOfWeek = (int)FirstDayOfWeek;
        for (int i = 0; i < firstDayOfWeek; i++)
        {
            days.Add(new CalendarDay { IsEmpty = true });
        }

        // Add actual days of the month
        for (int day = 1; day <= DaysInMonth; day++)
        {
            var date = new DateTime(Year, Month, day);
            var questsOnDay = GetQuestsForDate(date);

            days.Add(new CalendarDay
            {
                Date = date,
                Day = day,
                QuestsOnDay = questsOnDay
            });
        }

        return days;
    }

    private List<QuestOnDay> GetQuestsForDate(DateTime date)
    {
        var questsOnDay = new List<QuestOnDay>();

        foreach (var quest in Quests)
        {
            foreach (var proposedDate in quest.ProposedDates)
            {
                if (proposedDate.Date.Date == date.Date)
                {
                    questsOnDay.Add(new QuestOnDay
                    {
                        Quest = quest,
                        ProposedDate = proposedDate,
                        IsFinalized = quest.IsFinalized && quest.FinalizedDate?.Date == date.Date
                    });
                }
            }
        }

        return questsOnDay;
    }
}