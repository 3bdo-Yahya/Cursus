**DEPI GRADUATION PROJECT PROPOSAL**

Full-Stack Web Development \| .NET & React

**Cursus**

*Your AI-Powered Academic Advisor & Smart Graduation Planner*

  -----------------------------------------------------------------------
  **Program:**                 Full-Stack Web Development (.NET)
  ---------------------------- ------------------------------------------
  **Team Size:**               6 Members

  **Duration:**                3--4 Months

  **Date:**                    March 2026
  -----------------------------------------------------------------------

# 1. Executive Summary

  -----------------------------------------------------------------------
  Cursus is a smart, AI-assisted academic advising platform built for
  university students. It transforms the way students navigate their
  academic journey --- from tracking completed courses and prerequisites,
  to intelligently recovering from a failed course, to planning the
  optimal path toward on-time graduation. Unlike traditional portals that
  simply display data, Cursus actively guides students through decisions,
  especially in moments of academic crisis.
  -----------------------------------------------------------------------

  -----------------------------------------------------------------------

# 2. Problem Statement

Egyptian and Arab-world university students face a set of deeply
frustrating and costly academic challenges that are currently managed
through informal, error-prone, and stressful processes:

## 2.1 The Academic Crisis Moment

When a student fails or drops a course, the consequences are rarely
immediate or obvious. A failed course may be a prerequisite for two or
three upcoming courses, silently collapsing the student\'s entire
planned semester. Students are left to manually trace these dependency
chains on their own --- often discovering the damage too late to act.

## 2.2 Overloaded and Inaccessible Academic Advisors

Academic advisors in Egyptian universities are typically responsible for
hundreds of students simultaneously. The result is brief, rushed
consultations that lack personalization. Students receive generic advice
rather than guidance tailored to their specific course history, GPA
standing, and graduation timeline.

## 2.3 No Graduation Visibility

Most students have no reliable way to know whether they are on track to
graduate on time. They lack tools to visualize remaining requirements,
detect missing prerequisites, or understand how current decisions will
affect their graduation date --- until it is too late.

## 2.4 Scope Creep on Academic Decisions

Students frequently make course registration decisions without
understanding the downstream consequences --- choosing electives that
conflict with required courses, underloading in a way that forces an
extra semester, or overloading in a way that tanks their GPA. These
decisions compound over time with no feedback mechanism.

## 2.5 No Personalized Insights

There is no system that looks at a student\'s full academic history and
says: \"Based on where you are, here is what you should focus on, what
risks you face, and what the best path forward looks like.\" Students
navigate blindly.

# 3. Proposed Solution

Cursus is a full-stack web platform built on ASP.NET Core and React that
serves as every university student\'s personal academic operating
system. The platform does not merely display academic data --- it
actively reasons about it, surfaces risks, and proposes concrete
actions.

The platform operates on a core principle: every academic decision a
student makes has consequences that ripple forward through their degree.
Cursus makes those consequences visible before they become irreversible.

+-----------------------------------------------------------------------+
| **Core Value Proposition**                                            |
|                                                                       |
| *\"Cursus turns your academic advisor\'s office into a 24/7           |
| intelligent platform --- one that knows your full history,            |
| understands your degree requirements, and tells you exactly what to   |
| do next, even when things go wrong.\"*                                |
+=======================================================================+
+-----------------------------------------------------------------------+

# 4. Target Users and Roles

  -----------------------------------------------------------------------
  **Role**         **User Type**       **Primary Responsibility**
  ---------------- ------------------- ----------------------------------
  Student          Primary End User    Tracks academic progress, receives
                                       guidance, plans semesters, and
                                       views personal insights.

  Academic Advisor Staff / Secondary   Reviews flagged students, provides
                   User                notes, monitors at-risk cases, and
                                       manages advisee portfolios.

  Department Admin Staff / Power User  Manages curriculum data: courses,
                                       prerequisites, credit
                                       requirements, and graduation
                                       conditions.

  Super Admin      System              Manages universities, departments,
                   Administrator       user roles, and platform-wide
                                       configuration.
  -----------------------------------------------------------------------

# 5. System Features

## 5.1 Drop / Fail Impact Analyzer 🚨

The most critical and innovative feature of the platform. When a student
reports a failed or dropped course, the system immediately performs a
forward cascade analysis:

-   Identifies all courses in the student\'s upcoming plan that require
    the failed course as a prerequisite.

