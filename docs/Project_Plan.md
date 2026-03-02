# Cursus — Project Plan

**SDLC Stage 1: Planning & Requirements Analysis**

| Field              | Value                                                 |
|--------------------|-------------------------------------------------------|
| **Project Name**   | Cursus *(working title — name under review)*          |
| **Program**        | DEPI — Full-Stack Web Development (.NET)              |
| **Team Size**      | 6 Members                                             |
| **Duration**       | 12 Weeks (March – June 2026)                          |
| **Version**        | 1.1 *(Updated: MVC architecture per instructor)*      |
| **Date**           | March 2, 2026                                         |
| **Document Owner** | Team Lead                                             |

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Objectives](#2-objectives)
3. [Scope](#3-scope)
4. [Target Users & Roles](#4-target-users--roles)
5. [Features & Functional Requirements](#5-features--functional-requirements)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [Academic Rules & Business Logic](#7-academic-rules--business-logic)
8. [Technology Stack](#8-technology-stack)
9. [System Architecture](#9-system-architecture)
10. [Data Model Overview](#10-data-model-overview)
11. [Project Timeline](#11-project-timeline)
12. [Team Structure & Responsibilities](#12-team-structure--responsibilities)
13. [Development Workflow](#13-development-workflow)
14. [Risks & Mitigation](#14-risks--mitigation)
15. [Assumptions & Dependencies](#15-assumptions--dependencies)
16. [Success Criteria](#16-success-criteria)
17. [Deliverables](#17-deliverables)
18. [Approval & Sign-Off](#18-approval--sign-off)

---

## 1. Project Overview

### 1.1 Problem Statement

University students in credit-hour systems face a recurring and costly problem: when they fail or drop a course, they have no way to instantly understand the downstream consequences. A failed prerequisite can silently block multiple future courses, delay graduation by one or more semesters, and push a student toward academic probation — all without any warning.

Academic advisors, who are overloaded with hundreds of students each, cannot provide personalized, data-driven guidance for every individual case. Students navigate their academic decisions blindly.

### 1.2 Proposed Solution

Cursus is a web-based academic advising platform that gives students a clear, visual understanding of their degree progress, instantly analyzes the impact of academic setbacks, and provides AI-powered guidance — all based on the student's real academic data.

The platform's core principle: **every academic decision has consequences that ripple forward through a student's degree. Cursus makes those consequences visible before they become irreversible.**

### 1.3 Value Proposition

> *"Cursus turns your academic journey into a visible, interactive map — one that warns you when things go wrong, shows you how to recover, and tells you exactly what it takes to reach your goals."*

---

## 2. Objectives

### 2.1 Primary Objectives

| ID    | Objective                                                                                    |
|-------|----------------------------------------------------------------------------------------------|
| OBJ-1 | Build a fully functional web platform demonstrating mastery of ASP.NET Core MVC, EF Core, and SQL Server |
| OBJ-2 | Implement a prerequisite dependency engine that accurately models course relationships        |
| OBJ-3 | Deliver a fail/drop impact analyzer that performs forward cascade analysis in real-time       |
| OBJ-4 | Provide a visual, interactive course map showing the student's full degree status             |
| OBJ-5 | Integrate AI (OpenAI API) as a natural-language advisory layer on top of algorithmic logic    |

### 2.2 Strategic Objective

| ID    | Objective                                              |
|-------|--------------------------------------------------------|
| OBJ-S | Achieve a TOP 3 ranking among all DEPI Full-Stack .NET track graduation projects |

---

## 3. Scope

### 3.1 In Scope

- Student authentication and profile management
- Admin-controlled course catalog, prerequisites, and student grade management
- Interactive prerequisite dependency graph visualization
- Fail/drop impact analysis with cascade detection and recovery path
- Graduation progress tracking with degree audit by course category
- GPA simulation (grade prediction + target setting + grade improvement)
- Academic standing management (good standing, warning, probation, dismissal rules)
- Credit hour overload/restriction rules based on GPA
- Honor list / Dean's list motivational indicators
- AI-powered chatbot advisor using OpenAI API with student data context
- Deployment to a publicly accessible URL for demo
- English-only user interface

### 3.2 Out of Scope

| Item                                      | Reason                                                        |
|-------------------------------------------|---------------------------------------------------------------|
| University ERP/SIS integration            | No API access; data is manually seeded for demo               |
| Co-requisite / pre-co-requisite rules     | Added complexity; prerequisites-only is sufficient for demo    |
| Full bilingual UI (Arabic + English)      | Time cost of RTL layout; English only, AI may respond in Arabic |
| What-If Simulator (general)               | Overlaps with Impact Analyzer; cut to reduce scope             |
| Full Academic Advisor Portal              | No real advisors will use the demo; admin role covers needs    |
| Native mobile apps                        | Responsive web is sufficient                                   |
| Course repeat limit tracking              | Edge case; not demo-critical                                   |
| Transfer credit management                | Too complex for scope                                          |
| Payment / billing                         | Not applicable                                                 |
| CQRS / MediatR / full Clean Architecture | Over-engineering for a 3-month demo project                    |

---

## 4. Target Users & Roles

| Role        | Description                                   | Capabilities                                                                              |
|-------------|-----------------------------------------------|-------------------------------------------------------------------------------------------|
| **Student** | Primary end user                              | View course map, run impact analysis, track progress, simulate GPA, chat with AI advisor  |
| **Admin**   | Data manager (represents department staff)    | Manage course catalog, define prerequisites, enter/update student grades, configure graduation requirements |

> **Design decision:** Reduced from 4 roles (Student/Advisor/DeptAdmin/SuperAdmin) to 2 roles (Student/Admin) to minimize permission complexity, reduce the number of dashboards and UI screens, and keep the project achievable in 12 weeks.

---

## 5. Features & Functional Requirements

### Feature 1: Identity & Profile

**Description:** Secure authentication system with academic profile setup.

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F1-01  | Students can register with email and password                                     | Must     |
| F1-02  | Users can log in via ASP.NET Identity (cookie-based authentication)               | Must     |
| F1-03  | Role-based access control enforces Student vs Admin permissions                   | Must     |
| F1-04  | Student profile stores: department, academic year, current semester               | Must     |
| F1-05  | Admin can create and manage student accounts                                      | Must     |
| F1-06  | Default admin account is seeded on first deployment                               | Must     |
| F1-07  | Password reset via email                                                          | Should   |

---

### Feature 2: Course Map

**Description:** Interactive visual graph of all courses in the student's department with prerequisite connections and completion status.

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F2-01  | Admin can create, edit, and deactivate courses (code, name, credits, type, semester availability) | Must |
| F2-02  | Admin can define prerequisite relationships between courses                       | Must     |
| F2-03  | System displays all courses as an interactive directed graph (Cytoscape.js)       | Must     |
| F2-04  | Courses are color-coded by student status: completed (green), in-progress (blue), remaining (gray), blocked (dark/locked) | Must |
| F2-05  | Student can click any course node to see details (credits, prerequisites, status) | Must     |
| F2-06  | Graph layout automatically organizes courses by semester/year level               | Should   |

**Course Status Definitions:**

| Status          | Determined By                                                   |
|-----------------|-----------------------------------------------------------------|
| Completed       | Admin entered a passing grade (≥ passing threshold)             |
| Failed          | Admin entered a failing grade (< passing threshold)             |
| In-Progress     | Admin marked the course as currently enrolled                   |
| Remaining       | Not yet attempted; prerequisites may or may not be met          |
| Blocked         | System-calculated: one or more prerequisites not passed         |

---

### Feature 3: Impact Analyzer ⚡

**Description:** When a student selects a course and reports a failure/drop, the system instantly performs a forward cascade analysis showing all downstream consequences and a recovery path.

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F3-01  | Student can select any completed/in-progress course and mark it as "failed" (simulation mode) | Must |
| F3-02  | System identifies all courses directly and transitively dependent on the failed course | Must |
| F3-03  | Blocked courses are highlighted on the Course Map with animated cascade effect    | Must     |
| F3-04  | System calculates how many semesters are affected by the failure                  | Must     |
| F3-05  | System suggests a recovery path: when to retake the course (based on availability) and when blocked courses unlock | Must |
| F3-06  | System calculates the graduation delay impact: "Graduation delayed by X semester(s)" | Must |
| F3-07  | Recovery path accounts for course semester availability (Fall/Spring/Both/All)    | Must     |
| F3-08  | A green recovery path is displayed on the Course Map alongside the red cascade   | Should   |

**Core Algorithm:** BFS/DFS traversal on the prerequisite dependency graph. Pure algorithmic logic — no AI dependency.

---

### Feature 4: Progress & Planning

**Description:** Unified view combining graduation progress tracking, GPA simulation, and academic standing — organized as tabs within a single page.

#### Tab 1: Progress Tracker

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F4-01  | Display overall credit completion: X/Y credits completed with progress bar        | Must     |
| F4-02  | Display course completion grouped by category: Core, Department Elective, Free Elective, University Requirement | Must |
| F4-03  | For elective categories, show eligible courses the student can pick from          | Must     |
| F4-04  | Calculate and display projected graduation semester based on current pace         | Must     |
| F4-05  | If student's GPA qualifies for overload (≥ 3.0), show accelerated graduation projection | Should |
| F4-06  | Display "mathematically impossible to graduate" warning if min GPA (2.0) is unreachable | Must |
| F4-07  | Display total credit hours required for graduation                                | Must     |

#### Tab 2: GPA Simulator

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F4-08  | Display current cumulative GPA and current semester GPA                           | Must     |
| F4-09  | Student can set predicted grades for in-progress courses → system calculates predicted semester GPA and cumulative GPA | Must |
| F4-10  | Student can set a target GPA → system back-calculates minimum grades needed per course | Must |
| F4-11  | Courses eligible for grade improvement (original grade ≤ D+) are flagged with a badge | Must |
| F4-12  | Student can simulate retaking an improvable course with a new grade → GPA recalculates with the new grade replacing the original | Must |
| F4-13  | Honor list indicators: if predicted GPA qualifies for Honor List (≥ 3.0), Dean's List (≥ 3.5), or President's List (≥ 3.75), show a motivational badge | Should |
| F4-14  | Show how predicted GPA affects academic standing (warning/probation triggers)     | Should   |

#### Academic Standing Rules (applied system-wide)

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F4-15  | System tracks academic standing per semester based on GPA thresholds              | Must     |
| F4-16  | Dashboard displays current standing prominently: Good / Warning / Probation       | Must     |
| F4-17  | System warns if next semester standing will worsen based on GPA Simulator predictions | Should |
| F4-18  | When on probation, system reflects restricted credit hour limit in graduation projections | Should |

---

### Feature 5: AI Advisor 🤖

**Description:** A chat interface where students ask academic questions and receive AI-generated responses grounded in their real academic data.

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| F5-01  | Student can open a chat interface and type natural-language questions              | Must     |
| F5-02  | System injects student context (profile, courses, GPA, standing, impact results) into the AI prompt | Must |
| F5-03  | AI generates personalized, data-driven responses referencing specific courses and numbers | Must |
| F5-04  | AI responses are advisory only — no system actions are triggered by AI output     | Must     |
| F5-05  | Chat history is maintained within the session                                     | Should   |
| F5-06  | Graceful fallback message when OpenAI API is unavailable or rate-limited          | Must     |
| F5-07  | System uses GPT-3.5-turbo for cost efficiency (free tier / low cost)              | Must     |

**Key Principle:** AI is the **voice**, not the brain. All factual computations (prerequisites, GPA, graduation audit) are handled by deterministic algorithms. AI only translates structured results into natural, supportive language.

---

### Dashboard (Landing Page)

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| D-01   | After login, student sees a dashboard with key metrics at a glance               | Must     |
| D-02   | Display: current GPA, credits completed/total, courses remaining, projected graduation | Must |
| D-03   | Display: current academic standing with color-coded indicator                     | Must     |
| D-04   | Display: danger zone alert if GPA is approaching a critical threshold            | Should   |
| D-05   | Display: overload eligibility status                                              | Should   |
| D-06   | Quick-access links to Course Map, Impact Analyzer, Progress & Planning, AI Advisor | Must   |

---

### Admin Panel

| ID     | Requirement                                                                       | Priority |
|--------|-----------------------------------------------------------------------------------|----------|
| A-01   | Admin can CRUD courses (code, name, credits, type, semester availability, passing threshold) | Must |
| A-02   | Admin can define/edit prerequisite links between courses                          | Must     |
| A-03   | Admin can manage student accounts and assign departments                         | Must     |
| A-04   | Admin can enter/update student grades for completed courses                      | Must     |
| A-05   | Admin can mark courses as in-progress for students                               | Must     |
| A-06   | Admin can configure graduation requirements (category, required credits, eligible courses) | Must |
| A-07   | Admin sees a high-failure-rate alert badge for courses where many students failed | Should  |
| A-08   | Admin can configure GPA scale (grade letter → point value mapping)               | Should   |
| A-09   | Admin can configure credit hour limits by academic standing                      | Should   |

---

## 6. Non-Functional Requirements

| Category          | Requirement                                                          |
|-------------------|----------------------------------------------------------------------|
| **Performance**   | All primary views load within 2 seconds under normal conditions      |
| **Security**      | Cookie-based authentication via ASP.NET Identity; passwords hashed; CSRF protection; input validation against XSS/SQL injection |
| **Usability**     | English-only UI; responsive design for desktop and tablet viewports  |
| **Scalability**   | Architecture supports future multi-department / multi-university extension without re-engineering |
| **Availability**  | Deployed to a publicly accessible URL for demo purposes              |
| **Maintainability** | MVC 3-layer architecture (Controller → Service → Data); consistent code style |
| **Data Integrity** | Prerequisite relationships enforce referential integrity at DB level |

---

## 7. Academic Rules & Business Logic

This section codifies all credit-hour system rules that the platform enforces.

### 7.1 GPA Scale (Default — 4.0)

| Grade | Points | Grade | Points |
|-------|--------|-------|--------|
| A+    | 4.0    | C+    | 2.3    |
| A     | 4.0    | C     | 2.0    |
| A-    | 3.7    | C-    | 1.7    |
| B+    | 3.3    | D+    | 1.3    |
| B     | 3.0    | D     | 1.0    |
| B-    | 2.7    | F     | 0.0    |

*Stored in a configurable `GradeScale` database table to support other scales.*

### 7.2 GPA Calculation

```
Semester GPA = Σ(course_credit_hours × grade_points) / Σ(course_credit_hours)
Cumulative GPA = Σ(all_semesters_credit_hours × grade_points) / Σ(all_credit_hours)
```

### 7.3 Academic Standing

| Condition                                      | Standing             | Effect                        |
|------------------------------------------------|----------------------|-------------------------------|
| Semester GPA ≥ 2.0                             | ✅ Good Standing     | Normal course load allowed    |
| Semester GPA < 2.0 (1st occurrence)            | ⚠️ Academic Warning  | Student notified              |
| Semester GPA < 2.0 (2 consecutive semesters)   | 🟠 Academic Probation | Credit hours restricted       |
| Semester GPA < 2.0 (3 consecutive) OR CGPA < 1.0 | 🔴 Dismissal       | Student flagged for dismissal |

### 7.4 Credit Hour Limits

| Standing           | Min Credits/Semester | Max Credits/Semester |
|--------------------|----------------------|----------------------|
| Good (GPA < 3.0)  | 12                   | 18                   |
| Good (GPA ≥ 3.0)  | 12                   | 21 (overload)        |
| Warning            | 12                   | 18                   |
| Probation          | 12                   | 12–14 (restricted)   |

*Stored as configurable rules in the database.*

### 7.5 Passing Grade Threshold

- Default: D (1.0) — student passes and earns credit
- Configurable per course or department (e.g., core courses may require C)

### 7.6 Grade Improvement (Course Retake)

- A student may retake a course if the original grade is ≤ D+ (configurable threshold)
- The new grade **replaces** the original grade in GPA calculation
- Retake limits are **not tracked** in this version (future enhancement)

### 7.7 Graduation Requirements

- Each department defines total credit hours required (e.g., 132)
- Requirements are grouped by category: **Core, Department Elective, Free Elective, University Requirement**
- Each category specifies required credits and eligible courses (for elective pools)
- Minimum cumulative GPA for graduation: 2.0

### 7.8 Honor Recognition

| List              | Semester GPA Threshold |
|-------------------|------------------------|
| Honor List        | ≥ 3.0                  |
| Dean's List       | ≥ 3.5                  |
| President's List  | ≥ 3.75                 |

*Displayed as motivational badges in the GPA Simulator. No separate feature.*

### 7.9 Course Semester Availability

| Value        | Meaning                            |
|--------------|------------------------------------|
| Fall         | Offered in Fall semester only      |
| Spring       | Offered in Spring semester only    |
| FallSpring   | Offered in both Fall and Spring    |
| All          | Offered every semester inc. Summer |

*Manually set by Admin. System shows a high-failure alert recommending Admin update availability when warranted.*

---

## 8. Technology Stack

| Layer              | Technology                                               |
|--------------------|----------------------------------------------------------|
| **Application**    | ASP.NET Core 8 MVC, C#, Razor Views                     |
| **Styling**        | Bootstrap 5 + custom CSS                                 |
| **Graph Viz**      | Cytoscape.js (client-side, loaded in Razor views)        |
| **ORM**            | Entity Framework Core 8                                  |
| **Database**       | SQL Server (LocalDB for dev, hosted SQL for demo)        |
| **Authentication** | ASP.NET Identity (cookie-based authentication)           |
| **AI Integration** | OpenAI API (GPT-3.5-turbo) via AJAX calls                |
| **Client-Side**    | jQuery (bundled with MVC), vanilla JavaScript            |
| **Version Control**| Git + GitHub (branch-per-feature workflow)               |
| **Deployment**     | Free-tier hosting (Azure App Service / Somee / MonsterASP) |
| **Project Mgmt**   | GitHub Projects or Jira (free tier)                      |

---

## 9. System Architecture

```
┌──────────────────────────────────────────────────────┐
│           ASP.NET Core 8 MVC (Monolithic)             │
│                                                      │
│  Controllers/   →   Services/   →   Data/            │
│  (HTTP + Views)     (Business       (EF Core         │
│                      Logic)          DbContext)       │
│                        ↕                             │
│               OpenAI API Client                      │
│               (AI Advisor only)                      │
│                                                      │
│  Views/  (Razor)     wwwroot/                        │
│  ├── Shared/         ├── css/                        │
│  ├── Student/        ├── js/ (Cytoscape.js, custom)  │
│  ├── Admin/          └── lib/ (Bootstrap, jQuery)    │
│  └── Account/                                        │
└────────────────────────┬─────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────┐
│                  SQL Server                          │
│                                                      │
│  Users · Courses · Prerequisites ·                   │
│  StudentCourses · GraduationRequirements ·           │
│  GradeScales · StandingHistory                       │
└──────────────────────────────────────────────────────┘
```

**Architecture pattern:** ASP.NET Core MVC monolith with Razor views. Interactive components (graph, chat, GPA calculator) use client-side JavaScript (Cytoscape.js, jQuery AJAX) within server-rendered pages.

---

## 10. Data Model Overview

### 10.1 Core Entities

```
University
  ├── Id, Name, GpaScale (FK to GradeScale set)
  
Department
  ├── Id, Name, UniversityId (FK)
  ├── TotalCreditsRequired, MinGpaForGraduation

Course
  ├── Id, Code, Name, CreditHours
  ├── DepartmentId (FK)
  ├── CourseType (Core / DeptElective / FreeElective / UniversityReq)
  ├── SemesterAvailability (Fall / Spring / FallSpring / All)
  ├── PassingGradeThreshold (default: D)
  ├── IsActive (soft delete)

CoursePrerequisite (M:N self-referencing on Course)
  ├── CourseId (FK) — the course
  ├── PrerequisiteId (FK) — must pass this first

Student (extends ASP.NET Identity User)
  ├── Id, DepartmentId (FK)
  ├── AcademicYear, CurrentSemester (Fall/Spring/Summer)
  ├── CurrentStanding (Good / Warning / Probation / Dismissed)

StudentCourse
  ├── StudentId (FK), CourseId (FK)
  ├── Status (Completed / Failed / InProgress)
  ├── Grade (nullable — set by admin)
  ├── Semester, AcademicYear (when taken)

GraduationRequirement
  ├── Id, DepartmentId (FK)
  ├── CategoryType (Core / DeptElective / FreeElective / UniversityReq)
  ├── RequiredCredits

GraduationRequirementCourse (eligible courses per requirement)
  ├── GraduationRequirementId (FK), CourseId (FK)

GradeScale
  ├── Id, UniversityId (FK), LetterGrade, PointValue

StandingHistory
  ├── StudentId (FK), Semester, AcademicYear
  ├── SemesterGpa, CumulativeGpa, Standing

CreditHourRule
  ├── DepartmentId (FK), Standing
  ├── MinCredits, MaxCredits
```

### 10.2 Entity Count: ~10 tables

Lean, fully relational, no graph database needed. The prerequisite graph is modeled as a standard adjacency list (CoursePrerequisite) and traversed via BFS/DFS in the service layer.

---

## 11. Project Timeline

### 11.1 Phase Overview

| Phase | Weeks | Focus | Key Deliverables |
|---|---|---|---|
| **1 — Foundation** | 1–2 | Setup & Core | MVC project setup, DB schema + migrations, seed data from curriculum PDF, Identity auth (register/login/cookies), Admin CRUD for courses & prerequisites |
| **2 — Engine** | 3–5 | Core Logic & Viz | Prerequisite engine, Cytoscape.js graph visualization, course status color-coding, student dashboard, course history display |
| **3 — Killer Features** | 6–8 | Differentiators | Impact Analyzer (cascade engine + animated graph), Progress Tracker (audit by category + projections), GPA Simulator |
| **4 — Intelligence** | 9–10 | AI & Rules | AI Advisor chat (AJAX), academic standing engine, danger alerts, grade improvement logic, honor badges |
| **5 — Polish** | 11 | Quality | UI polish, error handling, loading states, responsive design, edge case testing, admin panel refinement |
| **6 — Demo** | 12 | Delivery | Deployment, demo data setup, demo script rehearsal (5+ runs), documentation cleanup |

### 11.2 Key Milestones

| Date       | Milestone                                                |
|------------|----------------------------------------------------------|
| Week 2 end | Auth working, course data seeded, admin can manage courses |
| Week 5 end | Interactive course map fully functional with real data    |
| Week 8 end | Impact Analyzer + Progress Tracker live and demo-ready   |
| Week 10 end | All features complete, AI Advisor integrated             |
| Week 11 end | All polishing done, deployment complete                  |
| Week 12 end | Demo rehearsed and delivered                             |

### 11.3 Schedule Rules

> ⛔ **No new features after Week 10.** Weeks 11–12 are polish and demo only. Non-negotiable.

> 📌 **Week 1 critical path:** Curriculum data must be converted from PDF to structured seed data. Without this, no feature works.

---

## 12. Team Structure & Responsibilities

| Member   | Role                  | Primary Responsibilities                                              |
|----------|-----------------------|-----------------------------------------------------------------------|
| Member 1 | **Team Lead + Architecture** | Project management, MVC architecture, Prerequisite Engine, Impact Analyzer logic |
| Member 2 | Backend Developer     | Auth system (Identity + Cookies), Admin controllers & services, GPA calculation services |
| Member 3 | Full-Stack Developer  | Student Dashboard views, Course Map (Cytoscape.js), Impact Analyzer animation (JS) |
| Member 4 | Full-Stack Developer  | Progress Tracker views, GPA Simulator (JS), Admin Panel views, AI Chat UI |
| Member 5 | Data & AI Integration | Curriculum data extraction & seeding, OpenAI API integration, prompt engineering, test data scenarios |
| Member 6 | QA & DevOps           | Testing, deployment pipeline, documentation, demo preparation         |

### Team Rules

- **MVC unified codebase:** All team members work in the same ASP.NET Core MVC project. No separate frontend project.
- **Controller ownership:** Each feature module has a designated controller owner. Views and services are developed together.
- **No solo silos:** Every feature has clear ownership but code reviews ensure team-wide awareness.

---

## 13. Development Workflow

### 13.1 Git Workflow

- **Branch strategy:** `main` (production) ← `develop` (integration) ← `feature/*` (individual work)
- Every feature is developed on a `feature/feature-name` branch
- Pull requests are required to merge into `develop`
- At least 1 team member must review before merge
- `main` is only updated from `develop` at milestone checkpoints

### 13.2 Communication

| Activity        | Frequency         | Tool            |
|-----------------|-------------------|-----------------|
| Daily standup   | Daily (async OK)  | WhatsApp / Discord |
| Sprint planning | Every 2 weeks     | GitHub Projects / Jira |
| Code reviews    | Per pull request   | GitHub PRs       |
| Demo rehearsal  | Week 12            | In-person / call |

### 13.3 Definition of Done

A feature is "done" when:
- [ ] Controller actions and service logic are implemented and tested
- [ ] Razor views are implemented with proper layout, validation, and styling
- [ ] Edge cases are handled (empty states, validation errors, loading states)
- [ ] Code is reviewed and merged to `develop`
- [ ] Feature works in the deployed environment

---

## 14. Risks & Mitigation

| #  | Risk                                      | Severity | Probability | Mitigation                                                     |
|----|-------------------------------------------|----------|-------------|----------------------------------------------------------------|
| R1 | **Curriculum data not extracted on time**  | 🔴 High  | Medium      | Start PDF-to-data conversion Day 1. Assign dedicated member. Use mock data as fallback. |
| R2 | **Impact Analyzer graph logic is more complex than expected** | 🟠 Medium | Medium | Build and test the BFS/DFS engine in isolation first (Week 3). Write unit tests for edge cases (circular deps, multiple paths). |
| R3 | **OpenAI API rate limits or downtime**     | 🟡 Low   | Medium      | Implement graceful fallback message. Use GPT-3.5-turbo (higher rate limits). Cache common prompts if possible. |
| R4 | **Scope creep — team wants to add features mid-project** | 🔴 High | High | Feature scope is locked at Week 2. All new ideas go to a "Future Enhancements" backlog. Team Lead enforces. |
| R5 | **Cytoscape.js learning curve**             | 🟡 Low   | Medium      | One team member prototypes the graph in Week 3. Use dagre layout plugin. Plenty of documentation and examples online. |
| R6 | **Team coordination issues across 6 members** | 🟠 Medium | Medium | Clear ownership per feature. Daily async standups. GitHub PR reviews enforce quality. |
| R7 | **Free-tier hosting limitations**          | 🟡 Low   | Low         | Test deployment early (Week 8). Identify backup hosting options. Keep database small (seed data for 1 department). |

---

## 15. Assumptions & Dependencies

### Assumptions

1. The team has access to the official curriculum document (PDF) for at least one department.
2. All team members can commit 15–20 hours per week to the project.
3. Free-tier hosting is sufficient for demo-level traffic (< 50 concurrent users).
4. OpenAI API free credits or minimal budget (< $10) covers demo usage.
5. Judges evaluate based on a live demo, not production readiness.

### Dependencies

| Dependency                        | Owner    | Required By |
|-----------------------------------|----------|-------------|
| Curriculum PDF → structured data  | Member 5 | Week 1      |
| OpenAI API key                    | Team Lead | Week 9     |
| Hosting environment setup         | Member 6 | Week 8      |
| SQL Server instance (dev)         | Member 1 | Week 1      |

---

## 16. Success Criteria

### Must-Have (project passes)

| Criterion                                                            | Measured By            |
|----------------------------------------------------------------------|------------------------|
| Student can register, log in, and view their academic profile        | Demo                   |
| Course map displays all courses with correct prerequisite connections | Demo + test data       |
| Impact Analyzer correctly identifies all blocked courses when a prerequisite fails | Manual test scenarios |
| Progress Tracker accurately shows credits completed by category      | Comparison with manual calculation |
| GPA Simulator calculates correct GPA when grades are adjusted        | Manual verification    |
| AI Advisor responds with context-aware, personalized answers         | Demo                   |
| Application is deployed and accessible via a public URL              | URL test               |

### Competition-Grade (TOP 3 target)

| Criterion                                                            | Measured By            |
|----------------------------------------------------------------------|------------------------|
| Course Map cascade animation is smooth and visually impressive       | Judge reaction         |
| Demo tells a compelling student story (not a feature tour)           | Demo rehearsal feedback |
| Academic rules (standing, overload, grade improvement) are correctly modeled | Edge case tests |
| UI is polished, responsive, and professional                         | Visual inspection      |
| AI Advisor handles diverse questions gracefully                      | Live Q&A during demo   |

---

## 17. Deliverables

| #  | Deliverable                            | Format            | Due      |
|----|----------------------------------------|-------------------|----------|
| D1 | Project Plan                           | Markdown          | Week 1   |
| D2 | Software Requirements Specification    | Markdown          | Week 1   |
| D3 | Database schema + seed data            | SQL / EF Migration | Week 2  |
| D4 | Working application (deployed)         | Web URL           | Week 11  |
| D5 | Demo script                            | Document          | Week 12  |
| D6 | Final presentation                     | Slides            | Week 12  |
| D7 | Source code repository                 | GitHub            | Ongoing  |

---

## 18. Approval & Sign-Off

> **By signing, all team members confirm they have read this Project Plan, understand the scope and timeline, and agree to the defined responsibilities and deliverables.**

---

*Document Version 1.1 — March 2, 2026 (Updated for MVC architecture per instructor directive)*
