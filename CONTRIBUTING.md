# Contributing to Cursus

Welcome to the Cursus project! This guide will help you get started as a contributor and keep our codebase clean and consistent.

---

## Development Setup

### Prerequisites

Make sure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB is fine for development)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code with C# extension
- [Git](https://git-scm.com/)

### First-Time Setup

```bash
# Clone the repo
git clone https://github.com/3bdo-Yahya/Cursus.git
cd Cursus/src

# Restore NuGet packages
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the project
dotnet run
```

---

## Implementation Workflow (Gitflow Lite)

We use a customized **Gitflow Lite** workflow adapted for our sprint structure, consisting of three main branch types: `master`, `develop`, and sprint-specific `release/` branches.

### Branch Flow

1. **Feature to Develop:** Developers create `feature/*` or `fix/*` branches dynamically from `develop`. When finished, they open a Pull Request (PR) to merge back into `develop`.
2. **Develop to Sprint:** At the end of the sprint, we create a `release/sprint-X` branch from `develop`. We perform final integration testing and bug fixes exclusively here to keep `develop` unblocked for the next sprint.
3. **Sprint to Master:** Once the sprint release is stable, we merge `release/sprint-X` into `master` (and back into `develop` if any bug fixes were made during the release phase).

```text
master (production-ready demos)
  └── release/sprint-1 (sprint finalization)
        └── develop (active integration branch)
              ├── feature/auth-system
              ├── feature/course-crud
              └── fix/gpa-calculation-bug
```

---

## Best Practices & Pull Requests

To keep our codebase healthy and prevent "broken" code from blocking teammates, follow these best practices:

1. **Protection Rules for `develop`:** 
   We enforce strict branch protection on `develop`. You cannot merge a PR unless it has:
   - At least one **approved peer review**.
   - A passing build (CI check). Our CI pipeline ([`.github/workflows/dotnet-ci.yml`](.github/workflows/dotnet-ci.yml)) automatically builds the code to catch errors.
   - *Never push directly to `master` or `develop`.*
2. **Small PRs:** 
   Encourage the team to keep feature branches small. This makes code reviews faster, reduces cognitive load, and minimizes painful merge conflicts in the Cursus project.
3. **Squash Merges:** 
   When merging `feature/*` branches into `develop`, always use **Squash and Merge**. This keeps the `develop` history clean by combining many small, noisy commits into one descriptive, atomic commit.
4. **Delete Branches:** 
   Delete your `feature/*` branches immediately after they are merged. This keeps the repository tidy and prevents confusion.
5. **Always pull before starting:**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature-name
   ```

### PR Checklist

Before submitting, make sure:

- [ ] The code compiles without errors (`dotnet build`)
- [ ] The app runs and the feature works as expected (`dotnet run`)
- [ ] No hardcoded values — use configuration files or constants
- [ ] Views follow the shared layout and use Bootstrap components
- [ ] Controller actions have `[Authorize]` attributes where needed
- [ ] No `bin/`, `obj/`, `.vs/`, or IDE files are included in the commit

---

## Code Conventions

### C# / Backend

- **Naming:** PascalCase for classes, methods, and properties. camelCase for local variables and parameters.
- **Controllers:** Keep thin — delegate logic to service classes.
- **Services:** All business logic lives in `Services/` folder. One service per feature domain (e.g., `GpaService`, `ImpactAnalysisService`).
- **Models:**
  - `Models/Entities/` — EF Core database entities
  - `Models/ViewModels/` — View-specific models passed to Razor views
- **Don't put logic in views.** If you need conditional logic, compute it in the controller/service and pass a clean ViewModel.

### Razor Views

- Use the shared `_Layout.cshtml` for all pages.
- Use **partial views** for reusable components (badges, progress bars, cards).
- Use `asp-for` and `asp-validation-for` tag helpers for forms.
- Use **Bootstrap 5** classes for layout and components — don't reinvent the wheel.

### JavaScript

- Keep JS minimal — only for interactive components (graph, chat, GPA calculator).
- Place JS files in `wwwroot/js/`.
- Use `jQuery` (bundled with MVC) or vanilla JS — no npm packages.
- For AJAX calls, use `$.ajax()` or `fetch()` consistently (pick one, don't mix).

### Database

- **Never modify the database directly.** Always use EF Core migrations:
  ```bash
  dotnet ef migrations add MigrationName
  dotnet ef database update
  ```
- Name migrations descriptively: `AddCoursePrerequisiteTable`, `UpdateStudentStanding`.

---

## Commit Messages

Write clear, concise commit messages:

```
✅ Good:
  "Add course CRUD operations to AdminController"
  "Fix GPA calculation rounding error"
  "Implement prerequisite graph visualization with Cytoscape.js"

❌ Bad:
  "Update files"
  "Fix stuff"
  "WIP"
```

**Format:** Start with a verb in imperative mood (Add, Fix, Implement, Update, Remove, Refactor).

---

## Project Architecture

```
src/
├── Controllers/         # HTTP handling + view rendering
│   ├── AccountController    → Auth (login, register, logout)
│   ├── AdminController      → Course/student/requirement CRUD
│   ├── StudentController    → Student dashboard
│   ├── CourseMapController  → Prerequisite graph
│   ├── ImpactController     → Cascade analysis
│   ├── ProgressController   → Progress tracker + GPA simulator
│   └── AiAdvisorController  → AI chat
│
├── Services/            # Business logic (the brain)
│   ├── CourseService
│   ├── PrerequisiteService
│   ├── ImpactAnalysisService
│   ├── GpaService
│   ├── ProgressService
│   ├── StandingService
│   └── AiAdvisorService
│
├── Models/
│   ├── Entities/        # EF Core entities (map to DB tables)
│   └── ViewModels/      # Data passed to Razor views
│
├── Data/
│   ├── AppDbContext.cs
│   ├── Migrations/
│   └── SeedData/        # Initial data (courses, prerequisites)
│
├── Views/               # Razor views (organized by controller)
│   ├── Shared/          # _Layout, partials
│   ├── Account/
│   ├── Admin/
│   ├── Student/
│   └── ...
│
└── wwwroot/
    ├── css/             # Custom styles
    ├── js/              # Cytoscape.js, custom scripts
    └── lib/             # Bootstrap, jQuery (bundled)
```

### Key Principle: Controllers → Services → Data

- **Controllers** handle HTTP, call services, return views
- **Services** contain ALL business logic
- **Data** is EF Core only — no raw SQL

---

## Feature Ownership

Each team member owns specific modules. If you need to change code in someone else's module, **coordinate with them first** (or tag them as a reviewer on the PR).

| Module | Controller(s) | Owned By |
|---|---|---|
| Auth & Profile | `AccountController` | Member 2 |
| Admin Panel | `AdminController` | Members 1 & 2 |
| Course Map | `CourseMapController` | Member 3 |
| Impact Analyzer | `ImpactController` | Members 1 & 3 |
| Progress & GPA | `ProgressController` | Member 4 |
| AI Advisor | `AiAdvisorController` | Members 4 & 5 |
| Data & Seeding | `Data/SeedData/` | Member 5 |
| Testing & Deployment | — | Member 6 |

---

## Definition of Done

A feature is ready for PR when:

- [ ] Controller actions and service logic are implemented
- [ ] Razor views render correctly with proper layout and styling
- [ ] Forms have both client-side and server-side validation
- [ ] Edge cases handled (empty states, errors, unauthorized access)
- [ ] `dotnet build` compiles without errors or warnings
- [ ] Feature is manually tested and works end-to-end
- [ ] Code follows the conventions in this document

---

## Communication

| Channel | Purpose |
|---|---|
| **ClickUp** | Sprint tracking, ticket management, SDLC documents |
| **WhatsApp** | Daily async standups, quick questions |
| **GitHub PRs** | Code reviews, technical discussions |
| **Weekly sync** | Sprint planning every 2 weeks |

---

## Need Help?

- **Stuck on a bug?** Post in the team chat with: what you tried, what error you see, and which file/line.
- **Unsure about architecture?** Ask the Team Lead before building — it's cheaper to discuss than to rewrite.
- **Found a bug you can't fix?** Create a GitHub Issue with steps to reproduce.

---

*Let's build something great together. 🚀*