-   Highlights which courses are now blocked for the next semester.

-   Suggests valid alternative courses the student can take instead.

-   Recalculates the expected graduation date based on the new
    situation.

-   Generates a recovery plan --- the optimal path to get back on track
    with minimum time loss.

## 5.2 Smart Semester Planner

An intelligent course recommendation engine that suggests an optimal
course load for the upcoming semester based on:

-   Completed courses and remaining graduation requirements.

-   Prerequisite chains and course availability.

-   Current GPA standing and credit hour load recommendations.

-   Historical data on course difficulty and failure rates within the
    department.

## 5.3 Graduation Audit & Requirement Tracker

A comprehensive visualization of the student\'s full degree map:

-   All graduation requirements broken down by category (core,
    electives, free electives).

-   Clear visual indicator of completed, in-progress, and remaining
    courses.

-   Projected graduation date based on current pace.

-   What-If Simulator: allows the student to test scenarios such as
    switching majors, adding a minor, or changing their semester load
    --- and see the impact instantly.

## 5.4 Academic Health Dashboard

A personalized academic summary updated each semester:

-   GPA trend chart across all semesters.

-   Credit hour completion rate vs. expected pace.

-   Academic standing indicator: Good Standing, Academic Warning,
    Academic Probation.

-   Courses at risk flag based on prerequisites and current enrollment
    patterns.

-   End-of-semester academic report with a plain-language summary of
    performance.

## 5.5 AI-Powered Insights Engine

A smart advisory layer built on top of the student\'s academic data:

-   Personalized course focus recommendations for the next semester.

-   Pattern recognition: identifies courses where similar students
    historically struggled.

-   Natural language academic report generated at the end of each
    semester.

-   Arabic-language AI chatbot assistant: students can ask questions
    such as \"Can I still graduate on time?\" or \"What happens if I
    drop Calculus 2?\" and receive intelligent, data-driven answers.

## 5.6 Academic Advisor Portal

A dedicated interface for academic advisors to manage their student
portfolio:

-   View all advisees at a glance with risk flags and GPA standing.

-   Receive automated alerts when a student fails a critical
    prerequisite.

-   Add notes and guidance to individual student profiles.

-   Approve or reject semester plans submitted by students.

-   Generate department-level reports on student academic health.

## 5.7 Curriculum Management System (Admin)

A powerful back-office tool for department admins to maintain the
academic data that drives the platform:

-   Define and edit course catalog with all metadata (credit hours,
    type, department).

-   Build and manage the prerequisite dependency graph visually.

-   Configure graduation requirements per major and academic year.

-   Manage academic calendar, semester availability of courses, and
    credit limits.

# 6. Functional Requirements

## 6.1 Authentication & Authorization

-   Users must be able to register, log in, and reset passwords
    securely.

-   Role-based access control (RBAC) must enforce permissions per role:
    Student, Advisor, Admin, Super Admin.

-   JWT-based authentication with refresh token support.

-   Students must be linked to a specific department and academic year
    upon registration.

## 6.2 Student Module

-   Students must be able to input or import their completed course
    history.

-   The system must map the student\'s completed courses against their
    major\'s graduation requirements.

-   Students must be able to report a failed or dropped course and
    immediately receive an impact analysis.

-   Students must be able to view a suggested semester plan and accept,
    modify, or reject it.

-   Students must be able to run what-if simulations on their degree
    plan.

-   Students must receive a generated academic summary report at the end
    of each semester.

## 6.3 Advisor Module

-   Advisors must be able to view all assigned students with their
    current GPA, progress, and risk flags.

-   Advisors must receive automated notifications when a student\'s GPA
    drops below a threshold or a critical prerequisite is failed.

-   Advisors must be able to annotate student profiles with notes
    visible to the student.

-   Advisors must be able to approve or reject semester plans before
    final registration.

## 6.4 Curriculum & Admin Module

-   Admins must be able to create, edit, and deactivate courses in the
    course catalog.

-   Admins must be able to define prerequisite relationships between
    courses.

-   Admins must be able to configure graduation requirement sets per
    major.

-   Admins must be able to set semester-level availability for each
    course.

## 6.5 AI & Analytics Module

-   The system must generate personalized course recommendations using
    the student\'s academic history and remaining requirements.

-   The AI chatbot must answer student questions in both Arabic and
    English using the student\'s own data as context.

-   The system must produce a readable, natural-language academic
    summary report per student per semester.

