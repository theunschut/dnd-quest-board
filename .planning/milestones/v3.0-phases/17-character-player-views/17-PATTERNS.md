# Phase 17: Character & Player Views - Pattern Map

**Mapped:** 2026-06-25
**Files analyzed:** 9
**Analogs found:** 9 / 9

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `EuphoriaInn.Service/Views/GuildMembers/Details.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` + `Index.Mobile.cshtml` | exact |
| `EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/GuildMembers/Create.cshtml` | exact |
| `EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml` | exact |
| `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml` | view | request-response | `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` | role-match |
| `EuphoriaInn.Service/wwwroot/css/character-detail.mobile.css` | CSS | — | `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` | exact |
| `EuphoriaInn.Service/wwwroot/css/character-form.mobile.css` | CSS | — | `EuphoriaInn.Service/wwwroot/css/account.mobile.css` | role-match |
| `EuphoriaInn.Service/wwwroot/css/players.mobile.css` | CSS | — | `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` | exact |
| `EuphoriaInn.Service/Views/Players/Index.cshtml` | view (modify) | request-response | self | exact |
| `.planning/REQUIREMENTS.md` | docs | — | existing file | exact |

---

## Pattern Assignments

### `EuphoriaInn.Service/Views/GuildMembers/Details.Mobile.cshtml` (view, request-response)

**Analog 1:** `EuphoriaInn.Service/Views/GuildMembers/Details.cshtml` — ViewModel bindings, portrait URL, class badges, action forms
**Analog 2:** `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` — mobile shell, `@section Styles`, glass card pattern

**Section Styles injection** (from `Index.Mobile.cshtml` lines 5-7):
```cshtml
@section Styles {
    <link href="~/css/character-detail.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Model directive and local vars** (from `Details.cshtml` lines 1-7):
```cshtml
@using EuphoriaInn.Domain.Enums
@using EuphoriaInn.Service.ViewModels.CharacterViewModels
@model CharacterViewModel
@{
    ViewData["Title"] = Model.Name;
    var isOwner = Model.IsOwner;
    var isRetired = Model.Status == CharacterStatus.Retired;
}
```

**Portrait URL pattern** (from `Details.cshtml` lines 16-26):
```cshtml
@if (Model.ProfilePicture != null)
{
    <img src="@Url.Action("GetProfilePicture", new { id = Model.Id })" alt="@Model.Name" class="character-portrait-mobile img-fluid" style="max-height: 220px; width: auto;" />
}
else
{
    <div class="character-portrait-placeholder mb-3">
        <i class="fas fa-user fa-5x text-muted"></i>
    </div>
}
```

**Status and role badges** (from `Details.cshtml` lines 31-49):
```cshtml
@if (Model.Status == CharacterStatus.Retired)
{
    <span class="badge bg-secondary fs-6">
        <i class="fas fa-moon me-1"></i>Retired
    </span>
}
else
{
    <span class="badge bg-success fs-6">
        <i class="fas fa-check-circle me-1"></i>Active
    </span>
}
@if (Model.Role == CharacterRole.Main)
{
    <span class="badge bg-warning fs-6">
        <i class="fas fa-star me-1"></i>Main Character
    </span>
}
```

**Class badges loop** (from `Details.cshtml` lines 121-136):
```cshtml
@if (Model.Classes.Any())
{
    <div class="class-list">
        @foreach (var charClass in Model.Classes)
        {
            <span class="badge bg-primary fs-6 me-2 mb-2">
                @charClass.Class Level @charClass.ClassLevel
            </span>
        }
    </div>
}
else
{
    <p class="form-control-plaintext text-muted">No classes defined</p>
}
```

**Sheet link pattern** (from `Details.cshtml` lines 103-116):
```cshtml
@if (!string.IsNullOrEmpty(Model.SheetLink))
{
    <p class="form-control-plaintext">
        <a href="@Model.SheetLink" target="_blank" class="text-break">
            @Model.SheetLink <i class="fas fa-external-link-alt ms-1"></i>
        </a>
    </p>
}
else
{
    <p class="form-control-plaintext text-muted">Not provided</p>
}
```

**Owner action buttons — all three** (from `Details.cshtml` lines 61-86):
```cshtml
<a href="@Url.Action("Edit", new { id = Model.Id })" class="btn btn-warning w-100 mb-2">
    <i class="fas fa-edit me-2"></i>Edit Character
