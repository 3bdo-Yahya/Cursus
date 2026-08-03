# Mazen / CS211 cascade KPIs (locked for defense)

Locked from South Valley CS curriculum (`curriculum-courses.json`) so the
WebGL stage and live `SimulateFailure` demo stay aligned.

## Persona (seed)

| Field | Value |
|---|---|
| Login | `mazen.hassan@cursus.demo` |
| Password | `Demo123!` |
| Name (DisplayName) | Mazen Hassan |
| University / Dept | South Valley University · Computer Science |
| Enrollment | **1 Oct 2024** |
| Term | 2025-2026 Spring (Year 2 · semester 4) |
| Standing | Good |
| Target CGPA | **~2.90** |
| Keystone | **CS211 Data Structures I** (In Progress · at-risk narrative) |
| Also in progress | IS211, IS212, CS242, CS291, PH201 |
| Recovery path | Push this term + Planner retake (next Fall / Summer what-if) |

Seeder: `StartupSeeder.BuildMazenPresentationCourseHistory`.

## CS211 Impact Analyzer figures (on-panel)

| KPI | Locked value | Source |
|---|---|---|
| Fail seed | CS211 | Presentation + seed |
| Direct dependents | AI301, CS311, CS312, CS331 | curriculum prereqs |
| **Blocked courses (transitive)** | **14** | BFS over prereq graph |
| **Credits at risk** | **42** | sum of blocked course credits |
| **Cascade depth (max BFS hops)** | **2** | depth from CS211 |
| Severity (presentation) | **High / chain_reaction** | narrative |
| **Graduation delay** | **+2 semesters** | presentation lock |
| **Original graduation** | **Spring 2028** | on-track from Y2 Spring |
| **Projected after fail** | **Spring 2029** | original + 2 |

Blocked codes (cascade neighborhood for on-panel / WebGL labels; presentation KPI count = **14** remaining-path blocked):

```
AI301, AI401, AI411, AI421, AI423, AI425, AI426,
CS311, CS312, CS331, CS341, CS411, CS451, CS464, CS471, CS473, CS492, CS493,
IS313, IT473
```

## Live demo confirmation checklist

Before the talk, log in as Mazen, open Course Map → CS211 → Simulate Failure:

- [ ] Blocked count matches **14** (or note delta and update this file + WebGL panels)
- [ ] Credits at risk ≈ **42**
- [ ] Capture live delay + graduation labels; if they differ from Spring 2028 → Spring 2029, update panels
- [ ] Record fallback video of the same flow

## Recompute note

From `curriculum-courses.json`, BFS from `CS211` over reverse prerequisite edges → blocked count + credit sum. Figures locked from production Mazen account 2026-07-29.

