# S1-006 — UI Shell Integration: Decision Log

## Overview

This document records key architectural decisions made while integrating the approved UI/UX shell into the Razor view system.

---

## Decision 1: Single Unified `_Navbar.cshtml` vs Separate Partials Per Role

| Approach | Pros | Cons |
|----------|------|------|
| **Single `_Navbar.cshtml` with role checks (chosen)** | One file to maintain, shared dark-mode/notification logic, active-state logic lives in one place, DRY | File is slightly longer (~100 lines) |
| Separate `_StudentNavbar.cshtml` + `_AdminNavbar.cshtml` | Each file is short and focused | Duplicates shared UI elements (dark toggle, user menu), requires syncing changes across files |

**Decision:** Single partial with `@if (User.IsInRole(...))` branching. The navbars share 60%+ of their structure (dark mode, notifications, user dropdown) — splitting would violate DRY and make theme/interaction changes error-prone.

---

## Decision 2: Post-Login Redirect Strategy

| Approach | Pros | Cons |
|----------|------|------|
| **`HomeController.Index()` role dispatcher (chosen)** | Zero configuration needed, works with default Identity login (which redirects to `~/`), no middleware overhead, no custom cookie events | Adds a redirect hop on first navigation |
| Cookie `OnSigningIn` event | Fires exactly at login | Ties routing logic to auth infrastructure, harder to test |
| Custom `AccountController.PostLogin` action | Explicit entry point | Requires modifying Identity pages to redirect to a non-standard URL |

**Decision:** Role-based redirect in `HomeController.Index()`. Identity's login defaults to `ReturnUrl ?? "~/"` — making the home page a smart router is the simplest, most testable approach. It also naturally handles deep links via `ReturnUrl`.

---

## Decision 3: Layout Architecture — Removing Bootstrap Navbar in Favor of Design System

| Aspect | Before | After |
|--------|--------|-------|
| **Navbar** | Default Bootstrap `navbar-expand-sm` with generic links | Custom `navbar-cursus` fixed-top with role-aware nav pills, dark mode toggle, notification panel, user avatar dropdown |
| **Font** | System defaults | Outfit (Google Fonts), Material Symbols Outlined for icons |
| **Global CSS** | Bootstrap + minimal site.css | Bootstrap + comprehensive `site.css` with design tokens, CSS variables for light/dark mode, smooth transitions |
| **Footer** | Basic copyright in a `container` | Branded footer matching the shell design |
| **Content area** | `<div class="container"><main>` | `<main style="padding-top:80px">` with `container-xl` for content width consistency |

**Decision:** Full replacement. The old layout used default MVC scaffolding which doesn't match the competition-grade design. The new layout loads the design system globally and the `@section Styles` mechanism allows page-specific CSS without polluting other routes.

---

## Decision 4: Student Dashboard ViewModel — Hardcoded Sample Data vs Database Queries

| Approach | Pros | Cons |
|----------|------|------|
| **Hardcoded sample data in controller (chosen)** | Sprint 1 scope, no enrollment tables yet, fast iteration, demonstrates data binding pattern | Not dynamic |
| Database queries | Real data | Requires enrollment/grade tables that don't exist in Sprint 1 |

**Decision:** Hardcoded in `StudentController.Dashboard()`. The ViewModel is typed with proper properties (`Cgpa`, `CreditsEarned`, computed `CreditPercentage`, etc.) so that when real data sources are available in future sprints, only the controller body changes — the view stays intact.

---

## Decision 5: Admin Dashboard — Keep Existing DB Metrics vs Static Redesign

| Approach | Pros | Cons |
|----------|------|------|
| **Keep `AdminDashboardViewModel` with live DB counts + new UI (chosen)** | Real data, demonstrates full-stack integration, leverages existing `AdminController.Index()` queries | None significant |
| Replace with hardcoded static UI only | Matches the prototype pixel-perfectly | Loses valuable real data binding |

**Decision:** Retained the existing database-driven metrics (universities, departments, courses, graduation requirements) but wrapped them in the approved Cursus design system (surface cards, icons, animations). This gives the admin dashboard both visual fidelity and functional value.

---

## Decision 6: Placeholder Student Actions (CourseMap, Planner, Progress)

The `StudentController` exposes `CourseMap()`, `Planner()`, and `Progress()` as empty `View()` returns. This allows:
- Nav pills to link to valid routes (no 404s)
- Tag Helpers (`asp-controller`, `asp-action`) to resolve at compile time
- Future sprint work to simply add the corresponding `.cshtml` views

No placeholder views were created — they'll return a framework error in dev mode, which is acceptable for Sprint 1 where only the Dashboard is in scope.

---

## File Summary

| File | Action | Purpose |
|------|--------|---------|
| `Views/Shared/_Layout.cshtml` | Rewritten | Global UI shell with Outfit font, Material Symbols, design tokens |
| `Views/Shared/_Navbar.cshtml` | Created | Role-based nav (Student/Admin/Guest) with dark mode, notifications, user menu |
| `Views/Shared/_Footer.cshtml` | Created | Branded footer partial |
| `Views/Shared/_LoginPartial.cshtml` | Unchanged | Still exists but no longer referenced (superseded by `_Navbar.cshtml` user menu) |
| `Views/Student/Dashboard.cshtml` | Rewritten | Full student dashboard with metric cards, course schedule, alerts |
| `Views/Admin/Index.cshtml` | Rewritten | Admin dashboard with DB-driven metrics and quick actions |
| `Views/Home/Index.cshtml` | Rewritten | Landing page for unauthenticated users |
| `Controllers/HomeController.cs` | Updated | Role-based redirect dispatcher |
| `Controllers/StudentController.cs` | Updated | ViewModel population, placeholder actions for future routes |
| `Models/StudentDashboardViewModel.cs` | Created | Typed ViewModel with computed properties |
