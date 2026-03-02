# Cursus — Software Requirements Specification (SRS)

**SDLC Stage 2: Defining Requirements**

| Field              | Value                                                 |
|--------------------|-------------------------------------------------------|
| **Project Name**   | Cursus *(working title)*                              |
| **Program**        | DEPI — Full-Stack Web Development                     |
| **Architecture**   | ASP.NET Core MVC (Monolithic)                         |
| **Team Size**      | 6 Members                                             |
| **Duration**       | 12 Weeks (March – June 2026)                          |
| **Version**        | 1.0                                                   |
| **Date**           | March 2, 2026                                         |

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Overall Description](#2-overall-description)
3. [Architecture & MVC Impact](#3-architecture--mvc-impact)
4. [Feature Priority & Build Order](#4-feature-priority--build-order)
5. [Module 1: Identity & Access Management](#5-module-1-identity--access-management)
6. [Module 2: Admin — Course & Curriculum Management](#6-module-2-admin--course--curriculum-management)
7. [Module 3: Admin — Student Data Management](#7-module-3-admin--student-data-management)
8. [Module 4: Student — Dashboard](#8-module-4-student--dashboard)
9. [Module 5: Student — Course Map](#9-module-5-student--course-map)
10. [Module 6: Student — Impact Analyzer](#10-module-6-student--impact-analyzer)
11. [Module 7: Student — Progress & Planning](#11-module-7-student--progress--planning)
12. [Module 8: Student — AI Advisor](#12-module-8-student--ai-advisor)
13. [Non-Functional Requirements](#13-non-functional-requirements)
14. [Academic Rules Reference](#14-academic-rules-reference)
15. [Data Dictionary](#15-data-dictionary)
16. [UI/UX Guidelines](#16-uiux-guidelines)
17. [Glossary](#17-glossary)
18. [Approval](#18-approval)

---

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) defines the detailed functional and non-functional requirements for Cursus — an AI-powered academic advising platform for credit-hour university students. This document serves as the authoritative reference for what the system must do, how each module behaves, and what data is required.

This document is intended for all team members (developers, testers, and project lead) to align on **exactly** what will be built and in what order.

### 1.2 Scope

Cursus is a monolithic ASP.NET Core MVC web application that allows:

- **Students** to visualize their degree as an interactive prerequisite graph, analyze the impact of failing a course, track graduation progress, simulate GPA scenarios, and receive AI-powered academic advice.
- **Admins** to manage the course catalog, prerequisite relationships, student grades, and graduation requirements.

The system is scoped to a **single university department** for the demo, with architecture that supports future multi-department and multi-university extension.

### 1.3 Definitions & Acronyms

| Term | Definition |
|---|---|
| **MVC** | Model-View-Controller — the architectural pattern used |
| **CGPA** | Cumulative Grade Point Average across all semesters |
| **SGPA** | Semester Grade Point Average for a single semester |
| **Prerequisite** | A course that must be passed before enrolling in another course |
| **Cascade** | The chain reaction of blocked courses when a prerequisite is failed |
| **Standing** | Academic status (Good, Warning, Probation, Dismissed) |
| **Overload** | Registering for more credit hours than the standard limit |
| **Impact Analysis** | The process of determining all downstream consequences of a course failure |

### 1.4 References

| Document | Location |
|---|---|
| Project Plan | `docs/Project_Plan.md` |
| Project Proposal | `docs/Cursus_Project_Proposal.md` |

---

## 2. Overall Description

### 2.1 Product Perspective

Cursus operates as a **standalone web application**. It does not integrate with any existing university ERP or student information system. All academic data (courses, prerequisites, grades) is managed within the platform by admin users.

The system is designed as if it were an add-on module to a university portal. In a production scenario, student data (grades, enrollment) would be fed from the university's SIS via an API. For this demo, admins enter data manually and/or data is pre-seeded.

### 2.2 User Classes

| User Class | Count (Demo) | Technical Skill | Access Frequency |
|---|---|---|---|
| **Student** | 3–5 test accounts | Low — expects simple, guided UI | Daily (hypothetical) |
| **Admin** | 1–2 accounts | Medium — comfortable with data entry forms | Weekly (manages data) |

### 2.3 Operating Environment

- **Server:** ASP.NET Core 8 running on any OS (Windows/Linux)
- **Client:** Modern web browser (Chrome, Edge, Firefox) — desktop and tablet
- **Database:** SQL Server (LocalDB for development, hosted SQL for demo)

### 2.4 Constraints

| Constraint | Impact |
|---|---|
| MVC architecture (instructor requirement) | No separate frontend SPA; all views are server-rendered Razor views with AJAX for interactive components |
| 12-week timeline | Features must be strictly prioritized; no scope additions after Week 2 |
| Free-tier hosting only | Database and app size must be minimal; no paid cloud services |
| 6-person beginner team | Architecture and code patterns must be kept simple and well-documented |
| OpenAI free/minimal budget | AI Advisor uses GPT-3.5-turbo; graceful fallback required when API is unavailable |

### 2.5 Assumptions

1. The team has access to the official curriculum PDF for at least one department.
2. All team members have basic proficiency in C#, ASP.NET Core MVC, and Entity Framework Core.
3. SQL Server LocalDB is available for local development.
4. Internet access is required for the AI Advisor feature (OpenAI API calls).

---

## 3. Architecture & MVC Impact

### 3.1 Why MVC Instead of Web API + React

The instructor directed the team to use ASP.NET Core MVC instead of a Web API + React SPA. This change:

**Simplifies:**
- One project, one deployment (no CORS, no separate frontend build)
- The whole team (6 members) can work in the same codebase and language (C#)
- Auth uses ASP.NET Identity's built-in cookie authentication (simpler than JWT)
- Server-side rendering means fewer client-side bugs

**Requires adaptation for:**
- **Interactive Course Map** — Razor views are server-rendered, but the prerequisite graph needs client-side interactivity. Solution: use **Cytoscape.js** or **vis.js** (JavaScript graph libraries) loaded via `<script>` tag in a Razor view, with data passed from the controller via `ViewBag`/`ViewData` or a JSON endpoint.
- **AI Advisor Chat** — Will use AJAX (fetch/jQuery) to send messages and receive responses without full page reload.
- **GPA Simulator** — Interactive grade adjustments will use JavaScript + AJAX partial updates.

### 3.2 MVC Architecture

```
Cursus (ASP.NET Core MVC Project)
│
├── Controllers/         # Handle HTTP requests, orchestrate services
│   ├── HomeController
│   ├── AccountController
│   ├── AdminController
│   ├── StudentController
│   ├── CourseMapController
│   ├── ImpactController
│   ├── ProgressController
│   └── AiAdvisorController
│
├── Services/            # Business logic layer
│   ├── AuthService
│   ├── CourseService
│   ├── PrerequisiteService
│   ├── ImpactAnalysisService
│   ├── GpaService
│   ├── ProgressService
│   ├── StandingService
│   └── AiAdvisorService
│
├── Models/              # EF Core entities + View Models
│   ├── Entities/        # Database entities
│   └── ViewModels/      # View-specific models
│
├── Data/                # DbContext + migrations + seed data
│   ├── AppDbContext
│   ├── Migrations/
│   └── SeedData/
│
├── Views/               # Razor views organized by controller
│   ├── Home/
│   ├── Account/
│   ├── Admin/
│   ├── Student/
│   ├── CourseMap/
│   ├── Impact/
│   ├── Progress/
│   ├── AiAdvisor/
│   └── Shared/          # _Layout, _Partials, _ViewStart
│
└── wwwroot/             # Static files
    ├── css/
    ├── js/              # Cytoscape.js, custom scripts
    └── lib/             # Client-side libraries
```

### 3.3 Authentication Change

| Aspect | Previous (Web API) | Current (MVC) |
|---|---|---|
| Auth mechanism | JWT Bearer tokens | Cookie authentication (ASP.NET Identity default) |
| Token management | Access + Refresh tokens | Session cookies (automatic) |
| Login flow | API call → store JWT in localStorage | Form POST → redirect on success |
| Route protection | `[Authorize]` on API controllers | `[Authorize]` on MVC controllers + `[Authorize(Roles = "Admin")]` |
| Complexity | Higher (manual token handling) | Lower (Identity handles everything) |

### 3.4 Interactive Components Strategy

For features that require client-side interactivity within server-rendered views:

| Component | Library | How It Works |
|---|---|---|
| **Course Map graph** | Cytoscape.js | Controller passes course + prerequisite data as JSON to the view. Cytoscape.js renders the interactive graph client-side. |
| **Impact Analyzer animation** | Cytoscape.js | AJAX call to an action method → returns affected course IDs as JSON → JS animates the graph nodes. |
| **GPA Simulator sliders** | Vanilla JS / jQuery | JavaScript calculates GPA on-the-fly as the student adjusts grade dropdowns. No server call needed for basic calculation. |
| **AI Advisor chat** | Fetch API / jQuery AJAX | AJAX POST to `AiAdvisorController` → returns AI response as JSON → JS appends to chat container. |

---

## 4. Feature Priority & Build Order

Features are ordered based on **dependency chain** (what must exist before other features can work) and **demo impact** (what impresses judges most).

### 4.1 Build Order

```
PHASE 1: Foundation (Weeks 1–2)
  ├── Module 1: Identity & Access Management
  ├── Module 2: Admin — Course & Curriculum Management
  └── Module 3: Admin — Student Data Management

PHASE 2: Core Experience (Weeks 3–5)
  ├── Module 5: Student — Course Map
  └── Module 4: Student — Dashboard

PHASE 3: Killer Features (Weeks 6–8)
  ├── Module 6: Student — Impact Analyzer  ⚡
  └── Module 7: Student — Progress & Planning

PHASE 4: Intelligence (Weeks 9–10)
  └── Module 8: Student — AI Advisor

PHASE 5: Polish & Demo (Weeks 11–12)
  └── UI polish, edge cases, deployment, demo rehearsal
```

### 4.2 Why This Order

| Order | Rationale |
|---|---|
| **Auth first** | Nothing works without login. It's also the simplest module — quick win for team confidence. |
| **Admin CRUD second** | The Course Map, Impact Analyzer, and Progress Tracker all need data to exist. Admin creates that data. No data = no demo. |
| **Course Map third** | The visual graph is the foundation for the Impact Analyzer. It must work before the cascade animation can be built on top of it. |
| **Dashboard fourth** | A simple aggregation page. Once student data and courses exist, the dashboard is straightforward. |
| **Impact Analyzer fifth** | The killer feature — but it depends on the Course Map and prerequisite data being functional. Build it once the foundation is solid. |
| **Progress & Planning sixth** | GPA calculation and progress tracking need student grades (from Admin module) and graduation requirements (from Curriculum module). |
| **AI Advisor last** | Depends on all other modules (it needs student data, impact results, GPA, and standing as context). Also highest risk (external API). Save for last so a failure here doesn't block the core demo. |

---

## 5. Module 1: Identity & Access Management

### 5.1 Description

Secure authentication and authorization using ASP.NET Identity with cookie-based authentication. Two roles: Student and Admin.

### 5.2 Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| IAM-01 | Users can register with email, full name, and password | Must | All |
| IAM-02 | Registration requires: email (unique), full name, password (min 8 chars, 1 uppercase, 1 digit) | Must | All |
| IAM-03 | New registrations are assigned "Student" role by default | Must | System |
| IAM-04 | Users can log in with email and password | Must | All |
| IAM-05 | Successful login redirects to role-appropriate dashboard (Student → Student Dashboard, Admin → Admin Dashboard) | Must | System |
| IAM-06 | Failed login shows an inline error message without revealing which credential is wrong | Must | System |
| IAM-07 | Users can log out; session is terminated | Must | All |
| IAM-08 | Routes are protected by role: student pages require Student role, admin pages require Admin role | Must | System |
| IAM-09 | Unauthorized access redirects to login page | Must | System |
| IAM-10 | A default admin account is seeded on first run (`admin@cursus.com` / configurable password) | Must | System |
| IAM-11 | Admin can promote a user to Admin role | Should | Admin |
| IAM-12 | Password reset via email link | Nice | All |

### 5.3 Views

| View | Route | Description |
|---|---|---|
| Register | `/Account/Register` | Registration form (email, name, password, confirm password) |
| Login | `/Account/Login` | Login form (email, password) |
| Access Denied | `/Account/AccessDenied` | Shown when a user tries to access a page they don't have permission for |

### 5.4 Validation Rules

| Field | Rules |
|---|---|
| Email | Required, valid email format, unique in system |
| Full Name | Required, 2–100 characters |
| Password | Required, min 8 characters, at least 1 uppercase, 1 digit |
| Confirm Password | Must match Password |

---

## 6. Module 2: Admin — Course & Curriculum Management

### 6.1 Description

Admin interface for managing the academic data backbone: universities, departments, courses, prerequisite relationships, graduation requirements, and system configuration.

### 6.2 Functional Requirements

#### University & Department

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| CCM-01 | Admin can create a university (name) | Must | Admin |
| CCM-02 | Admin can create departments within a university (name, total credits required, min GPA for graduation) | Must | Admin |
| CCM-03 | Admin can edit university and department details | Must | Admin |

#### Course Catalog

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| CCM-04 | Admin can create a course with: code, name, credit hours, course type, semester availability, passing grade threshold | Must | Admin |
| CCM-05 | Admin can edit course details | Must | Admin |
| CCM-06 | Admin can deactivate a course (soft delete — does not remove, just hides from active catalog) | Must | Admin |
| CCM-07 | Each course belongs to one department | Must | Admin |
| CCM-08 | Course code must be unique within a department | Must | System |
| CCM-09 | Course types are: Core, DeptElective, FreeElective, UniversityReq | Must | System |
| CCM-10 | Semester availability options: Fall, Spring, FallSpring, All | Must | System |
| CCM-11 | Default passing grade threshold is D (1.0); configurable per course | Must | Admin |

#### Course Fields Detail

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| Code | string(10) | Yes | — | e.g., "CS301" |
| Name | string(100) | Yes | — | e.g., "Data Structures II" |
| CreditHours | int | Yes | — | Range: 1–6 |
| CourseType | enum | Yes | Core | Core / DeptElective / FreeElective / UniversityReq |
| SemesterAvailability | enum | Yes | FallSpring | Fall / Spring / FallSpring / All |
| PassingGradeThreshold | string | Yes | "D" | The minimum grade to pass this course |
| DepartmentId | FK | Yes | — | — |
| IsActive | bool | Yes | true | Soft delete flag |

#### Prerequisite Management

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| CCM-12 | Admin can add prerequisite relationships: "Course X requires Course Y to be passed first" | Must | Admin |
| CCM-13 | Admin can remove prerequisite relationships | Must | Admin |
| CCM-14 | System prevents circular prerequisites (A requires B, B requires A) | Must | System |
| CCM-15 | A course can have multiple prerequisites (AND logic: all must be passed) | Must | System |
| CCM-16 | Admin views prerequisites as a list under each course | Must | Admin |

#### Graduation Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| CCM-17 | Admin can define graduation requirements per department, grouped by category type (Core, DeptElective, FreeElective, UniversityReq) | Must | Admin |
| CCM-18 | Each requirement specifies: category type and required credit hours | Must | Admin |
| CCM-19 | For elective categories, admin can link eligible courses that satisfy the requirement | Must | Admin |
| CCM-20 | Admin can edit and delete graduation requirements | Must | Admin |

#### System Configuration

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| CCM-21 | Admin can configure the GPA scale (letter grade → point value mapping) per university | Should | Admin |
| CCM-22 | Admin can configure credit hour limits per academic standing (Good/Warning/Probation → min/max credits) | Should | Admin |
| CCM-23 | Admin sees a high-failure alert badge next to courses where many students failed in the current or recent semester | Should | System |

### 6.3 Views

| View | Route | Description |
|---|---|---|
| Admin Dashboard | `/Admin` | Overview with quick stats + high-failure alerts |
| Course List | `/Admin/Courses` | Searchable/filterable table of all courses |
| Course Create/Edit | `/Admin/Courses/Create`, `/Admin/Courses/Edit/{id}` | Form for course details |
| Course Detail | `/Admin/Courses/{id}` | Full course info + prerequisites list + add/remove prerequisite |
| Graduation Req. | `/Admin/GraduationRequirements` | Requirements table grouped by category |
| Departments | `/Admin/Departments` | Department CRUD |
| GPA Scale | `/Admin/Settings/GpaScale` | Grade-to-points mapping editor |
| Credit Rules | `/Admin/Settings/CreditRules` | Standing-based credit hour limits editor |

---

## 7. Module 3: Admin — Student Data Management

### 7.1 Description

Admin interface for managing student academic records: profile assignment, grade entry, and enrollment status.

### 7.2 Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| SDM-01 | Admin can view a list of all registered students, filterable by department | Must | Admin |
| SDM-02 | Admin can assign/change a student's department | Must | Admin |
| SDM-03 | Admin can set a student's academic year and current semester | Must | Admin |
| SDM-04 | Admin can add a course record for a student: course, status (Completed/Failed/InProgress), grade (for Completed/Failed), semester taken, academic year | Must | Admin |
| SDM-05 | Admin can edit a student's course record (change grade, status) | Must | Admin |
| SDM-06 | Admin can remove a student's course record | Must | Admin |
| SDM-07 | When admin enters a grade, system automatically determines status: grade ≥ passing threshold → Completed; grade < passing threshold → Failed | Must | System |
| SDM-08 | Admin can mark a course as "InProgress" for a student (no grade yet) | Must | Admin |
| SDM-09 | System automatically calculates and displays the student's CGPA and per-semester GPA when viewing their profile | Must | System |
| SDM-10 | System automatically determines and displays the student's Academic Standing based on GPA history | Must | System |

### 7.3 Views

| View | Route | Description |
|---|---|---|
| Student List | `/Admin/Students` | Table of all students with search + department filter |
| Student Detail | `/Admin/Students/{id}` | Full student profile: demographics, department, year, semester, CGPA, standing, course history table |
| Add Course Record | `/Admin/Students/{id}/AddCourse` | Form: select course from catalog + grade dropdown + semester/year |
| Edit Course Record | `/Admin/Students/{id}/EditCourse/{recordId}` | Edit grade, status |

### 7.4 Student Course Record Fields

| Field | Type | Required | Notes |
|---|---|---|---|
| StudentId | FK | Yes | — |
| CourseId | FK | Yes | Selected from department's course catalog |
| Status | enum | Yes | Completed / Failed / InProgress (auto-determined from grade if grade provided) |
| Grade | string | Conditional | Required if status is Completed or Failed; null if InProgress |
| Semester | enum | Yes | Fall / Spring / Summer |
| AcademicYear | string | Yes | e.g., "2024-2025" |

---

## 8. Module 4: Student — Dashboard

### 8.1 Description

The student's landing page after login. Displays key academic metrics at a glance with visual indicators and quick-access navigation.

### 8.2 Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| SD-01 | Dashboard displays the student's name, department, academic year, and current semester | Must | System |
| SD-02 | Dashboard displays current CGPA with visual indicator (color-coded: green ≥ 3.0, yellow 2.0–2.99, red < 2.0) | Must | System |
| SD-03 | Dashboard displays credit hours completed vs. total required, with a progress bar | Must | System |
| SD-04 | Dashboard displays number of courses remaining | Must | System |
| SD-05 | Dashboard displays projected graduation semester (based on current pace and credit limits) | Must | System |
| SD-06 | Dashboard displays current Academic Standing with color-coded badge (Good ✅, Warning ⚠️, Probation 🟠, Dismissed 🔴) | Must | System |
| SD-07 | If standing is Warning or Probation, a prominent alert explains consequences | Should | System |
| SD-08 | If GPA ≥ 3.0, show "You qualify for overload registration" badge | Should | System |
| SD-09 | If graduation is mathematically impossible (even with all remaining A's, CGPA < 2.0), show a critical alert | Should | System |
| SD-10 | Dashboard provides quick-access cards/links to: Course Map, Impact Analyzer, Progress & Planning, AI Advisor | Must | System |
| SD-11 | If student qualifies for Honor List / Dean's List / President's List (based on latest semester GPA), show a motivational badge | Nice | System |

### 8.3 Views

| View | Route | Description |
|---|---|---|
| Student Dashboard | `/Student` or `/Student/Dashboard` | Main landing page with all metric cards |

### 8.4 Calculated Fields (Server-Side)

| Metric | Calculation |
|---|---|
| CGPA | `Σ(creditHours × gradePoints) / Σ(creditHours)` across all Completed + Failed courses |
| Latest SGPA | Same formula but only for the most recent semester |
| Credits Completed | `Σ(creditHours)` of all Completed courses |
| Courses Remaining | Total courses in graduation requirements minus completed courses |
| Projected Graduation | `(remainingCredits / maxCreditsPerSemester)` semesters from now, adjusted by current standing's credit limit |
| Max Achievable CGPA | `(currentTotalPoints + remainingCredits × 4.0) / (currentTotalCredits + remainingCredits)` |

---

## 9. Module 5: Student — Course Map

### 9.1 Description

An interactive, visual graph of every course in the student's department. Courses are nodes, prerequisites are directed edges. Nodes are color-coded by the student's personal completion status.

### 9.2 Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| CM-01 | Display all active courses in the student's department as an interactive directed graph | Must | System |
| CM-02 | Prerequisite relationships are shown as directed edges (arrows: prerequisite → dependent course) | Must | System |
| CM-03 | Nodes are color-coded by student's status: green (Completed), blue (In-Progress), gray (Remaining — prerequisites met), dark/locked (Blocked — prerequisites not met) | Must | System |
| CM-04 | Red nodes for Failed courses | Must | System |
| CM-05 | Student can click a course node to see a detail popup: course code, name, credits, type, prerequisites list, status, grade (if any) | Must | System |
| CM-06 | Graph is zoomable and pannable | Should | System |
| CM-07 | Graph layout automatically organizes courses by semester level (left-to-right or top-to-bottom) | Should | System |
| CM-08 | A legend explains the color coding | Must | System |

### 9.3 Implementation Notes

- **Controller** renders the Razor view and passes course + prerequisite data as a serialized JSON object (via `ViewBag` or a `<script>` block).
- **Cytoscape.js** (loaded from CDN or `wwwroot/lib/`) renders the graph client-side.
- Course status and color-coding is **computed server-side** by the controller/service and included in the JSON data passed to the view.
- Node positions can use Cytoscape's built-in layout algorithms (e.g., `dagre` for directed acyclic graphs).

### 9.4 Views

| View | Route | Description |
|---|---|---|
| Course Map | `/Student/CourseMap` | Full-page interactive graph with legend |

### 9.5 JSON Data Structure (Controller → View)

```json
{
  "nodes": [
    { "id": "CS101", "label": "Intro to CS", "credits": 3, "status": "completed", "grade": "A" },
    { "id": "CS201", "label": "Data Structures", "credits": 3, "status": "inProgress", "grade": null },
    { "id": "CS301", "label": "Algorithms", "credits": 3, "status": "blocked", "grade": null }
  ],
  "edges": [
    { "source": "CS101", "target": "CS201" },
    { "source": "CS201", "target": "CS301" }
  ]
}
```

---

## 10. Module 6: Student — Impact Analyzer ⚡

### 10.1 Description

The core differentiating feature. A student selects a course and simulates failing it. The system instantly performs a forward cascade analysis and shows all blocked courses, graduation impact, and a recovery path.

### 10.2 Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| IA-01 | Student can select any Completed or In-Progress course on the Course Map and choose "Simulate Failure" | Must | Student |
| IA-02 | System performs BFS/DFS traversal from the selected course forward through the prerequisite graph | Must | System |
| IA-03 | System identifies all courses **directly** blocked (the failed course is their immediate prerequisite) | Must | System |
| IA-04 | System identifies all courses **transitively** blocked (blocked because a directly-blocked course is their prerequisite, and so on) | Must | System |
| IA-05 | On the Course Map, the selected course turns red; blocked courses animate in a cascade effect (red pulse propagating forward) | Must | System |
| IA-06 | System displays a summary panel with: total blocked courses, semesters affected | Must | System |
| IA-07 | System suggests recovery: "Retake [course] in [next available semester based on SemesterAvailability]" | Must | System |
| IA-08 | System calculates graduation delay: "Graduation delayed by X semester(s)" or "No delay if retaken in [semester]" | Must | System |
| IA-09 | A green recovery path highlights on the graph showing the retake semester and when blocked courses unlock | Should | System |
| IA-10 | Student can clear the simulation and return to the normal Course Map view | Must | Student |
| IA-11 | Impact analysis is simulation-only; it does NOT modify actual student records | Must | System |
| IA-12 | If the selected course has no dependents, the system shows "No downstream courses are affected by this failure" | Must | System |

### 10.3 Algorithm: Cascade Detection

```
Input: courseId (the failed/dropped course), student's completedCourses, prerequisiteGraph
Output: list of blockedCourses, cascadeDepth, recoveryPath

1. Mark courseId as "failed" (temporarily, not in DB)
2. Initialize queue = [all courses that have courseId as a direct prerequisite]
3. BFS traversal:
   a. For each course in queue:
      - If all its prerequisites are NOT in completedCourses → mark as "blocked"
      - Add all courses that have THIS course as a prerequisite to the queue
   b. Continue until queue is empty
4. cascadeDepth = max depth level reached in traversal
5. recoveryPath:
   a. Find next available semester for courseId (based on SemesterAvailability and current semester)
   b. Count semesters until all blocked courses can be taken
   c. Compare with original graduation projection
6. Return results
```

### 10.4 Interaction Flow

```
1. Student opens Course Map → sees their normal color-coded graph
2. Student clicks a course node → detail popup appears
3. Student clicks "Simulate Failure" button in the popup
4. AJAX POST to /Impact/Analyze with courseId
5. Server runs cascade algorithm → returns JSON:
   { blockedCourses: [...], cascadeDepth: 2, delay: 1, recovery: {...} }
6. JavaScript animates the cascade on the graph (Cytoscape.js)
7. A summary panel slides in from the side showing details
8. Student clicks "Clear Simulation" → graph resets to normal
```

### 10.5 Views

| View | Route | Description |
|---|---|---|
| Impact Analysis (overlay on Course Map) | `/Student/CourseMap` (same page) | Analysis results displayed as overlay panel + graph animation |
| Analyze Endpoint (AJAX) | `POST /Impact/Analyze` | Returns JSON with cascade results |

---

## 11. Module 7: Student — Progress & Planning

### 11.1 Description

A unified page with two tabs: **Progress Tracker** (graduation audit) and **GPA Simulator** (grade prediction + target setting).

### 11.2 Progress Tracker — Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| PP-01 | Display overall credit hour progress: completed / required, with a progress bar | Must | System |
| PP-02 | Display progress grouped by graduation requirement category: Core, DeptElective, FreeElective, UniversityReq | Must | System |
| PP-03 | For each category, show: credits earned / credits required, and a list of completed courses + remaining courses | Must | System |
| PP-04 | For elective categories, show eligible courses the student can pick from | Must | System |
| PP-05 | Display projected graduation semester, accounting for credit hour limits based on current standing | Must | System |
| PP-06 | If GPA ≥ 3.0, show a secondary projection with overload pace: "With overload: Graduate [semester]" | Should | System |
| PP-07 | If min GPA for graduation (2.0) is mathematically unreachable, show a critical warning with explanation | Must | System |

### 11.3 GPA Simulator — Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| PP-08 | Display current In-Progress courses with their credit hours | Must | System |
| PP-09 | Student can select a predicted grade (A+ to F) for each in-progress course via dropdown | Must | Student |
| PP-10 | System calculates predicted SGPA and predicted CGPA **in real-time on the client side** (JavaScript) as the student adjusts grades | Must | System |
| PP-11 | Student can enter a target GPA → system back-calculates the minimum grade combination needed to reach it | Must | System |
| PP-12 | Courses eligible for grade improvement (current grade ≤ D+) are listed separately with a "🔄 Eligible for improvement" badge | Must | System |
| PP-13 | Student can simulate retaking an improvable course: select a new predicted grade → GPA recalculates with the new grade replacing the original | Must | Student |
| PP-14 | If predicted grades result in a standing change (e.g., Warning → Good, or Good → Warning), show an alert: "With these grades, your standing next semester will be [X]" | Should | System |
| PP-15 | If predicted SGPA qualifies for a recognition list (Honor ≥ 3.0, Dean's ≥ 3.5, President's ≥ 3.75), show a motivational badge | Should | System |

### 11.4 GPA Calculation Logic (Client-Side JavaScript)

```javascript
// Called whenever a grade dropdown changes
function calculatePredictedGpa() {
  let predictedCredits = 0;
  let predictedPoints = 0;
  
  // Existing completed courses (passed from server)
  let existingCredits = existingTotalCredits;  // from ViewBag
  let existingPoints = existingTotalPoints;    // from ViewBag
  
  // In-progress courses with predicted grades
  for (const course of inProgressCourses) {
    const gradePoints = gradeScale[course.selectedGrade]; // e.g., "A" → 4.0
    predictedCredits += course.creditHours;
    predictedPoints += course.creditHours * gradePoints;
  }
  
  // Grade improvement courses (replace original)
  for (const course of improvementCourses) {
    if (course.selectedNewGrade) {
      // Remove old points, add new
      existingPoints -= course.creditHours * gradeScale[course.originalGrade];
      existingPoints += course.creditHours * gradeScale[course.selectedNewGrade];
    }
  }
  
  const predictedSGPA = predictedPoints / predictedCredits;
  const predictedCGPA = (existingPoints + predictedPoints) / (existingCredits + predictedCredits);
  
  updateDisplay(predictedSGPA, predictedCGPA);
}
```

### 11.5 Views

| View | Route | Description |
|---|---|---|
| Progress & Planning | `/Student/Progress` | Tabbed page: Tab 1 = Progress Tracker, Tab 2 = GPA Simulator |

---

## 12. Module 8: Student — AI Advisor

### 12.1 Description

A chat interface where students ask natural-language academic questions. The system injects the student's real academic data as context, sends the query to OpenAI's GPT, and returns a personalized, data-driven response.

### 12.2 Functional Requirements

| ID | Requirement | Priority | Actor |
|---|---|---|---|
| AI-01 | Student sees a chat interface with a text input and a message history area | Must | Student |
| AI-02 | Student types a question and submits it | Must | Student |
| AI-03 | System constructs a prompt containing: student profile, completed courses, grades, CGPA, standing, in-progress courses, and any recent impact analysis results | Must | System |
| AI-04 | System sends the prompt + student question to OpenAI API (GPT-3.5-turbo) | Must | System |
| AI-05 | AI response is displayed in the chat area as a message from "Advisor" | Must | System |
| AI-06 | Chat history is maintained within the current browser session (not persisted to DB) | Should | System |
| AI-07 | If OpenAI API is unavailable or returns an error, display a graceful fallback: "The AI Advisor is currently unavailable. Please try again later or visit the Progress Tracker for detailed academic insights." | Must | System |
| AI-08 | A loading indicator is shown while waiting for AI response | Must | System |
| AI-09 | AI responses are advisory only; no system actions or data modifications are triggered | Must | System |
| AI-10 | System uses GPT-3.5-turbo model for cost efficiency | Must | System |

### 12.3 System Prompt Template

```
You are a friendly and supportive academic advisor at [University Name]. 
You help students understand their academic situation and make informed decisions.

You have access to the following student data:
- Name: {studentName}
- Department: {department}
- Academic Year: {year}, Current Semester: {semester}
- Cumulative GPA: {cgpa}
- Academic Standing: {standing}
- Credits Completed: {completed}/{total}
- Projected Graduation: {projectedGraduation}

Completed Courses:
{completedCoursesList}

In-Progress Courses:
{inProgressCoursesList}

Failed/Blocked Courses:
{failedCoursesList}

Guidelines:
- Be supportive and encouraging, but honest about academic risks.
- Always reference specific course codes and names when relevant.
- If the student asks about consequences of failing a course, 
  suggest they use the Impact Analyzer for detailed cascade analysis.
- Keep responses concise (3–5 paragraphs maximum).
- Do not make up course names or requirements that aren't in the data above.
```

### 12.4 Interaction Flow

```
1. Student opens AI Advisor page
2. Student types: "What should I take next semester?"
3. AJAX POST to /AiAdvisor/Ask with { message: "..." }
4. Server:
   a. Loads student profile, courses, GPA, standing
   b. Constructs system prompt with context
   c. Sends to OpenAI API
   d. Returns response as JSON { response: "..." }
5. JavaScript appends AI response to chat container
6. Student can continue the conversation (history maintained in JS array,
   sent with each request for multi-turn context)
```

### 12.5 Views

| View | Route | Description |
|---|---|---|
| AI Advisor | `/Student/AiAdvisor` | Full-page chat interface |
| Ask Endpoint (AJAX) | `POST /AiAdvisor/Ask` | Accepts question, returns AI response as JSON |

---

## 13. Non-Functional Requirements

| ID | Category | Requirement | Metric |
|---|---|---|---|
| NFR-01 | **Performance** | All server-rendered pages load within 2 seconds | Page load time |
| NFR-02 | **Performance** | Course Map graph renders within 3 seconds for up to 80 course nodes | Client-side render time |
| NFR-03 | **Performance** | Impact analysis calculation completes within 1 second | Server response time |
| NFR-04 | **Security** | Passwords hashed using ASP.NET Identity's default (PBKDF2) | — |
| NFR-05 | **Security** | All form inputs validated server-side (model validation) and client-side (unobtrusive validation) | — |
| NFR-06 | **Security** | CSRF protection enabled on all POST forms (ASP.NET's `[ValidateAntiForgeryToken]`) | — |
| NFR-07 | **Security** | Role-based authorization enforced on all controller actions | — |
| NFR-08 | **Usability** | UI is in English only | — |
| NFR-09 | **Usability** | Responsive design: functional on desktop (1280px+) and tablet (768px+) | Viewport testing |
| NFR-10 | **Reliability** | AI Advisor gracefully degrades when OpenAI API is unavailable | Fallback message shown |
| NFR-11 | **Maintainability** | Code follows MVC conventions: thin controllers, business logic in services, no logic in views | Code review |
| NFR-12 | **Maintainability** | All entities use data annotations or Fluent API for validation | — |
| NFR-13 | **Scalability** | Database schema supports multiple universities and departments without structural changes | Schema review |
| NFR-14 | **Deployment** | Application deployable to free-tier hosting with a public URL | Demo verification |

---

## 14. Academic Rules Reference

*Consolidated from the Project Plan. All rules are configurable via Admin settings.*

### 14.1 GPA Scale (4.0 — Default)

| Grade | Points | | Grade | Points |
|---|---|---|---|---|
| A+ | 4.0 | | C+ | 2.3 |
| A  | 4.0 | | C  | 2.0 |
| A- | 3.7 | | C- | 1.7 |
| B+ | 3.3 | | D+ | 1.3 |
| B  | 3.0 | | D  | 1.0 |
| B- | 2.7 | | F  | 0.0 |

### 14.2 Academic Standing Rules

| Condition | Standing | Credit Limit |
|---|---|---|
| Semester GPA ≥ 2.0 | Good | 18 (or 21 if CGPA ≥ 3.0) |
| Semester GPA < 2.0 — 1st consecutive | Warning | 18 |
| Semester GPA < 2.0 — 2nd consecutive | Probation | 12–14 |
| Semester GPA < 2.0 — 3rd consecutive OR CGPA < 1.0 | Dismissed | — |

### 14.3 Grade Improvement

Eligible if original grade ≤ D+ (1.3 points). New grade replaces original in GPA calculation.

### 14.4 Graduation Requirements

- Minimum CGPA: 2.0
- Total credits: Defined per department (e.g., 132)
- Four categories: Core, DeptElective, FreeElective, UniversityReq

### 14.5 Honor Recognition (based on Semester GPA)

| Recognition | Threshold |
|---|---|
| Honor List | SGPA ≥ 3.0 |
| Dean's List | SGPA ≥ 3.5 |
| President's List | SGPA ≥ 3.75 |

### 14.6 Course Semester Availability

| Value | Meaning |
|---|---|
| Fall | Fall only |
| Spring | Spring only |
| FallSpring | Fall and Spring |
| All | Fall, Spring, and Summer |

---

## 15. Data Dictionary

### 15.1 Entity Relationship Diagram (Conceptual)

```
University (1) ────► (N) Department (1) ────► (N) Course
                            │                       │
                            │               CoursePrerequisite
                            │               (self-ref M:N)
                            │
                            ├──► (N) GraduationRequirement
                            │         │
                            │         └──► (N) GraduationRequirementCourse
                            │
                            └──► CreditHourRule (per standing)

User (ASP.NET Identity)
  └──► Student (1) ────► (N) StudentCourse ◄──── Course
          │
          └──► (N) StandingHistory
          
University (1) ────► (N) GradeScale
```

### 15.2 Entity Specifications

#### University
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| Name | nvarchar(100) | Required, unique |

#### Department
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| Name | nvarchar(100) | Required |
| UniversityId | int | FK → University |
| TotalCreditsRequired | int | Required (e.g., 132) |
| MinGpaForGraduation | decimal(3,2) | Required, default 2.00 |

#### Course
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| Code | nvarchar(10) | Required, unique per department |
| Name | nvarchar(100) | Required |
| CreditHours | int | Required, range 1–6 |
| CourseType | enum (int) | Required (Core=0, DeptElective=1, FreeElective=2, UniversityReq=3) |
| SemesterAvailability | enum (int) | Required (Fall=0, Spring=1, FallSpring=2, All=3) |
| PassingGradeThreshold | nvarchar(2) | Required, default "D" |
| DepartmentId | int | FK → Department |
| IsActive | bit | Default true |

#### CoursePrerequisite
| Column | Type | Constraints |
|---|---|---|
| CourseId | int | FK → Course (the course that has the prerequisite), PK part 1 |
| PrerequisiteId | int | FK → Course (the prerequisite course), PK part 2 |

*Composite PK: (CourseId, PrerequisiteId). Constraint: CourseId ≠ PrerequisiteId.*

#### Student
| Column | Type | Constraints |
|---|---|---|
| Id | nvarchar | PK, FK → AspNetUsers.Id |
| DepartmentId | int | FK → Department, nullable (assigned by admin) |
| AcademicYear | nvarchar(10) | e.g., "2024-2025" |
| CurrentSemester | enum (int) | Fall=0, Spring=1, Summer=2 |
| CurrentStanding | enum (int) | Good=0, Warning=1, Probation=2, Dismissed=3 |

#### StudentCourse
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| StudentId | nvarchar | FK → Student |
| CourseId | int | FK → Course |
| Status | enum (int) | Completed=0, Failed=1, InProgress=2 |
| Grade | nvarchar(2) | Nullable (null if InProgress) |
| Semester | enum (int) | Fall=0, Spring=1, Summer=2 |
| AcademicYear | nvarchar(10) | e.g., "2024-2025" |

*Unique constraint: (StudentId, CourseId, AcademicYear, Semester) — a student can retake a course in a different semester.*

#### GraduationRequirement
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| DepartmentId | int | FK → Department |
| CategoryType | enum (int) | Core=0, DeptElective=1, FreeElective=2, UniversityReq=3 |
| RequiredCredits | int | Required |

#### GraduationRequirementCourse
| Column | Type | Constraints |
|---|---|---|
| GraduationRequirementId | int | FK → GraduationRequirement, PK part 1 |
| CourseId | int | FK → Course, PK part 2 |

#### GradeScale
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| UniversityId | int | FK → University |
| LetterGrade | nvarchar(2) | Required (e.g., "A+", "B-") |
| PointValue | decimal(3,2) | Required (e.g., 4.00) |

*Unique constraint: (UniversityId, LetterGrade)*

#### StandingHistory
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| StudentId | nvarchar | FK → Student |
| Semester | enum (int) | Fall=0, Spring=1, Summer=2 |
| AcademicYear | nvarchar(10) | e.g., "2024-2025" |
| SemesterGpa | decimal(3,2) | Calculated |
| CumulativeGpa | decimal(3,2) | Calculated |
| Standing | enum (int) | Good=0, Warning=1, Probation=2, Dismissed=3 |

*Unique constraint: (StudentId, Semester, AcademicYear)*

#### CreditHourRule
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-increment |
| DepartmentId | int | FK → Department |
| Standing | enum (int) | Good=0, Warning=1, Probation=2 |
| MinCredits | int | Required |
| MaxCredits | int | Required |

### 15.3 Total Entity Count: 10 tables + ASP.NET Identity tables

---

## 16. UI/UX Guidelines

### 16.1 Layout

- **Shared layout** (`_Layout.cshtml`): Top navbar with logo, navigation links (role-based), and user menu (profile, logout)
- **Student navigation:** Dashboard | Course Map | Impact Analyzer | Progress & Planning | AI Advisor
- **Admin navigation:** Dashboard | Courses | Students | Graduation Req. | Settings

### 16.2 Styling

- Use **Bootstrap 5** (comes with ASP.NET MVC template) for layout, grid, forms, cards, badges, and modals
- Custom CSS for: Course Map node colors, progress bars, dashboard cards, chat interface
- Color palette for academic status:

| Status | Color | CSS Class |
|---|---|---|
| Good / Completed | Green (#28a745) | `.status-good` |
| Warning / InProgress | Blue/Amber (#ffc107) | `.status-warning` |
| Probation / Failed | Orange/Red (#dc3545) | `.status-danger` |
| Blocked | Dark gray (#6c757d) | `.status-blocked` |
| Recovery Path | Green accent (#20c997) | `.status-recovery` |

### 16.3 Interactive Component Libraries

| Library | Version | Purpose | Loaded From |
|---|---|---|---|
| Bootstrap | 5.x | Layout, components, responsive | Bundled (default MVC template) |
| Cytoscape.js | 3.x | Course Map graph visualization | CDN or `wwwroot/lib/` |
| cytoscape-dagre | latest | Directed graph layout for Cytoscape | CDN or `wwwroot/lib/` |
| jQuery | 3.x | AJAX calls, DOM manipulation | Bundled (default MVC template) |

### 16.4 Partial Views

Use Razor partial views for reusable UI components:
- `_AcademicStandingBadge.cshtml` — colored badge for standing
- `_ProgressBar.cshtml` — credit progress bar component
- `_HonorBadge.cshtml` — honor list / dean's list badge
- `_CourseStatusBadge.cshtml` — colored badge for course status

---

## 17. Glossary

| Term | Definition |
|---|---|
| **Cascade** | The chain effect where failing one course blocks multiple downstream courses |
| **CGPA** | Cumulative Grade Point Average — across all semesters |
| **SGPA** | Semester Grade Point Average — for one semester |
| **Standing** | Academic status determined by GPA thresholds |
| **Overload** | Registering for above-normal credit hours (requires GPA ≥ 3.0) |
| **Prerequisite** | A course that must be passed before taking another course |
| **In-Progress** | A course the student is currently enrolled in (no final grade yet) |
| **Blocked** | A course whose prerequisites have not all been passed |
| **Grade Improvement** | Retaking a course with a low grade (≤ D+) to earn a higher grade |
| **Soft Delete** | Marking a record as inactive instead of deleting it from the database |
| **Seed Data** | Pre-loaded data inserted into the database on first run |

---

## 18. Approval

> **By signing, all team members confirm they have read this SRS, understand the requirements, and agree to implement the system as specified.**

---

*Document Version 1.0 — March 2, 2026*