</a>
<form asp-action="ToggleRetirement" method="post" class="mb-2">
    <input type="hidden" name="id" value="@Model.Id" />
    @if (isRetired)
    {
        <button type="submit" class="btn btn-success w-100">
            <i class="fas fa-undo me-2"></i>Reactivate Character
        </button>
    }
    else
    {
        <button type="submit" class="btn btn-secondary w-100">
            <i class="fas fa-moon me-2"></i>Retire Character
        </button>
    }
</form>
<form asp-action="Delete" method="post" onsubmit="return confirm('Are you sure you want to delete this character? This action cannot be undone.');">
    <input type="hidden" name="id" value="@Model.Id" />
    <button type="submit" class="btn btn-danger w-100">
        <i class="fas fa-trash me-2"></i>Delete Character
    </button>
</form>
```

**Glass card container** (from `guild-members.mobile.css` lines 3-11):
```cshtml
<div class="character-detail-card mb-3">
    <!-- glass card class defined in character-detail.mobile.css using same values as .guild-section-card -->
</div>
```

**Back navigation** (from `Details.cshtml` lines 156-159):
```cshtml
<a href="@Url.Action("Index")" class="btn btn-secondary">
    <i class="fas fa-arrow-left me-2"></i>Back to Guild Members
</a>
```

---

### `EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/GuildMembers/Create.cshtml`

**Model directive** (from `Create.cshtml` lines 1-8, note: the `isEdit` detection block is desktop-only — Create.Mobile.cshtml sets a fixed title):
```cshtml
@using EuphoriaInn.Domain.Enums
@using EuphoriaInn.Service.ViewModels.CharacterViewModels
@model CharacterViewModel
@{
    ViewData["Title"] = "Create New Character";
}
```

**Section Styles injection** (follow `Index.Mobile.cshtml` pattern):
```cshtml
@section Styles {
    <link href="~/css/character-form.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Form open + hidden fields** (from `Create.cshtml` lines 18-24):
```cshtml
<form asp-action="Create" method="post" enctype="multipart/form-data">
    <input type="hidden" asp-for="OwnerId" />
    <div asp-validation-summary="All" class="text-danger mb-3"></div>
```

**Profile picture upload block** (from `Create.cshtml` lines 28-40 — move to top, full-width `col-12` on mobile):
```cshtml
<label asp-for="ProfilePictureFile" class="form-label">Profile Picture</label>
<input type="file" asp-for="ProfilePictureFile" class="form-control" accept="image/*" id="profilePictureInput" />
<small class="form-text text-muted">Upload a character portrait (JPG, PNG, GIF - Max 5 MB)</small>
<span asp-validation-for="ProfilePictureFile" class="text-danger"></span>
<div id="fileSizeError" class="text-danger" style="display: none;"></div>
```

**Character name field** (from `Create.cshtml` lines 43-47):
```cshtml
<label asp-for="Name" class="form-label">Character Name <span class="text-danger">*</span></label>
<input asp-for="Name" class="form-control" placeholder="Enter character name" />
<span asp-validation-for="Name" class="text-danger"></span>
```

**Level, Status, Role fields** (from `Create.cshtml` lines 49-68 — each field becomes `col-12` full-width on mobile):
```cshtml
<label asp-for="Level" class="form-label">Level <span class="text-danger">*</span></label>
<input asp-for="Level" type="number" class="form-control" min="1" max="20" />
<span asp-validation-for="Level" class="text-danger"></span>

<label asp-for="Status" class="form-label">Status</label>
<select asp-for="Status" asp-items="Html.GetEnumSelectList<CharacterStatus>()" class="form-select"></select>

<label asp-for="Role" class="form-label">Role</label>
<select asp-for="Role" asp-items="Html.GetEnumSelectList<CharacterRole>()" class="form-select"></select>
<small class="form-text text-muted">Set as Main to make this your primary character</small>
```

**Class entries — stacked layout** (from `Create.cshtml` lines 75-108, cols changed to `col-12`):
```cshtml
<div id="class-list">
    @for (var i = 0; i < (Model.Classes.Any() ? Model.Classes.Count : 1); i++)
    {
        <div class="class-entry mb-2" data-index="@i">
            <div class="row g-2">
                <div class="col-12">
                    <select name="Classes[@i].Class" class="form-select" required>
                        <option value="">Select Class</option>
                        @foreach (DndClass dndClass in Enum.GetValues(typeof(DndClass)))
                        {
                            <option value="@((int)dndClass)" selected="@(Model.Classes.Count > i && Model.Classes[i].Class == dndClass)">@dndClass</option>
                        }
                    </select>
                </div>
                <div class="col-12">
                    <input type="number" name="Classes[@i].ClassLevel" class="form-control"
                           value="@(Model.Classes.Count > i ? Model.Classes[i].ClassLevel : 1)"
                           placeholder="Level" min="1" max="20" required />
                </div>
                @if (i > 0)
                {
                    <div class="col-12">
                        <button type="button" class="btn btn-danger w-100 remove-class">
                            <i class="fas fa-trash"></i> Remove
                        </button>
                    </div>
                }
            </div>
        </div>
    }
</div>
```

**SheetLink, Description, Backstory fields** (from `Create.cshtml` lines 110-127 — copy verbatim):
```cshtml
<label asp-for="SheetLink" class="form-label">Character Sheet Link</label>
<input asp-for="SheetLink" type="url" class="form-control" placeholder="https://..." />
<span asp-validation-for="SheetLink" class="text-danger"></span>

<label asp-for="Description" class="form-label">Description</label>
<textarea asp-for="Description" class="form-control" rows="3" placeholder="Brief character description"></textarea>
<span asp-validation-for="Description" class="text-danger"></span>

<label asp-for="Backstory" class="form-label">Backstory</label>
<textarea asp-for="Backstory" class="form-control" rows="5" placeholder="Character backstory"></textarea>
<span asp-validation-for="Backstory" class="text-danger"></span>
```

**Submit + Cancel buttons** (from `Create.cshtml` lines 129-137):
```cshtml
<div class="d-flex gap-2">
    <button type="submit" class="btn btn-warning">
        <i class="fas fa-plus me-2"></i>Create Character
    </button>
    <a href="@Url.Action("Index")" class="btn btn-secondary">
        <i class="fas fa-times me-2"></i>Cancel
    </a>
</div>
```

**Section Scripts — JS block** (from `Create.cshtml` lines 143-217 — copy verbatim; update `innerHTML` template cols to `col-12`):
```cshtml
@section Scripts {
    <script>
        let classIndex = @(Model.Classes.Any() ? Model.Classes.Count : 1);

        const MAX_FILE_SIZE = 5 * 1024 * 1024;
        const ALLOWED_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];

        document.getElementById('profilePictureInput')?.addEventListener('change', function(e) {
            /* ... file size + type validation — copy verbatim from Create.cshtml lines 151-177 ... */
        });

        document.getElementById('add-class').addEventListener('click', function() {
            const classListDiv = document.getElementById('class-list');
            const newEntry = document.createElement('div');
            newEntry.className = 'class-entry mb-2';
            newEntry.setAttribute('data-index', classIndex);
            newEntry.innerHTML = `
                <div class="row g-2">
                    <div class="col-12">
                        <select name="Classes[${classIndex}].Class" class="form-select" required>
                            <option value="">Select Class</option>
                            @foreach (DndClass dndClass in Enum.GetValues(typeof(DndClass)))
                            {
                                <text><option value="@((int)dndClass)">@dndClass</option></text>
                            }
                        </select>
                    </div>
                    <div class="col-12">
                        <input type="number" name="Classes[${classIndex}].ClassLevel" class="form-control"
                               value="1" placeholder="Level" min="1" max="20" required />
                    </div>
                    <div class="col-12">
                        <button type="button" class="btn btn-danger w-100 remove-class">
                            <i class="fas fa-trash"></i> Remove
                        </button>
                    </div>
                </div>
            `;
            classListDiv.appendChild(newEntry);
            classIndex++;
        });

        document.getElementById('class-list').addEventListener('click', function(e) {
            if (e.target.classList.contains('remove-class') || e.target.closest('.remove-class')) {
                const entry = e.target.closest('.class-entry');
                entry.remove();
            }
        });
    </script>
}
```

---

### `EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml`

**Model directive and title** (from `Edit.cshtml` lines 1-6):
```cshtml
@using EuphoriaInn.Domain.Enums
@using EuphoriaInn.Service.ViewModels.CharacterViewModels
@model CharacterViewModel
@{
    ViewData["Title"] = $"Edit {Model.Name}";
}
```

**Section Styles injection** (same file as Create.Mobile.cshtml — shared CSS):
```cshtml
@section Styles {
    <link href="~/css/character-form.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**Form open + hidden fields** (from `Edit.cshtml` lines 18-20 — Edit has both Id and OwnerId):
```cshtml
<form asp-action="Edit" method="post" enctype="multipart/form-data">
    <input type="hidden" asp-for="Id" />
    <input type="hidden" asp-for="OwnerId" />
```

**Existing portrait thumbnail** (from `Edit.cshtml` lines 27-31 — displayed above file input at top):
```cshtml
@if (Model.ProfilePicture != null)
{
    <div class="mb-2 text-center">
        <img src="@Url.Action("GetProfilePicture", new { id = Model.Id })" alt="Current" class="img-thumbnail" style="max-width: 200px;" />
    </div>
}
```

**Class entries — Edit variant** (from `Edit.cshtml` lines 73-103, cols changed to `col-12`; uses `classData` local var):
```cshtml
@for (var i = 0; i < (Model.Classes.Any() ? Model.Classes.Count : 1); i++)
{
    var classData = Model.Classes.Count > i ? Model.Classes[i] : new CharacterClassViewModel { ClassLevel = 1 };
    <div class="class-entry mb-2" data-index="@i">
        <div class="row g-2">
            <div class="col-12">
                <select name="Classes[@i].Class" class="form-select" required>
                    <option value="">Select Class</option>
                    @foreach (DndClass dndClass in Enum.GetValues(typeof(DndClass)))
                    {
                        <option value="@((int)dndClass)" selected="@(classData.Class == dndClass)">@dndClass</option>
                    }
                </select>
            </div>
            <div class="col-12">
                <input type="number" name="Classes[@i].ClassLevel" class="form-control"
                       value="@classData.ClassLevel" placeholder="Level" min="1" max="20" required />
            </div>
            @if (i > 0)
            {
                <div class="col-12">
                    <button type="button" class="btn btn-danger w-100 remove-class">
                        <i class="fas fa-trash"></i> Remove
                    </button>
                </div>
            }
        </div>
    </div>
}
```

**Cancel link points to Details** (from `Edit.cshtml` line 131 — differs from Create which goes to Index):
```cshtml
<a href="@Url.Action("Details", new { id = Model.Id })" class="btn btn-secondary">
    <i class="fas fa-times me-2"></i>Cancel
</a>
```

**Section Scripts — JS block** (from `Edit.cshtml` lines 140-214 — copy verbatim; update `innerHTML` template cols to `col-12`; note `classIndex` init differs: `let classIndex = @Model.Classes.Count;`):
```cshtml
@section Scripts {
    <script>
        let classIndex = @Model.Classes.Count;
        /* ... rest identical to Create variant with col-12 template ... */
    </script>
}
```

---

### `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` — mobile shell, glass card, tap-navigation rows
**Secondary analog:** `EuphoriaInn.Service/Views/Players/Index.cshtml` — ViewModel bindings (`Model.DungeonMasters`, `Model.Players`), DM profile link

**Model directive** (from `Players/Index.cshtml` lines 1-5):
```cshtml
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Service.ViewModels.GuildMembersViewModels
@model GuildMembersIndexViewModel
@{
    ViewData["Title"] = "Players";
}
```

**Section Styles injection** (follow `Index.Mobile.cshtml` pattern):
```cshtml
@section Styles {
    <link href="~/css/players.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

**DM tap-navigable row** (DM name → Profile page; pattern from `Index.Mobile.cshtml` line 21 + `Players/Index.cshtml` line 33):
```cshtml
<div class="d-flex align-items-center p-3 players-row"
     onclick="window.location.href='@Url.Action("Profile", "DungeonMaster", new { id = dm.Id })'">
    <span class="players-name">@dm.Name</span>
    <i class="fas fa-chevron-right ms-auto"></i>
</div>
```

**Player non-tappable row** (no profile page for players; from CONTEXT.md D-09):
```cshtml
<div class="d-flex align-items-center p-3 players-row">
    <span class="players-name">@player.Name</span>
</div>
```

**Empty-state pattern** (from `Index.Mobile.cshtml` lines 49-55 / `Players/Index.cshtml` lines 53-58):
```cshtml
<div class="guild-empty-state">
    <i class="fas fa-dragon fa-2x mb-2"></i>
    <h5>No Dungeon Masters Found</h5>
    <p>The guild registry is currently empty.</p>
</div>
```

**Section glass card shell** (from `Index.Mobile.cshtml` lines 13-14):
```cshtml
<div class="players-section-card mb-4">
    <div class="players-section-heading mb-2">
        <i class="fas fa-crown text-warning me-2"></i>Dungeon Masters
    </div>
    <!-- rows -->
</div>
```

---

### `EuphoriaInn.Service/wwwroot/css/character-detail.mobile.css` (CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css`

**File header comment** (from `guild-members.mobile.css` line 1):
```css
/* character-detail.mobile.css — loaded exclusively by GuildMembers/Details.Mobile.cshtml via _Layout.Mobile.cshtml; no media queries */
```

**Glass card container** (from `guild-members.mobile.css` lines 3-11 — rename selector to `.character-detail-card`):
```css
.character-detail-card {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 12px;
}
```

**Portrait sizing** (new rule, targeting ~220px guidance from CONTEXT.md D-02):
```css
.character-portrait-mobile {
    display: block;
    margin: 0 auto;
    max-height: 220px;
    width: auto;
    border-radius: 8px;
}
```

**Portrait placeholder** (follow `guild-members.mobile.css` lines 52-63 pattern):
```css
.character-portrait-placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    height: 120px;
    color: rgba(244, 228, 188, 0.7);
}
```

**Parchment heading** (from `guild-members.mobile.css` lines 14-20):
```css
.character-detail-heading {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
    text-transform: uppercase;
    font-size: 0.875rem;
    letter-spacing: 0.05em;
}
```

**Parchment name and labels** (from `guild-members.mobile.css` lines 65-72):
```css
.character-name-mobile {
    color: #F4E4BC !important;
    text-shadow: 1px 1px 3px rgba(0,0,0,0.9) !important;
    font-size: 1.25rem;
    line-height: 1.3;
}

.character-detail-card .form-label {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}
```

**Faded parchment for secondary text** (from `guild-members.mobile.css` lines 73-79):
```css
.character-detail-card .text-muted,
.character-detail-card small,
.character-info-value {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0,0,0,0.9) !important;
}
```

**Badge text-shadow suppression** (from `guild-members.mobile.css` lines 106-109):
```css
.character-detail-card .badge {
    text-shadow: none !important;
}
```

---

### `EuphoriaInn.Service/wwwroot/css/character-form.mobile.css` (CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/account.mobile.css`

**File header comment** (follow `account.mobile.css` line 1-2 pattern):
```css
/* character-form.mobile.css — loaded exclusively by GuildMembers/Create.Mobile.cshtml and Edit.Mobile.cshtml via _Layout.Mobile.cshtml; no media queries */
```

**Glass card container** (from `account.mobile.css` lines 5-12 — rename to `.character-form-card`):
```css
.character-form-card {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 16px;
}
```

**Parchment form labels and headings** (from `account.mobile.css` lines 14-21):
```css
.character-form-card h5,
.character-form-card h6,
.character-form-card .form-label {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
}
```

**Faded parchment for hints** (from `account.mobile.css` lines 33-38):
```css
.character-form-card .form-text,
.character-form-card small {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0, 0, 0, 0.9) !important;
}
```

**text-muted scoped override** (from `account.mobile.css` lines 47-50):
```css
.character-form-card .text-muted {
    color: rgba(244, 228, 188, 0.7) !important;
}
```

**Badge text-shadow suppression** (from `account.mobile.css` lines 52-55):
```css
.character-form-card .badge {
    text-shadow: none !important;
}
```

**Stacked class-entry layout** (new rule; implements CONTEXT.md D-06 — col-12 stack for class select + level + remove):
```css
.class-entry .row {
    row-gap: 0.5rem;
}

.class-entry .btn-danger {
    min-height: 44px;
}
```

**Profile picture file input block** (new rule; full-width touch target):
```css
.character-form-card input[type="file"] {
    display: block;
    width: 100%;
}
```

---

### `EuphoriaInn.Service/wwwroot/css/players.mobile.css` (CSS)

**Analog:** `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css`

**File header comment**:
```css
/* players.mobile.css — loaded exclusively by Players/Index.Mobile.cshtml via _Layout.Mobile.cshtml; no media queries */
```

**Section glass card** (from `guild-members.mobile.css` lines 3-11 — rename to `.players-section-card`):
```css
.players-section-card {
    background: rgba(255, 255, 255, 0.15);
    backdrop-filter: blur(15px);
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 12px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
    padding: 12px;
}
```

**Section heading — parchment uppercase** (from `guild-members.mobile.css` lines 13-20):
```css
.players-section-heading {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
    text-transform: uppercase;
    font-size: 0.875rem;
    letter-spacing: 0.05em;
}
```

**List row — tappable with divider** (from `guild-members.mobile.css` lines 22-37):
```css
.players-row {
    padding: 12px 0;
    border-bottom: 1px solid rgba(139, 69, 19, 0.3);
    cursor: pointer;
}

.players-row:last-child {
    border-bottom: none;
    padding-bottom: 0;
}

.players-row:active {
    background-color: rgba(255, 255, 255, 0.05);
    border-radius: 4px;
}

/* Non-tappable player row — no cursor */
.players-row.no-link {
    cursor: default;
}
```

**Player name — parchment** (from `guild-members.mobile.css` lines 65-72):
```css
.players-name {
    color: #F4E4BC !important;
    text-shadow: 1px 1px 3px rgba(0,0,0,0.9) !important;
    font-size: 1rem;
    line-height: 1.3;
}
```

**Empty state** (from `guild-members.mobile.css` lines 81-99):
```css
.players-empty-state {
    text-align: center;
    padding: 16px;
}

.players-empty-state i {
    color: rgba(244, 228, 188, 0.7);
}

.players-empty-state h5 {
    color: #F4E4BC !important;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.9);
}

.players-empty-state p {
    color: rgba(244, 228, 188, 0.7) !important;
    text-shadow: 1px 1px 3px rgba(0,0,0,0.9) !important;
}
```

**text-muted override inside section card** (from `guild-members.mobile.css` lines 101-104):
```css
.players-section-card .text-muted {
    color: rgba(244, 228, 188, 0.7) !important;
}
```

---

### `EuphoriaInn.Service/Views/Players/Index.cshtml` (view, modify — remove email column)

**Analog:** self (`Players/Index.cshtml`)

**Elements to remove — DM table** (lines 25-26 in `<thead>`, lines 36-45 in `<tbody>`):

Remove `<th>` (line 25):
```cshtml
<th scope="col" class="w-50">Email</th>
```

Remove `<td>` block (lines 36-45):
```cshtml
<td>
    @if (!string.IsNullOrEmpty(dm.Email))
    {
        <a href="mailto:@dm.Email" class="email-link">@dm.Email</a>
    }
    else
    {
        <span class="text-muted fst-italic">No email provided</span>
    }
</td>
```

**Elements to remove — Player table** (lines 78-79 in `<thead>`, lines 87-96 in `<tbody>`):

Remove `<th>` (line 79):
```cshtml
<th scope="col" class="w-50">Email</th>
```

Remove `<td>` block (lines 87-96):
```cshtml
<td>
    @if (!string.IsNullOrEmpty(player.Email))
    {
        <a href="mailto:@player.Email" class="email-link">@player.Email</a>
    }
    else
    {
        <span class="text-muted fst-italic">No email provided</span>
    }
</td>
```

**Post-edit Name-only `<th>` — update `w-50` to `w-100`** (both tables):
```cshtml
<th scope="col">Name</th>
```

---

### `.planning/REQUIREMENTS.md` (docs — add CHAR-01, CHAR-02, CHAR-03, PLAYER-01)

**Analog:** existing `.planning/REQUIREMENTS.md` — read existing structure before appending Phase 17 entries.

New requirement IDs to add, derived from CONTEXT.md and ROADMAP.md Phase 17 success criteria:
- `CHAR-01` — Mobile character detail view (`GuildMembers/Details.Mobile.cshtml`)
- `CHAR-02` — Mobile character create form (`GuildMembers/Create.Mobile.cshtml`)
- `CHAR-03` — Mobile character edit form (`GuildMembers/Edit.Mobile.cshtml`)
- `PLAYER-01` — Mobile players list + desktop email removal (`Players/Index.Mobile.cshtml` + `Players/Index.cshtml`)

---

## Shared Patterns

### Glass Card CSS (all mobile CSS files)
**Source:** `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` lines 3-11
**Apply to:** `character-detail.mobile.css`, `character-form.mobile.css`, `players.mobile.css`
```css
background: rgba(255, 255, 255, 0.15);
backdrop-filter: blur(15px);
border: 1px solid rgba(255, 255, 255, 0.3);
border-radius: 12px;
box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
```

### Parchment Text
**Source:** `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` lines 15-16
**Apply to:** all CSS files, primary text elements
```css
color: #F4E4BC !important;
text-shadow: 2px 2px 4px rgba(0,0,0,0.9), -1px -1px 2px rgba(0,0,0,0.9);
```

### Faded Parchment (secondary/hint text)
**Source:** `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` lines 73-79
**Apply to:** all CSS files, `.text-muted`, `small`, helper text
```css
color: rgba(244, 228, 188, 0.7) !important;
text-shadow: 1px 1px 3px rgba(0,0,0,0.9) !important;
```

### Section Styles CSS injection
**Source:** `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` lines 5-7
**Apply to:** all four new mobile `.cshtml` files
```cshtml
@section Styles {
    <link href="~/css/{page}.mobile.css" asp-append-version="true" rel="stylesheet" />
}
```

### No `@Layout` directive in mobile views
**Source:** `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` (no Layout line)
**Apply to:** all four new mobile `.cshtml` files
`_ViewStart.cshtml` handles layout selection automatically — mobile views must NOT set `Layout`.

### Tap-navigation row onclick pattern
**Source:** `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` line 21
**Apply to:** `Players/Index.Mobile.cshtml` DM rows, `Details.Mobile.cshtml` back navigation
```cshtml
onclick="window.location.href='@Url.Action("ActionName", "ControllerName", new { id = item.Id })'"
```

### Row divider inside glass card
**Source:** `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` lines 23-37
**Apply to:** `players.mobile.css` row rules
```css
border-bottom: 1px solid rgba(139, 69, 19, 0.3);
```

---

## No Analog Found

All files have close analogs in the codebase.

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Views/GuildMembers/`, `EuphoriaInn.Service/Views/Players/`, `EuphoriaInn.Service/wwwroot/css/`
**Files scanned:** 8
**Pattern extraction date:** 2026-06-25
