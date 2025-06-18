using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestBoard.Data;
using QuestBoard.Models;

namespace QuestBoard.Pages;

public class CreateQuestModel : PageModel
{
    private readonly QuestBoardContext _context;

    public CreateQuestModel(QuestBoardContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Quest Quest { get; set; } = default!;

    public void OnGet()
    {
        Quest = new Quest();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Debug: Log all form keys for troubleshooting
            var formKeys = string.Join(", ", Request.Form.Keys);
            Console.WriteLine($"Form keys received: {formKeys}");
            
            // Get proposed dates from form - handle array notation
            var proposedDates = new List<DateTime>();
            
            // Look for ProposedDates[0], ProposedDates[1], etc.
            for (int i = 0; i < 10; i++) // Allow up to 10 proposed dates
            {
                var dateKey = $"ProposedDates[{i}]";
                if (Request.Form.ContainsKey(dateKey))
                {
                    var dateString = Request.Form[dateKey].ToString();
                    Console.WriteLine($"Found date field {dateKey}: '{dateString}'");
                    
                    if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var date))
                    {
                        proposedDates.Add(date);
                        Console.WriteLine($"Successfully parsed date: {date}");
                    }
                }
            }
            
            Console.WriteLine($"Total proposed dates found: {proposedDates.Count}");

            if (!proposedDates.Any())
            {
                ModelState.AddModelError("", "At least one proposed date is required.");
            }

            // Validate Quest properties
            if (string.IsNullOrWhiteSpace(Quest.Title))
            {
                ModelState.AddModelError("Quest.Title", "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(Quest.Description))
            {
                ModelState.AddModelError("Quest.Description", "Description is required.");
            }

            if (string.IsNullOrWhiteSpace(Quest.DmName))
            {
                ModelState.AddModelError("Quest.DmName", "DM Name is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Quest.CreatedAt = DateTime.UtcNow;
            _context.Quests.Add(Quest);
            await _context.SaveChangesAsync();

            // Add proposed dates
            foreach (var date in proposedDates)
            {
                var proposedDate = new ProposedDate
                {
                    QuestId = Quest.Id,
                    Date = date
                };
                _context.ProposedDates.Add(proposedDate);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            return Page();
        }
    }
}