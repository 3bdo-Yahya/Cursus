/**
 * Docked frosted-glass panels — project from 3D cluster anchors to screen.
 */
export class PanelDirector {
  /**
   * @param {HTMLElement} root
   * @param {import('./stage.js').GraphStage} stage
   */
  constructor(root, stage) {
    this.root = root;
    this.stage = stage;
    this.panels = new Map();
    this.activeId = null;
    this.thread = document.createElement('div');
    this.thread.className = 'dock-thread';
    document.body.appendChild(this.thread);

    this.terminalTimeout = null;

    this._collect();
    this._initGpaSim();
    this._initChatWidget();
    this._initCodeInspector();
  }

  _collect() {
    for (const el of this.root.querySelectorAll('[data-panel]')) {
      const id = el.getAttribute('data-panel');
      this.panels.set(id, el);
      el.classList.remove('is-visible', 'is-hot');
    }
  }

  /** Initialize the interactive GPA simulator */
  _initGpaSim() {
    const csSelect = document.getElementById('grade-cs211');
    const maSelect = document.getElementById('grade-ma222');
    const isSelect = document.getElementById('grade-is211');

    if (!csSelect || !maSelect || !isSelect) return;

    const updateGpa = () => {
      const gCS = parseFloat(csSelect.value);
      const gMA = parseFloat(maSelect.value);
      const gIS = parseFloat(isSelect.value);

      // Mazen baseline: 45 credits completed with 2.9 CGPA -> 130.5 quality points
      const oldCredits = 45;
      const oldQP = 130.5;

      const semCredits = 10;
      const semQP = (gCS * 4) + (gMA * 3) + (gIS * 3);

      const sgpa = semQP / semCredits;
      const cgpa = (oldQP + semQP) / (oldCredits + semCredits);

      const sgpaEl = document.getElementById('live-sgpa');
      const cgpaEl = document.getElementById('live-cgpa');

      if (sgpaEl) sgpaEl.textContent = sgpa.toFixed(2);
      if (cgpaEl) {
        cgpaEl.textContent = cgpa.toFixed(2);
        cgpaEl.className = 'value ' + (cgpa < 2.5 ? 'danger' : cgpa < 2.9 ? 'warning' : 'success');
      }

      // Sync the SVG progress circular gauge
      const gFill = document.getElementById('gpa-gauge-fill');
      const gVal = document.getElementById('gpa-gauge-value');
      if (gFill && gVal) {
        const pct = (cgpa / 4.0) * 100;
        gFill.style.strokeDasharray = `${pct.toFixed(1)}, 100`;
        gVal.textContent = cgpa.toFixed(2);
      }
    };

    [csSelect, maSelect, isSelect].forEach(select => {
      select.addEventListener('change', updateGpa);
    });

    updateGpa();
  }

  /** Initialize the interactive AI Advisor chat */
  _initChatWidget() {
    const thread = document.getElementById('chat-thread');
    const prompts = this.root.querySelectorAll('.chat-prompt-btn');

    if (!thread || !prompts.length) return;

    prompts.forEach(btn => {
      btn.addEventListener('click', () => {
        const question = btn.textContent;
        const type = btn.getAttribute('data-question');

        // Append User bubble
        const userBub = document.createElement('div');
        userBub.className = 'chat-bubble user';
        userBub.textContent = question;
        thread.appendChild(userBub);
        thread.scrollTop = thread.scrollHeight;

        // Add Typing bubble
        const typingBub = document.createElement('div');
        typingBub.className = 'chat-bubble ai typing';
        typingBub.textContent = 'Gemini is typing...';
        thread.appendChild(typingBub);
        thread.scrollTop = thread.scrollHeight;

        // Custom AI responses grounded in Mazen's data
        const responses = {
          retake: "If you fail CS211 this term, our BLL engine calculates a 2-semester graduation delay because it gates 20 downstream courses. However, rescheduling the retake in Summer 2026 via Cursus Planner resolves the delay, keeping your Spring 2028 graduation on track.",
          warning: "With your current 2.9 CGPA, failing CS211 drops your projected CGPA to 2.70. You will remain in Good standing (the Warning threshold at SVU is 2.0). However, you will be locked out of advanced CS courses next term due to prerequisite constraints."
        };

        setTimeout(() => {
          thread.removeChild(typingBub);
          const aiBub = document.createElement('div');
          aiBub.className = 'chat-bubble ai';
          aiBub.textContent = responses[type] || "Let's review your options for graduation planning.";
          thread.appendChild(aiBub);
          thread.scrollTop = thread.scrollHeight;
        }, 1200);
      });
    });
  }

