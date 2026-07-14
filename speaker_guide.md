# Speaker Guide: Cursus Presentation

This guide outlines how to host, run, and narrate the restructured Cursus presentation. The deck is designed as a hybrid presentation, starting with a narrative user-focused hook (Slides 1–2) and transitioning into a structured technical defense (Slides 3–8).

---

## 🚀 How to Run the Presentation

### 1. Launching the Deck
The local presentation server is already active on your system. If you ever need to start it manually, run this command:
```bash
python3 -m http.server 8765 --bind 127.0.0.1
```
Then, open the following URL in any browser:
👉 [http://127.0.0.1:8765](http://127.0.0.1:8765)

### 2. Interaction & Keyboard Shortcuts
- **Next Slide / Advance**: Press `ArrowRight` or `Space` (or use the timeline scrubber at the bottom).
- **Previous Slide**: Press `ArrowLeft`.
- **Node Tooltips**: Hover over any 3D node in the WebGL background to display details on course name, credit hours, and current status.
- **Trigger Prerequisite Cascade**: Press the `C` key (or click the **Trigger Prereq Cascade Simulation** button) on Slide 6 (Live Application).
- **Heal the Graph**: Press the `H` key on Slide 6 or Slide 8 to simulate the retake path and watch the graph turn from failed red back to healthy states.

---

## 🗣️ Slide-by-Slide Script & Talk Track

### Slide 0: Cover (Ambient Opening)
* **Speaker**: None (Ambient WebGL constellation floats in the background)
* **Visual state**: Camera zoomed out, showing the entire 3D degree constellation.
* **Talk Track**:
  > *"Good morning, esteemed committee members. Today, we are proud to present Cursus: an AI-Powered Academic Advisor and Smart Graduation Planner. This is a full-stack web platform built for credit-hour university systems to map, analyze, and optimize a student's path to graduation."*

---

### Slide 1: Project Idea (Story Hook)
* **Speaker**: **Abdo**
* **Visual state**: Camera flies in to focus on `CS211` (Data Structures). Mazen's avatar dock appears at the bottom-left showing a "Worried" expression.
* **Talk Track**:
  > *"To understand the problem Cursus solves, let's meet Mazen Hassan, a sophomore CS student at South Valley University. Mazen looks fine on paper: he has a 2.9 CGPA and is in good standing. But this semester, his CS211 Data Structures course is at risk.*
  >
  > *In credit-hour systems, one silent failure cascades. Mazen doesn't know it, but CS211 is a keystone. If he fails it, the consequences cascade through his prerequisites—instantly blocking 20 downstream courses, delaying his graduation by a full year, and jeopardizing his standing. Cursus turns this invisible academic disaster into a visible, fully recoverable plan using deterministic prerequisite graph analysis."*

---

### Slide 2: Project Wireframe (User Journey)
* **Speaker**: **Abdo**
* **Visual state**: Camera stays focused on the central graph. Mazen's avatar changes to "Focused".
* **Talk Track**:
  > *"This journey is mapped into a cohesive, user-first workflow. In the live demo, we will show you how Mazen goes from onboarding to:
  > 1. Visualizing his entire prerequisite chain on the **Interactive Course Map**.
  > 2. Simulating failure on his at-risk course to run the **Prerequisite Impact Analysis**.
  > 3. Adjusting his targeted term grades in the **GPA Simulator** to check CGPA standing.
  > 4. Planning his retakes and recovery semesters in the **Semester Planner**, which are saved directly to the database.
  > 5. Consulting the **AI Advisor** for custom advice grounded in his real records."*

---

### Slide 3: End Users + Features (Personas)
* **Speaker**: **Esraa**
* **Visual state**: Camera shifts to the right (`DATA` node). Mazen's avatar is hidden.
* **Talk Track**:
  > *"Cursus is built as a multi-tenant platform with three isolated, role-based interfaces.
  > - **The Student** gets visual planning clarity: the Interactive Course Map, the Prerequisite Impact Analyzer, the Target GPA Simulator, and the AI Advisor.
  > - **The University Admin** has complete authority over the department catalog: configuring courses, defining prerequisite links, importing student grades, and adjusting credit-hour limits.
  > - **The Super Admin** controls the multi-tenant system: provisioning new university scopes, managing admin access, and ensuring complete data isolation between institutions like SVU, Sinai, and AUC."*

---

### Slide 4: Data Structure (Database & Schema)
* **Speaker**: **Hussein**
* **Visual state**: Camera moves to focus on `UNI101`. Code inspector drawer slides in on the right, and the mock terminal at the bottom begins typing EF database migrations.
* **Talk Track**:
  > *"At its core, Cursus relies on a highly relational prerequisite graph database. The database is managed using SQL Server with Entity Framework Core Code-First. 
  > 
  > We implemented 11 core domain entities, including University, Department, Course, and self-referencing relationships in CoursePrerequisite. As you can see in the terminal on screen, EF Core migrations automatically apply schema rules, foreign key constraints, and seed data for 280+ course prerequisite edges.
  > 
  > Our code inspector displays the actual C# Business Logic Service running the BFS graph traversal to calculate blocked courses recursively. Data flows cleanly from the database, through BLL services, into ViewModels, and up to the presentation layer."*

---

### Slide 5: Tech Stack (Architecture)
* **Speaker**: **Hazem**
* **Visual state**: Camera moves to `CS411`.
* **Talk Track**:
  > *"We built Cursus using a strict 4-layer N-tier architecture. Each layer is isolated inside its own .NET project: Cursus.Domain, Cursus.DAL for data access, Cursus.BLL for services, and Cursus.PL for the Web interface. 
  > 
  > We chose ASP.NET Core 10 MVC and C# for the robust, type-safe backend core. The database layer uses EF Core 10 and SQL Server. On the client side, we use vanilla JavaScript and Bootstrap 5 for responsiveness, combined with Cytoscape.js for rendering the interactive prerequisite graphs. 
  > 
  > For the AI Advisor, we integrated Google Gemini 2.5 Flash. We enforce the design principle that AI is the voice, not the brain—all computations are deterministic C# logic, and Gemini simply translates these results into natural academic recommendations."*

---

### Slide 6: Live Application (Staging & Demo)
* **Speaker**: **Ezz**
* **Visual state**: Camera moves to `IS313`. Progress rings show Core, Elective, and University requirements audit.
* **Talk Track**:
  > *"Cursus is fully production-ready and deployed live at cursus.runasp.net. The system performs real-time graduation audits, tracking a student's completion by category. 
  > 
  > Let's simulate the core feature of the platform. If we trigger the prerequisite cascade on Mazen's at-risk course—either by clicking the button or pressing 'C'—the BFS algorithm traverses the graph. Watch the screen: the cascade ripples downstream from CS211, instantly coloring blocked courses in red and showing Mazen the exact impact. 
  > 
  > When he schedules a retake or heals the graph (pressing 'H'), the system recalculates, showing his recovery plan is validated and his graduation is back on track."*

---

### Slide 7: Deliverables (Sprints & Artifacts)
* **Speaker**: **Tawfik**
* **Visual state**: Camera flies to `CS451`.
* **Talk Track**:
  > *"Our deliverables are fully realized. We have delivered:
  > 1. A comprehensive Software Requirements Specification (SRS) mapping all academic rules.
  > 2. The full source code repository hosted on GitHub, adhering to Gitflow Lite.
  > 3. The working, deployed web application.
  > 
  > We managed the project development lifecycle through ClickUp, completing three consecutive two-week agile sprints. This structured workflow ensured 100% feature coverage and build integrity."*

---

### Slide 8: Team + Roles (Agile & Conclusion)
* **Speaker**: **Tawfik & All**
* **Visual state**: Camera zooms out to the final view focusing on `CS492`.
* **Talk Track**:
  > *"Our team of six worked as Full-Stack Developers, collaborating on different tiers of the system. By coordinating on ClickUp and enforcing clean pull request workflows on GitHub, we resolved database and controller bottlenecks early. 
  > 
  > In conclusion, Cursus takes the anxiety out of credit-hour academic planning. It turns a static, confusing ledger into a dynamic course-map that empowers students to succeed. Mazen Hassan is indeed going to make it.
  > 
  > We now open the floor to the committee's questions. Thank you."*