# 7. Non-Functional Requirements

  -------------------------------------------------------------------------
  **Category**       **Metric /          **Details**
                     Standard**          
  ------------------ ------------------- ----------------------------------
  Performance        Page load \< 2      All primary views must load within
                     seconds             2 seconds under normal load
                                         conditions.

  Scalability        Multi-institution   Architecture must support multiple
                     ready               universities and departments
                                         without re-engineering.

  Security           OWASP Top 10        Input validation, SQL injection
                     compliance          prevention, XSS protection, and
                                         HTTPS enforced throughout.

  Availability       99% uptime target   Core student-facing features must
                                         remain accessible during peak
                                         registration periods.

  Usability          Bilingual (AR / EN) Full Arabic and English support
                                         across all student-facing
                                         interfaces.

  Maintainability    Clean architecture  Backend follows Clean Architecture
                                         / CQRS pattern. Frontend follows
                                         component-based design.

  Data Integrity     Referential         Course prerequisites and
                     integrity           graduation requirements must
                                         maintain referential integrity at
                                         the DB level.

  Accessibility      WCAG 2.1 AA         Sufficient color contrast,
                                         keyboard navigation, and screen
                                         reader support on core views.
  -------------------------------------------------------------------------

# 8. Technical Architecture

  -----------------------------------------------------------------------
  **Layer**          **Technology / Tool**
  ------------------ ----------------------------------------------------
  Frontend           React.js, Tailwind CSS, React Query, React Router

  Backend            ASP.NET Core 8 Web API, C#, Clean Architecture

  Database           SQL Server --- relational model with graph-like
                     prerequisite chains

  Authentication     ASP.NET Identity + JWT with Refresh Tokens

  AI Layer           OpenAI API (GPT-4o) for chatbot and report
                     generation

  Deployment         Azure App Service or VPS (Ubuntu + Nginx + Docker)

  Version Control    Git + GitHub with branch-per-feature workflow

  Project Mgmt       Jira or GitHub Projects for sprint tracking
  -----------------------------------------------------------------------

# 9. Project Scope and Feasibility

## 9.1 In Scope

-   Full student academic tracking and graduation audit system.

-   Drop/fail impact analyzer with prerequisite cascade logic.

-   Smart semester planner with conflict detection.

-   Academic advisor portal with student risk dashboards.

-   Department admin panel for curriculum and prerequisite management.

-   AI chatbot assistant (Arabic + English) powered by OpenAI API.

-   Automated semester-end academic summary reports.

-   Bilingual user interface (Arabic and English).

-   Deployment on a publicly accessible URL for demo purposes.

## 9.2 Out of Scope

-   Direct integration with official university ERP or student
    information systems (e.g., Oracle Banner). Data will be entered
    manually for the demo.

-   Native mobile applications (iOS / Android). The platform will be
    responsive web only.

-   Real payment or billing functionality.

-   Live synchronization with official university grade systems.

## 9.3 Feasibility Analysis

+-----------------------------------------------------------------------+
| **Technical Feasibility**                                             |
|                                                                       |
| All core technologies are well within the team\'s declared stack.     |
| ASP.NET Core and React are the primary learning objectives of the     |
| course. The AI integration relies on well-documented OpenAI APIs. The |
| prerequisite graph logic is implementable in SQL Server using         |
| standard relational modeling. No exotic or unstable technology is     |
| required.                                                             |
+=======================================================================+
+-----------------------------------------------------------------------+

+-----------------------------------------------------------------------+
| **Time Feasibility**                                                  |
|                                                                       |
| With 3--4 months and a team of 6, the project is achievable if core   |
| features are prioritized and the curriculum data is collected in the  |
| first two weeks. The what-if simulator and AI chatbot are high-value  |
| features that can be added in the final month as enhancements once    |
| the core platform is stable.                                          |
+=======================================================================+
+-----------------------------------------------------------------------+

+-----------------------------------------------------------------------+
| **Data Feasibility**                                                  |
|                                                                       |
| The most critical dependency is obtaining the official curriculum     |
| data (courses, prerequisites, graduation requirements) from the       |
| team\'s own college. This should be treated as a first-week priority. |
| Approaching the academic affairs office directly and requesting the   |
| official study plans in any format (PDF, spreadsheet) is recommended  |
| immediately.                                                          |
+=======================================================================+
+-----------------------------------------------------------------------+

