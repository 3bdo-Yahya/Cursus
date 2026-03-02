# Cursus

**Your AI-Powered Academic Advisor & Smart Graduation Planner**

> DEPI Graduation Project — Full-Stack Web Development (.NET & React)

---

## What is Cursus?

Cursus is a web platform that helps university students **see, understand, and plan** their academic journey. It models the full prerequisite dependency chain of a degree program and uses it to provide real-time impact analysis, graduation tracking, and AI-powered academic guidance.

**The core idea:** When a student fails a course, the consequences are rarely obvious. That failed course may be a prerequisite for two or three upcoming courses — silently collapsing the student's plan. Cursus makes those consequences **instantly visible** and provides a clear recovery path.

---

## Key Features

### 1. Interactive Course Map
A visual, interactive graph of every course in the student's department. Courses are connected by prerequisite relationships and color-coded by status (completed, in-progress, remaining, blocked). Students see their entire degree at a glance.

### 2. Impact Analyzer ⚡
The core differentiator. When a student fails or drops a course, the system performs a **forward cascade analysis**: it instantly identifies every downstream course that is now blocked, calculates how many semesters are affected, and suggests a recovery path — including when to retake the course and when blocked courses will unlock. The Course Map animates the cascade in real-time.

### 3. Progress & Planning
A graduation audit that tracks the student's progress by course category (core, department elective, free elective, university requirement). It shows credits completed, courses remaining, projected graduation date, and warns if a student is at risk of not meeting graduation GPA requirements.

Includes a **GPA Simulator** where students can predict grades for current courses, set GPA targets, and see what grades they need to hit those targets. Courses eligible for grade improvement are flagged.

### 4. AI Advisor 🤖
A chat interface powered by OpenAI's GPT where students ask natural-language questions about their academic situation. The AI uses the student's real data (courses, grades, impact analysis results) as context, so responses are personalized and data-driven — not generic advice.

> **Design principle:** AI is the *voice*, not the brain. All core logic (prerequisite resolution, GPA calculation, graduation audit) is handled by deterministic algorithms. AI translates the results into supportive, human-readable guidance.

### 5. Academic Rules Engine
The platform models standard credit-hour system rules:
- **Academic Standing** — Good Standing, Warning, Probation, and Dismissal based on GPA thresholds
- **Credit Hour Limits** — overload eligibility for high-GPA students, restrictions for probation students
- **Grade Improvement** — flags courses eligible for retake and shows the GPA impact
- **Honor Recognition** — motivational badges for Honor List, Dean's List, and President's List qualification

---

## Tech Stack

| Layer          | Technology                                  |
|----------------|---------------------------------------------|
| Frontend       | React, React Flow (graph visualization)     |
| Backend        | ASP.NET Core 8 Web API, C#                  |
| Database       | SQL Server + Entity Framework Core          |
| Auth           | ASP.NET Identity + JWT                      |
| AI             | OpenAI API (GPT-3.5-turbo)                  |
| Deployment     | Free-tier cloud hosting                     |

---

## User Roles

| Role        | What They Do                                                          |
|-------------|-----------------------------------------------------------------------|
| **Student** | View course map, run impact analysis, track progress, simulate GPA, chat with AI |
| **Admin**   | Manage courses, prerequisites, student grades, and graduation requirements      |

---

## Project Structure

```
Cursus/
├── backend/         # ASP.NET Core Web API
├── frontend/        # React application
├── docs/            # Project documentation
│   ├── Cursus_Project_Proposal.md
│   └── Project_Plan.md
├── .gitignore
└── README.md
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [Project Proposal](docs/Cursus_Project_Proposal.md) | Original project idea and motivation |
| [Project Plan](docs/Project_Plan.md) | Full SDLC Stage 1 document: requirements, features, architecture, timeline, and team structure |

---

## Team

| Role                       | Responsibilities                                        |
|----------------------------|---------------------------------------------------------|
| Team Lead + Backend Lead   | Architecture, prerequisite engine, impact analyzer      |
| Backend Developer          | Auth, student module, admin APIs, GPA services          |
| Frontend Lead              | Dashboard, course map visualization, impact animation   |
| Frontend Developer         | Progress tracker, GPA simulator, admin panel, chat UI   |
| Data & AI Integration      | Data seeding, OpenAI integration, prompt engineering    |
| QA & DevOps                | Testing, deployment, documentation, demo preparation    |

---

## Timeline

| Phase             | Weeks  | Focus                                                |
|-------------------|--------|------------------------------------------------------|
| Foundation        | 1–2    | Auth, DB schema, seed data, project setup            |
| Core Engine       | 3–5    | Course catalog, prerequisite graph, visualization    |
| Killer Features   | 6–8    | Impact analyzer, progress tracker, GPA simulator     |
| Intelligence      | 9–10   | AI advisor, academic standing, polish                |
| Demo Prep         | 11–12  | UI polish, deployment, demo rehearsal                |

---

## Status

📋 **Stage 1: Planning & Requirements Analysis** — Complete

---

*DEPI Graduation Project 2026 · Full-Stack Web Development (.NET & React)*