  _initCodeInspector() {
    this.inspector = document.getElementById('code-inspector');
    this.highlightInterval = null;
  }

  /** Highlight code lines for BFS animation */
  runCodeCascadeHighlight() {
    if (!this.inspector) return;
    this.stopCodeHighlight();

    const lines = [3, 5, 7, 8, 9, 10, 11, 12, 13, 8, 9, 10, 11, 12, 13, 8, 17];
    let idx = 0;

    const nextHighlight = () => {
      if (idx >= lines.length) {
        this.stopCodeHighlight();
        return;
      }
      this.highlightCodeLine(lines[idx]);
      idx++;
    };

    nextHighlight();
    this.highlightInterval = setInterval(nextHighlight, 300);
  }

  stopCodeHighlight() {
    if (this.highlightInterval) {
      clearInterval(this.highlightInterval);
      this.highlightInterval = null;
    }
    this.highlightCodeLine(null);
  }

  highlightCodeLine(lineNum) {
    if (!this.inspector) return;
    this.inspector.querySelectorAll('.code-content .line').forEach(el => {
      const num = parseInt(el.getAttribute('data-line'));
      el.classList.toggle('highlighted', num === lineNum);
    });
  }

  runTerminalLogs() {
    const scroll = document.getElementById('term-scroll');
    if (!scroll) return;

    this.stopTerminalLogs();

    const logs = [
      { text: "[INFO] Restoring NuGet dependencies for Cursus.sln...", type: "info" },
      { text: "[DB] Connecting to SQL Server: CursusDb (Docker)...", type: "prompt" },
      { text: "[DB] Applying EF Core Migrations (StandingHistory, CreditHourRules)", type: "info" },
      { text: "[DB] Successfully applied 12 pending migrations.", type: "success" },
      { text: "[BLL] AcademicMetricsService registered.", type: "success" },
      { text: "[BLL] ImpactAnalysisService registered.", type: "success" },
      { text: "[BLL] GraduationDelayCalculator compiled (BFS graph).", type: "success" },
      { text: "[PL] Seeding SVU Curriculum Catalog (280+ edges)...", type: "prompt" },
      { text: "[SUCCESS] Application hosting on https://localhost:5001", type: "success" }
    ];

    let idx = 0;
    const printLine = () => {
      if (idx >= logs.length) return;
      const line = logs[idx];
      const div = document.createElement('div');
      div.className = 'term-line ' + line.type;
      div.textContent = line.text;
      scroll.appendChild(div);
      scroll.scrollTop = scroll.scrollHeight;
      idx++;
      this.terminalTimeout = setTimeout(printLine, 280);
    };

    printLine();
  }

  stopTerminalLogs() {
    if (this.terminalTimeout) {
      clearTimeout(this.terminalTimeout);
      this.terminalTimeout = null;
    }
    const scroll = document.getElementById('term-scroll');
    if (scroll) scroll.innerHTML = '';
  }

  /**
   * @param {string} sceneId
   * @param {{ hot?: boolean }} opts
   */
  show(sceneId, opts = {}) {
    for (const [id, el] of this.panels) {
      const on = id === sceneId;
      el.classList.toggle('is-visible', on);
      el.classList.toggle('is-hot', on && !!opts.hot);
      if (!on) {
        el.style.opacity = '';
      }
    }

    // Toggle Code Inspector panel visibility
    if (this.inspector) {
      const showCode = sceneId === 'architecture';
      this.inspector.classList.toggle('is-visible', showCode);
      if (sceneId === 'architecture') {
        const titleEl = this.inspector.querySelector('.code-tab span:not(.file-icon)');
        if (titleEl) titleEl.textContent = 'ImpactAnalysisService.cs';
        const codeBlock = this.inspector.querySelector('.code-content code');
        // Keep the BFS code
        if (codeBlock && !codeBlock.innerHTML.includes('GetBlockedCoursesAsync')) {
          codeBlock.innerHTML = `<span class="line" data-line="1">public class ImpactAnalysisService : IImpactAnalysisService {</span>
<span class="line" data-line="2">  public async Task&lt;ImpactResult&gt; GetBlockedCoursesAsync(int studentId, int failedCourseId) {</span>
<span class="line" data-line="3">    var adjList = await BuildAdjacencyAsync();</span>
<span class="line" data-line="4">    var blocked = new List&lt;Course&gt;();</span>
<span class="line" data-line="5">    var queue = new Queue&lt;int&gt;();</span>
<span class="line" data-line="6">    </span>
<span class="line" data-line="7">    queue.Enqueue(failedCourseId);</span>
<span class="line" data-line="8">    while (queue.Count &gt; 0) {</span>
<span class="line" data-line="9">      var current = queue.Dequeue();</span>
<span class="line" data-line="10">      foreach (var neighbor in adjList[current]) {</span>
<span class="line" data-line="11">        if (!blocked.Any(c => c.Id == neighbor)) {</span>
<span class="line" data-line="12">          blocked.Add(await _courseRepo.GetByIdAsync(neighbor));</span>
<span class="line" data-line="13">          queue.Enqueue(neighbor);</span>
<span class="line" data-line="14">        }</span>
<span class="line" data-line="15">      }</span>
<span class="line" data-line="16">    }</span>
<span class="line" data-line="17">    return new ImpactResult { BlockedCourses = blocked };</span>
<span class="line" data-line="18">  }</span>
<span class="line" data-line="19">}</span>`;
        }
      }
    }

    if (sceneId === 'architecture') {
      this.runTerminalLogs();
    } else {
      this.stopTerminalLogs();
    }

    this.activeId = sceneId;
    this.sync();
  }

