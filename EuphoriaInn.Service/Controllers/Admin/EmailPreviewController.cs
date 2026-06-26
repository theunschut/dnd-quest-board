using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Service.Components.Emails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
public class EmailPreviewController(IEmailRenderService emailRenderService) : Controller
{
    private static readonly IList<string> SamplePlayers = ["Arannis", "Tordek", "Mialee"];

    [HttpGet]
    public IActionResult Index()
    {
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var html = $$"""
            <!doctype html><html><head><meta charset="utf-8">
            <title>Email Preview — Admin</title>
            <style>body{font-family:sans-serif;padding:2rem;max-width:600px;margin:auto}
            h1{margin-bottom:1rem}ul{list-style:none;padding:0}
            li{margin:.5rem 0}a{color:#4a6cf7;text-decoration:none;font-size:1.1rem}
            a:hover{text-decoration:underline}</style></head>
            <body><h1>Email Template Previews</h1>
            <ul>
              <li><a href="{{appUrl}}/EmailPreview/QuestFinalized">Quest Finalized</a></li>
              <li><a href="{{appUrl}}/EmailPreview/QuestDateChanged">Quest Date Changed</a></li>
              <li><a href="{{appUrl}}/EmailPreview/SessionReminder">Session Reminder</a></li>
            </ul></body></html>
            """;
        return Content(html, "text/html");
    }

    [HttpGet]
    public async Task<IActionResult> QuestFinalized()
    {
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var html = await emailRenderService.RenderAsync<Components.Emails.QuestFinalized>(new()
        {
            [nameof(Components.Emails.QuestFinalized.QuestTitle)] = "The Tomb of Annihilation",
            [nameof(Components.Emails.QuestFinalized.DmName)] = "Dungeon Master Theomund",
            [nameof(Components.Emails.QuestFinalized.QuestDate)] = DateTime.Today.AddDays(7),
            [nameof(Components.Emails.QuestFinalized.QuestDescription)] = "Deep in the jungles of Chult lies an ancient tomb that devours the souls of the dead. Your party must venture into the heart of darkness to stop the death curse before it claims you all.",
            [nameof(Components.Emails.QuestFinalized.ConfirmedPlayerNames)] = SamplePlayers,
            [nameof(Components.Emails.QuestFinalized.QuestUrl)] = $"{appUrl}/Quest",
            [nameof(Components.Emails.QuestFinalized.ChallengeRating)] = 9,
            [nameof(Components.Emails.QuestFinalized.AppUrl)] = appUrl,
        });
        return Content(html, "text/html");
    }

    [HttpGet]
    public async Task<IActionResult> QuestDateChanged()
    {
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var html = await emailRenderService.RenderAsync<Components.Emails.QuestDateChanged>(new()
        {
            [nameof(Components.Emails.QuestDateChanged.QuestTitle)] = "The Tomb of Annihilation",
            [nameof(Components.Emails.QuestDateChanged.DmName)] = "Dungeon Master Theomund",
            [nameof(Components.Emails.QuestDateChanged.OldDate)] = DateTime.Today.AddDays(7),
            [nameof(Components.Emails.QuestDateChanged.NewDate)] = DateTime.Today.AddDays(14),
            [nameof(Components.Emails.QuestDateChanged.QuestUrl)] = $"{appUrl}/Quest",
            [nameof(Components.Emails.QuestDateChanged.AppUrl)] = appUrl,
        });
        return Content(html, "text/html");
    }

    [HttpGet]
    public async Task<IActionResult> SessionReminder()
    {
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var html = await emailRenderService.RenderAsync<Components.Emails.SessionReminder>(new()
        {
            [nameof(Components.Emails.SessionReminder.QuestTitle)] = "The Tomb of Annihilation",
            [nameof(Components.Emails.SessionReminder.DmName)] = "Dungeon Master Theomund",
            [nameof(Components.Emails.SessionReminder.QuestDate)] = DateTime.Today.AddDays(1),
            [nameof(Components.Emails.SessionReminder.QuestDescription)] = "Deep in the jungles of Chult lies an ancient tomb that devours the souls of the dead. Your party must venture into the heart of darkness to stop the death curse before it claims you all.",
            [nameof(Components.Emails.SessionReminder.ConfirmedPlayerNames)] = SamplePlayers,
            [nameof(Components.Emails.SessionReminder.QuestUrl)] = $"{appUrl}/Quest",
            [nameof(Components.Emails.SessionReminder.ChallengeRating)] = 9,
            [nameof(Components.Emails.SessionReminder.AppUrl)] = appUrl,
        });
        return Content(html, "text/html");
    }
}