# 10. Suggested Project Timeline

  ------------------------------------------------------------------------
  **Phase**   **Duration**       **Key Deliverables**
  ----------- ------------------ -----------------------------------------
  1           Weeks 1--2         Requirements finalized, curriculum data
                                 collected, DB schema designed, Git repo
                                 and project structure set up.

  2           Weeks 3--6         Core backend APIs: Auth, Courses,
                                 Prerequisites, Graduation Requirements.
                                 Basic React shell and routing.

  3           Weeks 7--10        Drop/Fail Analyzer, Semester Planner,
                                 Graduation Audit UI, Advisor Portal ---
                                 fully functional.

  4           Weeks 11--13       AI Chatbot integration, Academic Health
                                 Dashboard, What-If Simulator, Bilingual
                                 support.

  5           Weeks 14--16       Testing, bug fixes, UI polish,
                                 deployment, documentation, and demo
                                 preparation.
  ------------------------------------------------------------------------

# 11. Suggested Team Structure

  -------------------------------------------------------------------------
  **Member**   **Role**              **Responsibilities**
  ------------ --------------------- --------------------------------------
  1            Backend Lead          Core API architecture, Auth, Database
                                     design, Prerequisite graph logic.

  2            Backend Developer     Semester Planner logic, Drop/Fail
                                     Analyzer engine, Advisor Portal APIs.

  3            Frontend Lead         React app architecture, Student
                                     Dashboard, Graduation Audit UI,
                                     Routing.

  4            Frontend Developer    Advisor Portal UI, Admin Panel,
                                     Semester Planner UI, Bilingual
                                     support.

  5            AI & Integration      OpenAI API integration, Chatbot,
                                     Report generation, Analytics features.

  6            DevOps & QA           Deployment, CI/CD, testing strategy,
                                     documentation, demo preparation.
  -------------------------------------------------------------------------

# 12. Competitive Advantage

Existing solutions such as DegreeWorks (USA), Stellic, and university
ERP portals are built for Western academic structures, cost hundreds of
thousands of dollars to implement, and are entirely inaccessible to
Egyptian universities. Cursus\'s competitive edge is threefold:

-   Localization: Built natively for Egyptian and Arab university
    structures --- Arabic language, local academic rules, and semester
    patterns specific to Egyptian colleges.

-   Crisis Intelligence: No existing platform has a dedicated Drop/Fail
    Impact Analyzer. This is the feature that makes Cursus proactive
    rather than passive --- it does not just show data, it tells
    students what to do when things go wrong.

-   Accessibility: A lightweight, web-based platform that can be adopted
    by any Egyptian university without million-dollar ERP contracts.

# 13. Success Metrics

The following metrics will be used to evaluate the success of Cursus at
demo and post-launch:

-   Accuracy of the prerequisite cascade engine: 100% of blocked courses
    correctly identified in all test scenarios.

-   Recovery plan quality: suggested alternative courses are always
    valid, available, and prerequisite-compliant.

-   GPA and graduation date projections match manual calculations in all
    test cases.

-   Advisor portal successfully surfaces at-risk students before the end
    of the demo scenario.

-   AI chatbot answers student academic questions accurately in both
    Arabic and English.

-   Platform loads and renders all primary views within 2 seconds on a
    standard connection.

# 14. Risks and Mitigation

  ------------------------------------------------------------------------
  **Risk**                 **Severity**    **Mitigation**
  ------------------------ --------------- -------------------------------
  Curriculum data          High            Start collection in Week 1.
  unavailable from college                 Manually build data for 2--3
                                           departments as demo scope.

  Prerequisite graph logic Medium          Build and test the graph
  more complex than                        traversal engine in isolation
  expected                                 first before integrating into
                                           the full platform.

  AI API costs exceed      Low             Use GPT-3.5-turbo for
  budget                                   development. Switch to GPT-4o
                                           only for final demo. Cache
                                           repeated queries.

  Scope creep leading to   High            Lock core feature scope at Week
  incomplete core features                 2. AI and advanced features are
                                           Phase 4 enhancements, not
                                           blockers.

  Team coordination issues Medium          Weekly standups, GitHub PR
  across 6 members                         reviews, and a shared Jira
                                           board with clearly owned
                                           tickets.
  ------------------------------------------------------------------------

***Cursus --- Turning Academic Chaos into Clarity***

DEPI Graduation Project 2026 \| Full-Stack Web Development (.NET &
React)
