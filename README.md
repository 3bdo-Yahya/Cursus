# Cursus

**Your AI-Powered Academic Advisor & Smart Graduation Planner**

> DEPI Graduation Project — Full-Stack Web Development (.NET)

---

## What is Cursus?

Cursus is a web platform that helps university students **see, understand, and plan** their academic journey. It models the full prerequisite dependency chain of a degree program and uses it to provide real-time impact analysis, graduation tracking, and AI-powered academic guidance.

**The core idea:** When a student fails a course, the consequences are rarely obvious. That failed course may be a prerequisite for two or three upcoming courses — silently collapsing the student's plan. Cursus makes those consequences **instantly visible** and provides a clear recovery path.

---

## Key Features

### 🗺️ Interactive Course Map
A visual, interactive graph of every course in the student's department. Courses are connected by prerequisite relationships and color-coded by status (completed, in-progress, remaining, blocked). Built with Cytoscape.js for smooth, zoomable graph interactions.

### ⚡ Impact Analyzer
The core differentiator. When a student fails or drops a course, the system performs a **forward cascade analysis**: it instantly identifies every downstream course that is now blocked, calculates the graduation delay, and suggests a recovery path with animated visualizations.

### 📊 Progress & Planning
A graduation audit tracking progress by course category (core, elective, free, university requirements), with GPA simulation — predict grades, set targets, and see exactly what you need to hit your goals. Includes grade improvement tracking and academic standing alerts.

### 🤖 AI Advisor
A chat interface powered by OpenAI's GPT where students ask natural-language questions about their academic situation. The AI uses the student's real data as context, so responses are personalized and data-driven.

> **Design principle:** AI is the *voice*, not the brain. All core logic (prerequisite resolution, GPA calculation, graduation audit) is handled by deterministic algorithms. AI translates the results into supportive, human-readable guidance.

### 🎓 Academic Rules Engine
Models standard credit-hour system rules: academic standing (Good → Warning → Probation → Dismissal), credit hour limits by GPA, grade improvement eligibility, and honor recognition (Honor / Dean's / President's List).

---

## Tech Stack

| Layer          | Technology                                  |
|----------------|---------------------------------------------|
| Application    | ASP.NET Core 10 MVC, C#, Razor Views       |
| Styling        | Bootstrap 5 + custom CSS                    |
| Graph Viz      | Cytoscape.js (interactive prerequisite map) |
| Database       | SQL Server + Entity Framework Core          |
| Auth           | ASP.NET Identity (cookie-based)             |
| AI             | OpenAI API (GPT-3.5-turbo)                  |

---

## Project Structure

```
Cursus/
├── src/                # ASP.NET Core MVC application
│   ├── Controllers/    # MVC controllers
│   ├── Services/       # Business logic layer
│   ├── Models/         # Entities + View Models
│   ├── Data/           # EF Core DbContext + migrations
│   ├── Views/          # Razor views
│   └── wwwroot/        # Static files (CSS, JS, Cytoscape.js)
├── .gitignore
├── CONTRIBUTING.md
├── LICENSE.txt
└── README.md
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB or Express)
- A code editor (Visual Studio 2022 or VS Code)

### Run Locally

```bash
# Clone the repository
git clone https://github.com/3bdo-Yahya/Cursus.git
cd Cursus/src

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
```

The app will be available at `https://localhost:5001` (or the port shown in the terminal).

---

## Team

| Role                       | Responsibilities                                        |
|----------------------------|---------------------------------------------------------|
| Team Lead + Architecture   | Architecture, prerequisite engine, impact analyzer      |
| Backend Developer          | Auth, admin module, GPA calculation services            |
| Full-Stack Developer       | Dashboard, course map (Cytoscape.js), impact animation  |
| Full-Stack Developer       | Progress tracker, GPA simulator, admin panel, chat UI   |
| Data & AI Integration      | Data seeding, OpenAI integration, prompt engineering    |
| QA & DevOps                | Testing, deployment, documentation, demo preparation    |

---

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for our development workflow, branching strategy, and coding conventions.

---

## License

This project is licensed under the terms specified in [LICENSE.txt](LICENSE.txt).

---

*DEPI Graduation Project 2026 · Full-Stack Web Development (.NET)*