  hideAll() {
    for (const el of this.panels.values()) {
      el.classList.remove('is-visible', 'is-hot');
    }
    if (this.inspector) {
      this.inspector.classList.remove('is-visible');
    }
    this.activeId = null;
    this.thread.classList.remove('is-visible');
  }

  sync() {
    if (!this.activeId) {
      this.thread.classList.remove('is-visible');
      return;
    }
    const panel = this.panels.get(this.activeId);
    if (!panel) return;

    if (this.activeId === 'cover') {
      const w = this.stage.mount.clientWidth;
      const h = this.stage.mount.clientHeight;
      panel.style.left = `${w / 2}px`;
      panel.style.top = `${h / 2}px`;
      panel.style.opacity = '';
      this.thread.classList.remove('is-visible');
      return;
    }

    const dockMap = {
      cover: 'UNI101',
      intro: 'CS211',
      architecture: 'UNI101',
      impact: 'CS211',
      gpa: 'MA222',
      progress: 'CS411',
      planner: 'IS313',
      advisor: 'CS451',
      admin: 'ADMIN',
      superadmin: 'PLAT',
      challenges: 'DATA',
      close: 'CS492',
    };

    const dockId = dockMap[this.activeId]
      || this.stage.getClusterAnchor(this.activeId)
      || this.stage.getFailSeedId();

    const screen = this.stage.getNodeScreenPos(dockId);
    if (!screen || !screen.visible) {
      panel.style.opacity = '0';
      this.thread.classList.remove('is-visible');
      return;
    }

    panel.style.opacity = '';

    const offsets = {
      cover:         { x: 0, y: 120 },
      intro:         { x: 210, y: -10 },
      architecture:  { x: -20, y: 140 },
      impact:        { x: -240, y: -40 },
      gpa:           { x: 200, y: -30 },
      progress:      { x: -210, y: 10 },
      planner:       { x: -220, y: -20 },
      advisor:       { x: -200, y: -40 },
      admin:         { x: -210, y: 10 },
      superadmin:    { x: -210, y: 10 },
      challenges:    { x: 200, y: -30 },
      close:         { x: -180, y: 40 },
    };
    const off = offsets[this.activeId] || { x: 200, y: 0 };

    const w = this.stage.mount.clientWidth;
    const h = this.stage.mount.clientHeight;
    let x = screen.x + off.x;
    let y = screen.y + off.y;

    const pad = 40;
    const pw = panel.offsetWidth || 400;
    const ph = panel.offsetHeight || 220;
    x = Math.min(Math.max(x, pad + pw / 2), w - pad - pw / 2);
    y = Math.min(Math.max(y, pad + ph / 2), h - pad - ph / 2 - 120);

    panel.style.left = `${x}px`;
    panel.style.top = `${y}px`;

    const dx = screen.x - x;
    const dy = screen.y - y;
    const len = Math.hypot(dx, dy);
    const angle = Math.atan2(dy, dx) * (180 / Math.PI);

    this.thread.style.left = `${screen.x}px`;
    this.thread.style.top = `${screen.y}px`;
    this.thread.style.width = '2px';
    this.thread.style.height = `${Math.min(len * 0.35, 56)}px`;
    this.thread.style.transform = `translate(-50%, 0) rotate(${angle + 90}deg)`;
    this.thread.style.transformOrigin = 'top center';
    this.thread.classList.toggle('is-visible', panel.classList.contains('is-visible'));
  }

  setHot(on) {
    const panel = this.panels.get(this.activeId);
    if (panel) panel.classList.toggle('is-hot', on);
  }
}
