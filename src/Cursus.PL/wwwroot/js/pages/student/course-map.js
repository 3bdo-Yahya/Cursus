(function () {

const COURSES = [
  // Year 1
  { id:'CS121', name:'Computer Science Fundamentals', credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'passed',      grade:'A',  prereqs:[], year:1 },
  { id:'CS141', name:'Programming Fundamentals',       credits:3, type:'Core',        avail:'Spring',      dept:'CS', passing:'D', status:'passed',      grade:'B+', prereqs:[], year:1 },
  { id:'MA113', name:'Calculus I',                     credits:3, type:'Core',        avail:'Fall',        dept:'Math', passing:'D', status:'passed',    grade:'B',  prereqs:[], year:1 },
  { id:'HU111', name:'English Language I',             credits:2, type:'Univ. Req.',  avail:'Fall & Spring',dept:'HU',  passing:'D', status:'passed',    grade:'A-', prereqs:[], year:1 },
  // Year 2
  { id:'CS241', name:'Object Oriented Programming',    credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'passed',      grade:'A',  prereqs:['CS141'], year:2 },
  { id:'CS211', name:'Data Structures I',              credits:3, type:'Core',        avail:'Spring',      dept:'CS', passing:'D', status:'passed',      grade:'B',  prereqs:['CS241'], year:2 },
  { id:'MA222', name:'Probability & Statistics',       credits:3, type:'Core',        avail:'Spring',      dept:'Math', passing:'D', status:'passed',    grade:'C+', prereqs:['MA113'], year:2 },
  { id:'EE201', name:'Logic Design',                   credits:3, type:'Core',        avail:'Fall',        dept:'EE',  passing:'D', status:'passed',      grade:'B-', prereqs:[], year:2 },
  // Year 3 (current)
  { id:'CS311', name:'Algorithms Analysis & Design',   credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'in-progress', grade:null, prereqs:['CS211'], year:3 },
  { id:'CS312', name:'Data Structures II',             credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'in-progress', grade:null, prereqs:['CS211'], year:3 },
  { id:'CS321', name:'Operating Systems I',            credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'remaining',   grade:null, prereqs:['CS121'], year:3 },
  { id:'CS391', name:'Software Engineering',           credits:3, type:'Core',        avail:'Fall & Spring',dept:'CS', passing:'D', status:'remaining',  grade:null, prereqs:[], year:3 },
  // Year 4
  { id:'AI301', name:'Artificial Intelligence',        credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'blocked',     grade:null, prereqs:['CS311'], year:4 },
  { id:'AI413', name:'Machine Learning',               credits:3, type:'Core',        avail:'Fall',        dept:'CS', passing:'D', status:'blocked',     grade:null, prereqs:['MA222','CS311'], year:4 },
  { id:'CS401', name:'Natural Language Processing',    credits:3, type:'Elective',    avail:'All',         dept:'CS', passing:'D', status:'blocked',     grade:null, prereqs:['CS311'], year:4 },
  { id:'CS491', name:'Senior Project I',               credits:3, type:'Core',        avail:'Fall & Spring',dept:'CS', passing:'C', status:'blocked',    grade:null, prereqs:['CS391','CS321'], year:4 },
];

const STATUS_STYLE = {
  'passed':      { bg:'#10b981', border:'#059669', text:'#fff',     label:'Passed'      },
  'in-progress': { bg:'#3b82f6', border:'#2563eb', text:'#fff',     label:'In Progress' },
  'remaining':   { bg:'#e2e8f0', border:'#cbd5e1', text:'#475569',  label:'Remaining'   },
  'blocked':     { bg:'#475569', border:'#334155', text:'#94a3b8',  label:'Blocked'     },
  'failed':      { bg:'#ef4444', border:'#dc2626', text:'#fff',     label:'Failed'      },
  'cascade':     { bg:'#ef4444', border:'#b91c1c', text:'#fff',     label:'Blocked'     },
};

const elements = [];

const yearX = { 1:120, 2:380, 3:640, 4:900 };

const perYear = {};
COURSES.forEach(c => { perYear[c.year] = (perYear[c.year]||0) + 1; });
const yearIdx = {};
COURSES.forEach(c => {
  if (!yearIdx[c.year]) yearIdx[c.year] = 0;
  const i = yearIdx[c.year]++;
  const total = perYear[c.year];
  const s = STATUS_STYLE[c.status];
  elements.push({
    data: {
      id: c.id, label: c.id + '\n' + c.name,
      ...c,
      bgColor: s.bg, borderColor: s.border, textColor: s.text,
      originalStatus: c.status,
    },
    position: { x: yearX[c.year], y: -((total-1)*60) + i*120 },
    classes: c.status,
  });
});

COURSES.forEach(c => {
  c.prereqs.forEach(p => {
    elements.push({ data:{ id:`${p}->${c.id}`, source:p, target:c.id } });
  });
});

  const cyContainer = document.getElementById('cy');
  if (!cyContainer || typeof cytoscape === 'undefined') {
    return;
  }

/* Init Cytoscape */
const cy = cytoscape({
  container: cyContainer,
  elements,
  style: [
    {
      selector: 'node',
      style: {
        'label':              'data(label)',
        'text-wrap':          'wrap',
        'text-max-width':     '138px',
        'font-size':          '11px',
        'font-family':        'Outfit, system-ui, sans-serif',
        'font-weight':        '700',
        'text-halign':        'center',
        'text-valign':        'center',
        'width':              168,
        'height':             58,
        'shape':              'roundrectangle',
        'background-color':   'data(bgColor)',
        'color':              'data(textColor)',
        'border-width':       2,
        'border-color':       'data(borderColor)',
        'shadow-blur':        12,
        'shadow-color':       'rgba(0,0,0,0.10)',
        'shadow-offset-x':    0,
        'shadow-offset-y':    4,
        'shadow-opacity':     1,
        'transition-property': 'background-color, border-color, opacity, width, height',
        'transition-duration': '280ms',
      },
    },
    {
      selector: 'node.selected-node',
      style: { 
        'border-width':4, 'border-color':'#4F46E5', 'width':178, 'height':64,
        'shadow-blur': 20, 'shadow-color': 'rgba(79,70,229,0.3)',
        'shadow-offset-x': 0, 'shadow-offset-y': 4, 'shadow-opacity': 1,
      },
    },
    {
      selector: 'node.dimmed',
      style: { 'opacity': 0.18 },
    },
    {
      selector: 'node.cascade-source',
      style: {
        'background-color': '#dc2626',
        'border-color':     '#991b1b',
        'border-width':     4,
        'color':            '#fff',
        'width':            172,
        'height':           62,
      },
    },
    {
      selector: 'node.cascade-hit',
      style: {
        'background-color': '#ef4444',
        'border-color':     '#b91c1c',
        'color':            '#fff',
      },
    },
    {
      selector: 'edge',
      style: {
        'width':                 2,
        'line-color':            '#D1D9E6',
        'target-arrow-color':    '#D1D9E6',
        'target-arrow-shape':    'triangle',
        'arrow-scale':           0.85,
        'curve-style':           'bezier',
        'transition-property':   'line-color, target-arrow-color, opacity, width',
        'transition-duration':   '280ms',
      },
    },
    {
      selector: 'edge.dimmed',
      style: { 'opacity': 0.06 },
    },
    {
      selector: 'edge.cascade-edge',
      style: {
        'line-color':         '#ef4444',
        'target-arrow-color': '#ef4444',
        'width':              3,
      },
    },
  ],
  layout:            { name:'preset' },
  minZoom:           0.35,
  maxZoom:           2.5,
  userZoomingEnabled:true,
  userPanningEnabled:true,
  boxSelectionEnabled:false,
});

cy.fit(undefined, 60);

let selectedNode   = null;
let simActive      = false;
let simSourceId    = null;

cy.on('tap', 'node', e => {
  const n = e.target;
  if (selectedNode) selectedNode.removeClass('selected-node');
  selectedNode = n;
  n.addClass('selected-node');
  openPanel(n.data());
});

cy.on('tap', e => {
  if (e.target === cy) closePanel();
});

cy.on('mouseover', 'node', () => document.getElementById('cy').style.cursor = 'pointer');
cy.on('mouseout',  'node', () => document.getElementById('cy').style.cursor = 'default');

function openPanel(d) {
  const panel  = document.getElementById('node-panel');
  const st     = d.originalStatus || d.status;
  const style  = STATUS_STYLE[st] || STATUS_STYLE['remaining'];

  const pill = document.getElementById('panel-status-pill');
  pill.textContent = style.label;
  pill.className   = 'cm-status-pill cm-status-' + st;

  document.getElementById('panel-code').textContent    = d.id;
  document.getElementById('panel-name').textContent    = d.name;
  document.getElementById('panel-credits').textContent = d.credits + ' credit hours';

  const iconMap  = { 'passed':'check_circle','failed':'cancel','in-progress':'autorenew','remaining':'radio_button_unchecked','blocked':'lock','cascade':'bolt' };
  const colorMap = { 'passed':'#10b981','failed':'#ef4444','in-progress':'#3b82f6','remaining':'var(--c-muted)','blocked':'var(--c-muted)','cascade':'#ef4444' };
  const icon     = document.getElementById('panel-status-icon');
  icon.textContent  = iconMap[st] || 'help';
  icon.style.color  = colorMap[st] || 'var(--c-muted)';
  document.getElementById('panel-status-text').textContent = style.label;
  document.getElementById('panel-grade-text').textContent  = d.grade ? 'Grade: ' + d.grade : (st === 'in-progress' ? 'Awaiting final grade' : '');

  document.getElementById('panel-type').textContent  = d.type;
  document.getElementById('panel-avail').textContent = d.avail;
  document.getElementById('panel-pass').textContent  = d.passing;
  document.getElementById('panel-dept').textContent  = d.dept;

  const preEl = document.getElementById('panel-prereqs');
  preEl.innerHTML = '';
  if (d.prereqs && d.prereqs.length) {
    d.prereqs.forEach(pid => {
      const pc = COURSES.find(c => c.id === pid);
      const passed = pc && pc.status === 'passed';
      const row = document.createElement('div');
      row.className = 'cm-prereq-row ' + (passed ? 'cm-prereq-passed' : 'cm-prereq-pending');
      row.innerHTML = `<span class="material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 1,'wght' 400">${passed?'check_circle':'radio_button_unchecked'}</span>
        <span class="flex-fill" style="font-size:12px;">${pid}: ${pc?pc.name:''}</span>`;
      row.addEventListener('click', () => {
        const n = cy.getElementById(pid);
        if (n.length) { cy.animate({ center:{ eles:n }, duration:300 }); n.emit('tap'); }
      });
      preEl.appendChild(row);
    });
  } else {
    preEl.innerHTML = '<span style="font-size:12px;color:var(--c-muted);font-style:italic;">No prerequisites</span>';
  }

  const unlockEl = document.getElementById('panel-unlocks');
  unlockEl.innerHTML = '';
  const dependents = COURSES.filter(c => c.prereqs.includes(d.id));
  if (dependents.length) {
    dependents.forEach(dep => {
      const chip = document.createElement('span');
      chip.className = 'cm-unlock-chip';
      chip.textContent = dep.id;
      chip.addEventListener('click', () => {
        const n = cy.getElementById(dep.id);
        if (n.length) { cy.animate({ center:{ eles:n }, duration:300 }); n.emit('tap'); }
      });
      unlockEl.appendChild(chip);
    });
  } else {
    unlockEl.innerHTML = '<span style="font-size:12px;color:var(--c-muted);font-style:italic;">Terminal course</span>';
  }

  const canSim = (st === 'passed' || st === 'in-progress') && !simActive;
  document.getElementById('panel-simulate-wrap').style.display = canSim ? '' : 'none';
  document.getElementById('panel-clear-wrap').style.display    = simActive ? '' : 'none';

  panel.classList.add('open');
}

function closePanel() {
  document.getElementById('node-panel').classList.remove('open');
  if (selectedNode) { selectedNode.removeClass('selected-node'); selectedNode = null; }
}

document.getElementById('btn-close-panel').addEventListener('click', closePanel);

/* Zoom / Fit */
document.getElementById('btn-zoom-in').addEventListener('click', () =>
  cy.zoom({ level: cy.zoom() * 1.3, renderedPosition:{ x:cy.width()/2, y:cy.height()/2 } }));
document.getElementById('btn-zoom-out').addEventListener('click', () =>
  cy.zoom({ level: cy.zoom() / 1.3, renderedPosition:{ x:cy.width()/2, y:cy.height()/2 } }));
document.getElementById('btn-fit').addEventListener('click', () =>
  cy.animate({ fit:{ padding:60 }, duration:400, easing:'ease-out-cubic' }));

/* Impact Simulation */
document.getElementById('btn-simulate').addEventListener('click', startSim);
document.getElementById('btn-clear-banner').addEventListener('click', clearSim);
document.getElementById('btn-clear-panel').addEventListener('click', clearSim);

function startSim() {
  if (!selectedNode) return;
  simActive   = true;
  simSourceId = selectedNode.data().id;
  const src   = COURSES.find(c => c.id === simSourceId);

  /* BFS */
  const blocked = [];
  const queue   = [simSourceId];
  const visited = new Set([simSourceId]);
  while (queue.length) {
    const cur = queue.shift();
    COURSES.filter(c => c.prereqs.includes(cur)).forEach(dep => {
      if (!visited.has(dep.id)) {
        visited.add(dep.id);
        blocked.push(dep);
        queue.push(dep.id);
      }
    });
  }

  cy.nodes().addClass('dimmed');
  cy.edges().addClass('dimmed');

  cy.getElementById(simSourceId).removeClass('dimmed').addClass('cascade-source');

  blocked.forEach((dep, i) => {
    setTimeout(() => {
      const n = cy.getElementById(dep.id);
      n.removeClass('dimmed').addClass('cascade-hit');
      n.incomers('edge').forEach(edge => {
        if (visited.has(edge.source().id())) {
          edge.removeClass('dimmed').addClass('cascade-edge');
        }
      });
    }, (i + 1) * 220);
  });

  COURSES.filter(c => c.status === 'passed' || c.status === 'in-progress').forEach(c => {
    if (c.id !== simSourceId) {
      cy.getElementById(c.id).removeClass('dimmed').style('opacity', 0.45);
    }
  });

  document.getElementById('sim-course-label').textContent = simSourceId + ' ' + src.name;
  document.getElementById('sim-banner').classList.add('show');

  document.getElementById('panel-simulate-wrap').style.display = 'none';
  document.getElementById('panel-clear-wrap').style.display    = '';

  document.getElementById('btn-impact-toggle').style.display = '';

  setTimeout(() => openImpactDrawer(blocked, src), blocked.length * 220 + 350);
}

function clearSim() {
  simActive   = false;
  simSourceId = null;

  cy.nodes().removeClass('dimmed cascade-hit cascade-source');
  cy.edges().removeClass('dimmed cascade-edge');
  cy.nodes().style('opacity', 1);

  document.getElementById('sim-banner').classList.remove('show');
  closeImpactDrawer();
  document.getElementById('btn-impact-toggle').style.display = 'none';

  document.getElementById('panel-simulate-wrap').style.display = '';
  document.getElementById('panel-clear-wrap').style.display    = 'none';

  if (selectedNode) openPanel(selectedNode.data());
}

/* ── Impact Drawer ── */
function openImpactDrawer(blocked, src) {
  const delay = blocked.length > 3 ? 2 : 1;
  const panel = document.getElementById('node-panel');
  panel.classList.add('open', 'cm-panel-impact');

  panel.innerHTML = `
    <div class="cm-impact-header">
      <div class="d-flex align-items-center gap-2">
        <span class="material-symbols-outlined" style="font-size:19px;color:#ef4444;font-variation-settings:'FILL' 1,'wght' 400">bolt</span>
        <h3 class="fw-800 mb-0" style="font-size:.95rem;color:var(--c-text);">Simulation Result</h3>
      </div>
      <button id="btn-close-impact" class="cm-panel-close">
        <span class="material-symbols-outlined" style="font-size:20px;font-variation-settings:'FILL' 0,'wght' 300">close</span>
      </button>
    </div>

    <!-- Damage headline -->
    <div class="cm-damage-headline">
      <div class="cm-damage-number">${blocked.length}</div>
      <div>
        <p class="fw-700 mb-0" style="font-size:14px;color:var(--c-text);">Courses Blocked</p>
        <p style="font-size:11px;color:var(--c-muted);margin:0;">by failing <strong>${src.id}</strong></p>
      </div>
      <span class="cm-damage-badge">${blocked.length >= 4 ? 'CRITICAL' : blocked.length >= 2 ? 'HIGH' : 'LOW'}</span>
    </div>

    <!-- Quick metrics row -->
    <div class="cm-metrics-row">
      <div class="cm-impact-metric">
        <p class="cm-impact-metric-val" style="color:var(--c-primary);">${Math.min(blocked.length + 1, 3)}</p>
        <p class="cm-impact-metric-label">Semesters</p>
      </div>
      <div class="cm-impact-metric">
        <p class="cm-impact-metric-val" style="color:#ef4444;">+${delay} sem</p>
        <p class="cm-impact-metric-label">Grad Delay</p>
      </div>
    </div>

    <!-- Blocked list (compact) -->
    <div class="cm-panel-section">
      <p class="cm-panel-section-title">Blocked Courses</p>
      <div class="d-flex flex-column gap-1">
        ${blocked.map((b,i) => `
          <div class="cm-blocked-row" style="animation-delay:${i*60}ms">
            <span class="material-symbols-outlined" style="font-size:15px;color:#ef4444;font-variation-settings:'FILL' 1,'wght' 400">error</span>
            <div class="flex-fill">
              <p class="fw-700 mb-0" style="font-size:12px;color:var(--c-text);">${b.id} — ${b.name}</p>
              <p style="font-size:10.5px;color:var(--c-muted);margin:0;">${b.prereqs.includes(src.id)?'Direct':'Chain'} dependency</p>
            </div>
          </div>
        `).join('')}
      </div>
    </div>

    <!-- Action buttons -->
    <div class="cm-panel-action d-flex flex-column gap-2">
      <a href="impact-analyzer.html" class="cm-btn-view-report text-decoration-none d-flex align-items-center justify-content-center gap-2">
        <span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">open_in_new</span>
        View Full Impact Report
      </a>
      <button id="btn-clear-impact" class="cm-btn-clear w-100">
        <span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">refresh</span>
        Clear Simulation
      </button>
    </div>
    <div class="cm-panel-ask-ai">
      <a href="ai-advisor.html" class="d-flex align-items-center gap-1" style="font-size:12px;color:var(--c-primary);text-decoration:none;font-weight:600;">
        <span class="material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 0,'wght' 300">auto_awesome</span>
        Ask AI Advisor for recovery plan
      </a>
    </div>
  `;

  document.getElementById('btn-close-impact').addEventListener('click', () => { closePanel(); clearSim(); });
  document.getElementById('btn-clear-impact').addEventListener('click', clearSim);
}

function closeImpactDrawer() {
  const panel = document.getElementById('node-panel');
  panel.classList.remove('cm-panel-impact');
  panel.innerHTML = `
    <div class="cm-panel-header">
      <span id="panel-status-pill" class="cm-status-pill"></span>
      <button id="btn-close-panel" class="cm-panel-close">
        <span class="material-symbols-outlined" style="font-size:20px;font-variation-settings:'FILL' 0,'wght' 300">close</span>
      </button>
    </div>
    <div class="cm-panel-identity">
      <h2 id="panel-code"    class="fw-900" style="font-size:1.3rem;letter-spacing:-.3px;color:var(--c-text);margin:0;"></h2>
      <p  id="panel-name"    style="font-size:13px;color:var(--c-text-sub);margin:3px 0 0;"></p>
      <p  id="panel-credits" style="font-size:11px;color:var(--c-muted);margin:2px 0 0;"></p>
    </div>
    <div id="panel-status-box" class="cm-panel-status-box">
      <span class="material-symbols-outlined cm-panel-status-icon" id="panel-status-icon" style="font-size:18px;"></span>
      <div>
        <p id="panel-status-text" class="fw-700 mb-0" style="font-size:13px;"></p>
        <p id="panel-grade-text"  style="font-size:11px;color:var(--c-muted);margin:0;"></p>
      </div>
    </div>
    <div class="cm-panel-grid">
      <div class="cm-panel-meta-item"><span class="cm-panel-meta-label">Type</span><span id="panel-type"  class="cm-panel-meta-value"></span></div>
      <div class="cm-panel-meta-item"><span class="cm-panel-meta-label">Availability</span><span id="panel-avail" class="cm-panel-meta-value"></span></div>
      <div class="cm-panel-meta-item"><span class="cm-panel-meta-label">Pass Grade</span><span id="panel-pass"  class="cm-panel-meta-value"></span></div>
      <div class="cm-panel-meta-item"><span class="cm-panel-meta-label">Department</span><span id="panel-dept"  class="cm-panel-meta-value"></span></div>
    </div>
    <div class="cm-panel-section"><p class="cm-panel-section-title">Prerequisites</p><div id="panel-prereqs"></div></div>
    <div class="cm-panel-section"><p class="cm-panel-section-title">Unlocks</p><div id="panel-unlocks" class="d-flex flex-wrap gap-1"></div></div>
    <div id="panel-simulate-wrap" class="cm-panel-action">
      <button id="btn-simulate" class="cm-btn-simulate">
        <span class="material-symbols-outlined" style="font-size:17px;font-variation-settings:'FILL' 1,'wght' 400">bolt</span>
        Simulate Failure
      </button>
    </div>
    <div id="panel-clear-wrap" class="cm-panel-action" style="display:none;">
      <button id="btn-clear-panel" class="cm-btn-clear">
        <span class="material-symbols-outlined" style="font-size:17px;font-variation-settings:'FILL' 0,'wght' 300">refresh</span>
        Clear Simulation
      </button>
    </div>
    <div class="cm-panel-ask-ai">
      <a href="ai-advisor.html" class="d-flex align-items-center gap-1" style="font-size:12px;color:var(--c-primary);text-decoration:none;font-weight:600;">
        <span class="material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 0,'wght' 300">auto_awesome</span>
        Ask AI Advisor about this course
      </a>
    </div>
  `;

  document.getElementById('btn-close-panel').addEventListener('click', closePanel);
  document.getElementById('btn-simulate').addEventListener('click', startSim);
  document.getElementById('btn-clear-panel').addEventListener('click', clearSim);
}

})();