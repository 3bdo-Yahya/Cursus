(function () {

const API_URL = '/api/CourseMap/get-all';

const STATUS_BY_CODE = { 0:'passed', 1:'failed', 2:'in-progress' };

const SEMESTER_MIN = 1;
const SEMESTER_MAX = 8;
const FLEXIBLE_SEMESTER = 9;
const COLUMN_WIDTH = 360;
const ROW_HEIGHT = 92;
const DENSE_LANE_WIDTH = 178;
const HEADER_MODEL_Y = -92;
const SEMESTER_LABELS = {
  1: 'Sem 1',
  2: 'Sem 2',
  3: 'Sem 3',
  4: 'Sem 4',
  5: 'Sem 5',
  6: 'Sem 6',
  7: 'Sem 7',
  8: 'Sem 8',
  9: 'Flexible'
};

const STATUS_STYLE = {
  'passed':      { bg:'#10b981', border:'#059669', text:'#fff',     label:'Passed'      },
  'in-progress': { bg:'#3b82f6', border:'#2563eb', text:'#fff',     label:'In Progress' },
  'planned':     { bg:'#dbeafe', border:'#60a5fa', text:'#1e40af',  label:'Planned'     },
  'remaining':   { bg:'#e2e8f0', border:'#cbd5e1', text:'#475569',  label:'Remaining'   },
  'blocked':     { bg:'#475569', border:'#334155', text:'#94a3b8',  label:'Blocked'     },
  'failed':      { bg:'#ef4444', border:'#dc2626', text:'#fff',     label:'Failed'      },
  'cascade':     { bg:'#ef4444', border:'#b91c1c', text:'#fff',     label:'Blocked'     },
};

const cyContainer = document.getElementById('cy');
if (!cyContainer || typeof cytoscape === 'undefined') {
  return;
}

let COURSES = [];
let cy = null;
let selectedNode   = null;
let simActive      = false;
let simSourceId    = null;
let simTimeouts    = [];

const IMPACT_STORAGE_KEY = 'cursusImpactReport';


function buildCourses(nodes, edges) {
  const prereqsById = {};
  edges.forEach(e => {
    const tId = String(e.targetCourseId);
    (prereqsById[tId] = prereqsById[tId] || []).push(String(e.sourceCourseId));
  });

  const knownStatus = {};
  nodes.forEach(n => {
    const id = String(n.id);
    if (n.isPlanned && (n.status === null || n.status === undefined)) {
      knownStatus[id] = 'planned';
      return;
    }
    knownStatus[id] = (n.status === null || n.status === undefined) ? null : STATUS_BY_CODE[n.status];
  });

  return nodes.map(n => {
    const id = String(n.id);
    const prereqs = prereqsById[id] || [];
    let status = knownStatus[id];
    if (!status) {
      const prereqsMet = prereqs.every(p => knownStatus[p] === 'passed');
      status = prereqsMet ? 'remaining' : 'blocked';
    }
    return {
      id,
      code: n.code,
      name: n.name,
      credits: n.creditHours,
      status,
      grade: n.grade || null,
      prereqs,
      courseType: n.courseType,
      recommendedSemester: normalizeSemester(n.recommendedSemester),
    };
  });
}

function buildElements(courses) {
  const elements = [];
  const courseIdSet = new Set(courses.map(c => c.id));

  courses.forEach(c => {
    const s = STATUS_STYLE[c.status] || STATUS_STYLE['remaining'];
    elements.push({
      data: {
        id: c.id, label: c.code + '\n' + c.name,
        ...c,
        bgColor: s.bg, borderColor: s.border, textColor: s.text,
        originalStatus: c.status,
      },
      classes: c.status,
    });
  });
  courses.forEach(c => {
    c.prereqs.forEach(p => {
      if (courseIdSet.has(p)) {
        elements.push({ data:{ id:`${p}->${c.id}`, source:p, target:c.id } });
      }
    });
  });
  return elements;
}

function normalizeSemester(value) {
  const semester = Number(value);
  return Number.isInteger(semester) && semester >= SEMESTER_MIN && semester <= SEMESTER_MAX
    ? semester
    : null;
}

function resolveCourseSemesters(courses) {
  const courseById = new Map(courses.map(course => [course.id, course]));
  const dependentsById = new Map();
  courses.forEach(course => {
    course.prereqs.forEach(prereqId => {
      const dependents = dependentsById.get(prereqId) || [];
      dependents.push(course.id);
      dependentsById.set(prereqId, dependents);
    });
  });

  const resolved = new Map();
  const resolving = new Set();

  function resolve(course) {
    if (resolved.has(course.id)) return resolved.get(course.id);

    const knownSemester = normalizeSemester(course.recommendedSemester);
    if (knownSemester) {
      resolved.set(course.id, knownSemester);
      return knownSemester;
    }

    if (resolving.has(course.id)) {
      return FLEXIBLE_SEMESTER;
    }

    resolving.add(course.id);
    const prereqSemesters = course.prereqs
      .map(prereqId => courseById.get(prereqId))
      .filter(Boolean)
      .map(prereq => resolve(prereq))
      .filter(semester => semester >= SEMESTER_MIN && semester <= SEMESTER_MAX);
    resolving.delete(course.id);

    let semester = FLEXIBLE_SEMESTER;
    if (prereqSemesters.length > 0) {
      const nextSemester = Math.max(...prereqSemesters) + 1;
      semester = nextSemester <= SEMESTER_MAX ? nextSemester : FLEXIBLE_SEMESTER;
    } else if (course.prereqs.length === 0 && (dependentsById.get(course.id) || []).length > 0) {
      semester = SEMESTER_MIN;
    }

    resolved.set(course.id, semester);
    return semester;
  }

  courses.forEach(resolve);
  return resolved;
}

function compareCoursesForLayout(a, b) {
  return (a.courseType ?? 99) - (b.courseType ?? 99) || a.code.localeCompare(b.code);
}

function computeLaneCount(courseCount) {
  if (courseCount > 14) return 3;
  if (courseCount > 7) return 2;
  return 1;
}

function computeSemesterPositions(courses) {
  const resolvedSemesters = resolveCourseSemesters(courses);
  const groups = new Map();
  const positionMap = {};

  courses.forEach(course => {
    const semester = resolvedSemesters.get(course.id) || FLEXIBLE_SEMESTER;
    const group = groups.get(semester) || [];
    group.push(course);
    groups.set(semester, group);
  });

  for (let semester = SEMESTER_MIN; semester <= FLEXIBLE_SEMESTER; semester++) {
    const group = (groups.get(semester) || []).sort(compareCoursesForLayout);
    const laneCount = computeLaneCount(group.length);
    const rows = Math.max(1, Math.ceil(group.length / laneCount));
    const columnX = getSemesterModelX(semester);

    group.forEach((course, index) => {
      const lane = index % laneCount;
      const row = Math.floor(index / laneCount);
      const laneOffset = (lane - (laneCount - 1) / 2) * DENSE_LANE_WIDTH;

      positionMap[course.id] = {
        x: columnX + laneOffset,
        y: (row - (rows - 1) / 2) * ROW_HEIGHT
      };
    });
  }

  return positionMap;
}

function getSemesterModelX(semester) {
  return (semester - SEMESTER_MIN) * COLUMN_WIDTH;
}

function buildPresetLayout(positionMap, animate) {
  return {
    name: 'preset',
    positions: node => positionMap[node.id()] || { x: 0, y: 0 },
    animate,
    animationDuration: animate ? 400 : 0,
    animationEasing: 'ease-out-cubic'
  };
}

function renderSemesterHeaders() {
  const headerContainer = document.getElementById('cm-semester-headers');
  if (!headerContainer) return;

  headerContainer.innerHTML = '';
  for (let semester = SEMESTER_MIN; semester <= FLEXIBLE_SEMESTER; semester++) {
    const label = document.createElement('div');
    label.className = 'cm-semester-header';
    label.dataset.semester = String(semester);
    label.textContent = SEMESTER_LABELS[semester];
    headerContainer.appendChild(label);
  }
}

function updateSemesterHeaders() {
  if (!cy) return;

  const headerContainer = document.getElementById('cm-semester-headers');
  if (!headerContainer) return;

  const pan = cy.pan();
  const zoom = cy.zoom();
  headerContainer.querySelectorAll('.cm-semester-header').forEach(label => {
    const semester = Number(label.dataset.semester);
    const renderedX = pan.x + getSemesterModelX(semester) * zoom;
    const renderedY = pan.y + HEADER_MODEL_Y * zoom;
    label.style.transform = `translate(${renderedX}px, ${renderedY}px) translateX(-50%)`;
  });
}

function wireSemesterHeaders() {
  renderSemesterHeaders();
  updateSemesterHeaders();
  cy.on('pan zoom layoutstop render', updateSemesterHeaders);
  window.addEventListener('resize', updateSemesterHeaders);
}

function showMessage(message) {
  cyContainer.innerHTML = `<div style="display:flex;align-items:center;justify-content:center;height:100%;padding:24px;text-align:center;font-size:13px;color:var(--c-muted);">${message}</div>`;
}

init();

async function init() {
  let graph;
  try {
    const res = await fetch(API_URL, { headers: { 'Accept': 'application/json' } });
    if (!res.ok) {
      let message = 'Unable to load your course map. Please try again later.';
      const body = await res.json().catch(() => null);
      if (body && body.error) message = body.error;
      showMessage(message);
      return;
    }
    graph = await res.json();
  } catch (err) {
    showMessage('Unable to load your course map. Please try again later.');
    return;
  }

  const nodes = graph.nodes || [];
  if (!nodes.length) {
    showMessage('No courses are available for your department yet.');
    return;
  }

  COURSES = buildCourses(nodes, graph.edges || []);
  const elements = buildElements(COURSES);
  const positionMap = computeSemesterPositions(COURSES);

/* Init Cytoscape */
cy = cytoscape({
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
      selector: 'node.planned',
      style: {
        'border-style': 'dashed',
        'border-width': 3,
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
      selector: 'node.cascade-hit-direct',
      style: {
        'background-color': '#ef4444',
        'border-color':     '#b91c1c',
        'color':            '#fff',
      },
    },
    {
      selector: 'node.cascade-hit-chain',
      style: {
        'background-color': '#f59e0b',
        'border-color':     '#b45309',
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
    {
      selector: 'edge.cascade-edge-chain',
      style: {
        'line-color':         '#f59e0b',
        'target-arrow-color': '#f59e0b',
        'width':              3,
      },
    },
  ],
  layout: buildPresetLayout(positionMap, false),
  minZoom: 0.35,
  maxZoom: 2.5,
  userZoomingEnabled:true,
  userPanningEnabled:true,
  boxSelectionEnabled:false,
});

cy.fit(undefined, 60);
wireSemesterHeaders();

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

  wireStaticControls();
  wireFilterControls();
}

function openPanel(d) {
  const panel  = document.getElementById('node-panel');
  const st     = d.originalStatus || d.status;
  const style  = STATUS_STYLE[st] || STATUS_STYLE['remaining'];

  const pill = document.getElementById('panel-status-pill');
  pill.textContent = style.label;
  pill.className   = 'cm-status-pill cm-status-' + st;

  document.getElementById('panel-code').textContent    = d.code;
  document.getElementById('panel-name').textContent    = d.name;
  document.getElementById('panel-credits').textContent = d.credits + ' credit hours';

  const iconMap  = { 'passed':'check_circle','failed':'cancel','in-progress':'autorenew','planned':'event_note','remaining':'radio_button_unchecked','blocked':'lock','cascade':'bolt' };
  const colorMap = { 'passed':'#10b981','failed':'#ef4444','in-progress':'#3b82f6','planned':'#60a5fa','remaining':'var(--c-muted)','blocked':'var(--c-muted)','cascade':'#ef4444' };
  const icon     = document.getElementById('panel-status-icon');
  icon.textContent  = iconMap[st] || 'help';
  icon.style.color  = colorMap[st] || 'var(--c-muted)';
  document.getElementById('panel-status-text').textContent = style.label;
  document.getElementById('panel-grade-text').textContent  = d.grade ? 'Grade: ' + d.grade : (st === 'in-progress' ? 'Awaiting final grade' : (st === 'planned' ? 'Planned for primary term' : ''));

  document.getElementById('panel-type').textContent  = d.type || '—';
  document.getElementById('panel-avail').textContent = d.avail || '—';
  document.getElementById('panel-pass').textContent  = d.passing || '—';
  document.getElementById('panel-dept').textContent  = d.dept || '—';

  const preEl = document.getElementById('panel-prereqs');
  preEl.innerHTML = '';
  if (d.prereqs && d.prereqs.length) {
    d.prereqs.forEach(pid => {
      const pc = COURSES.find(c => c.id === pid);
      const passed = pc && pc.status === 'passed';
      const row = document.createElement('div');
      row.className = 'cm-prereq-row ' + (passed ? 'cm-prereq-passed' : 'cm-prereq-pending');
      row.innerHTML = `<span class="material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 1,'wght' 400">${passed?'check_circle':'radio_button_unchecked'}</span>
        <span class="flex-fill" style="font-size:12px;">${pc ? pc.code : pid}: ${pc?pc.name:''}</span>`;
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
      chip.textContent = dep.code;
      chip.addEventListener('click', () => {
        const n = cy.getElementById(dep.id);
        if (n.length) { cy.animate({ center:{ eles:n }, duration:300 }); n.emit('tap'); }
      });
      unlockEl.appendChild(chip);
    });
  } else {
    unlockEl.innerHTML = '<span style="font-size:12px;color:var(--c-muted);font-style:italic;">Terminal course</span>';
  } 

  const canSim = (st === 'passed' || st === 'in-progress' || st === 'planned') && !simActive;
  document.getElementById('panel-simulate-wrap').style.display = canSim ? '' : 'none';
  document.getElementById('panel-clear-wrap').style.display    = simActive ? '' : 'none';

  panel.classList.add('open');
}

function closePanel() {
  document.getElementById('node-panel').classList.remove('open');
  if (selectedNode) { selectedNode.removeClass('selected-node'); selectedNode = null; }
}

function wireStaticControls() {
  document.getElementById('btn-close-panel').addEventListener('click', closePanel);

  document.getElementById('btn-zoom-in').addEventListener('click', () =>
    cy.zoom({ level: cy.zoom() * 1.3, renderedPosition:{ x:cy.width()/2, y:cy.height()/2 } }));
  document.getElementById('btn-zoom-out').addEventListener('click', () =>
    cy.zoom({ level: cy.zoom() / 1.3, renderedPosition:{ x:cy.width()/2, y:cy.height()/2 } }));
  document.getElementById('btn-fit').addEventListener('click', () =>
    cy.animate({ fit:{ padding:60 }, duration:400, easing:'ease-out-cubic' }));

  document.getElementById('btn-simulate').addEventListener('click', startSim);
  document.getElementById('btn-clear-banner').addEventListener('click', clearSimAnimated);
  document.getElementById('btn-clear-panel').addEventListener('click', clearSimAnimated);
}

function pulseNode(node) {
  node.animate(
    { style: { 'width': 184, 'height': 66 } },
    {
      duration: 260,
      easing: 'ease-out-sine',
      complete: () => node.removeStyle('width height'),
    }
  );
}

function pulseEdge(edge) {
  edge.animate(
    { style: { 'width': 5, 'line-color': '#dc2626', 'target-arrow-color': '#dc2626' } },
    {
      duration: 260,
      easing: 'ease-out-sine',
      complete: () => edge.removeStyle('width line-color target-arrow-color'),
    }
  );
}

async function fetchImpactResult(courseId) {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
  const res = await fetch('/Student/SimulateFailure', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'RequestVerificationToken': token
    },
    body: JSON.stringify({ courseId }),
  });
  if (res.status === 404) throw new Error('Selected course was not found in your curriculum.');
  if (!res.ok) throw new Error('Unable to run the simulation right now. Please try again.');
  return res.json();
}

function showSimError(message) {
  const panel = document.getElementById('node-panel');
  panel.classList.add('open');
  panel.innerHTML = `
    <div class="cm-panel-header">
      <span class="cm-status-pill" style="background:#fee2e2;color:#b91c1c;">Error</span>
      <button id="btn-close-sim-error" class="cm-panel-close">
        <span class="material-symbols-outlined" style="font-size:20px;font-variation-settings:'FILL' 0,'wght' 300">close</span>
      </button>
    </div>
    <div style="padding:16px;font-size:13px;color:var(--c-text-sub);">${message}</div>
  `;
  document.getElementById('btn-close-sim-error').addEventListener('click', () => {
    if (selectedNode) openPanel(selectedNode.data());
    else closePanel();
  });
}

async function startSim() {
  if (!selectedNode || simActive) return;
  const sourceId = selectedNode.data().id;
  const btn = document.getElementById('btn-simulate');
  if (btn) btn.disabled = true;

  let result;
  try {
    result = await fetchImpactResult(parseInt(sourceId, 10));
  } catch (err) {
    if (btn) btn.disabled = false;
    showSimError(err.message);
    return;
  }
  if (btn) btn.disabled = false;

  simActive   = true;
  simSourceId = sourceId;
  sessionStorage.setItem(IMPACT_STORAGE_KEY, JSON.stringify(result));

  const blocked    = result.blockedCourses || [];
  const coveredIds = new Set([simSourceId, ...blocked.map(b => String(b.courseId))]);

  cy.nodes().addClass('dimmed');
  cy.edges().addClass('dimmed');
  cy.getElementById(simSourceId).removeClass('dimmed').addClass('cascade-source');

  blocked.forEach(b => {
    const tId = setTimeout(() => {
      const n = cy.getElementById(String(b.courseId));
      if (!n.length) return;
      const hitClass = b.depth === 1 ? 'cascade-hit cascade-hit-direct' : 'cascade-hit cascade-hit-chain';
      const edgeClass = b.depth === 1 ? 'cascade-edge' : 'cascade-edge cascade-edge-chain';
      n.removeClass('dimmed').addClass(hitClass);
      pulseNode(n);
      n.incomers('edge').forEach(edge => {
        if (coveredIds.has(edge.source().id())) {
          edge.removeClass('dimmed').addClass(edgeClass);
          pulseEdge(edge);
        }
      });
    }, b.depth * 220);
    simTimeouts.push(tId);
  });

  COURSES.filter(c => c.status === 'passed' || c.status === 'in-progress' || c.status === 'planned').forEach(c => {
    if (c.id !== simSourceId) {
      cy.getElementById(c.id).removeClass('dimmed').style('opacity', 0.45);
    }
  });

  document.getElementById('sim-course-label').textContent = result.failedCourseCode + ' ' + result.failedCourseName;
  document.getElementById('sim-banner').classList.add('show');

  document.getElementById('panel-simulate-wrap').style.display = 'none';
  document.getElementById('panel-clear-wrap').style.display    = '';
  document.getElementById('btn-impact-toggle').style.display   = '';

  const maxDepth  = blocked.length ? Math.max(...blocked.map(b => b.depth)) : 0;
  const drawerTId = setTimeout(() => openImpactDrawer(result), maxDepth * 220 + 350);
  simTimeouts.push(drawerTId);
}

function formatSeverity(severity) {
  return (severity || 'Low').toUpperCase();
}

function clearSimAnimated() {
  simTimeouts.forEach(id => clearTimeout(id));
  simTimeouts = [];

  cy.nodes().stop(true);
  cy.edges().stop(true);

  // Reverse the blocked order so last-hit clears first
  const cascadeNodes = cy.nodes('.cascade-hit').toArray().reverse();
  const cascadeEdges = cy.edges('.cascade-edge').toArray().reverse();

  cascadeNodes.forEach((n, i) => {
    const tId = setTimeout(() => {
      n.animate(
        { style: { opacity: 0.15 } },
        {
          duration: 180,
          complete: () => {
            n.removeClass('cascade-hit cascade-hit-direct cascade-hit-chain dimmed');
            n.removeStyle('opacity width height');
          }
        }
      );
    }, i * 120);
    simTimeouts.push(tId);
  });

  cascadeEdges.forEach((e, i) => {
    const tId = setTimeout(() => {
      e.removeClass('cascade-edge cascade-edge-chain dimmed');
      e.removeStyle('width line-color target-arrow-color');
    }, i * 120);
    simTimeouts.push(tId);
  });

  const totalDuration = cascadeNodes.length * 120 + 250;

  const finalTId = setTimeout(() => {
    simTimeouts = [];
    cy.nodes().stop(true).removeStyle('width height opacity');
    cy.nodes().removeClass('dimmed cascade-hit cascade-hit-direct cascade-hit-chain cascade-source');
    cy.edges().removeClass('dimmed cascade-edge cascade-edge-chain');
    cy.nodes().style('opacity', 1);

    simActive   = false;
    simSourceId = null;

    document.getElementById('sim-banner').classList.remove('show');
    closeImpactDrawer();
    document.getElementById('btn-impact-toggle').style.display = 'none';
    document.getElementById('panel-simulate-wrap').style.display = '';
    document.getElementById('panel-clear-wrap').style.display    = 'none';

    if (selectedNode) openPanel(selectedNode.data());
  }, totalDuration);

  simTimeouts.push(finalTId);
}


function openImpactDrawer(result) {
  const blocked  = result.blockedCourses || [];
  const severity = formatSeverity(result.severity);
  const panel    = document.getElementById('node-panel');
  panel.classList.add('open', 'cm-panel-impact');

  if (blocked.length === 0) {
    panel.innerHTML = `
      <div class="cm-impact-header">
        <div class="d-flex align-items-center gap-2">
          <span class="material-symbols-outlined" style="font-size:19px;color:#10b981;font-variation-settings:'FILL' 1,'wght' 400">check_circle</span>
          <h3 class="fw-800 mb-0" style="font-size:.95rem;color:var(--c-text);">Simulation Result</h3>
        </div>
        <button id="btn-close-impact" class="cm-panel-close">
          <span class="material-symbols-outlined" style="font-size:20px;font-variation-settings:'FILL' 0,'wght' 300">close</span>
        </button>
      </div>
      <div class="cm-panel-section">
        <p style="font-size:13px;color:var(--c-text-sub);">No downstream courses are affected by failing <strong>${result.failedCourseCode}</strong>.</p>
      </div>
      <div class="cm-panel-action">
        <button id="btn-clear-impact" class="cm-btn-clear w-100">
          <span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">refresh</span>
          Clear Simulation
        </button>
      </div>
    `;
    document.getElementById('btn-close-impact').addEventListener('click', () => { closePanel(); clearSimAnimated(); });
    document.getElementById('btn-clear-impact').addEventListener('click', clearSimAnimated);
    return;
  }

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

    <div class="cm-damage-headline">
      <div class="cm-damage-number">${result.blockedCoursesCount}</div>
      <div>
        <p class="fw-700 mb-0" style="font-size:14px;color:var(--c-text);">Courses Blocked</p>
        <p style="font-size:11px;color:var(--c-muted);margin:0;">by failing <strong>${result.failedCourseCode}</strong></p>
      </div>
      <span class="cm-damage-badge">${severity}</span>
    </div>

    <div class="cm-metrics-row">
      <div class="cm-impact-metric">
        <p class="cm-impact-metric-val" style="color:var(--c-primary);">${result.semestersAffected}</p>
        <p class="cm-impact-metric-label">Semesters</p>
      </div>
      <div class="cm-impact-metric">
        <p class="cm-impact-metric-val" style="color:#ef4444;">+${result.graduationDelaySemesters} sem</p>
        <p class="cm-impact-metric-label">Grad Delay</p>
      </div>
    </div>

    ${result.scenarioSummary ? `
    <div class="cm-panel-section" style="padding-top:0;">
      <p style="font-size:11.5px;color:var(--c-text-sub);line-height:1.5;margin:0;">${result.scenarioSummary}</p>
    </div>` : ''}

    <div class="cm-panel-section">
      <p class="cm-panel-section-title">Blocked Courses</p>
      <div class="d-flex flex-column gap-1">
        ${blocked.map((b,i) => {
          const isDirect = b.depth === 1;
          return `
          <div class="cm-blocked-row ${isDirect ? 'cm-blocked-row-direct' : 'cm-blocked-row-chain'}" style="animation-delay:${i*60}ms">
            <span class="material-symbols-outlined" style="font-size:15px;color:${isDirect ? '#ef4444' : '#d97706'};font-variation-settings:'FILL' 1,'wght' 400">${isDirect ? 'error' : 'account_tree'}</span>
            <div class="flex-fill">
              <p class="fw-700 mb-0" style="font-size:12px;color:var(--c-text);">${b.code} — ${b.name}</p>
              <p style="font-size:10.5px;color:var(--c-muted);margin:0;">${isDirect ? 'Direct' : 'Chain'} dependency</p>
            </div>
            <span class="cm-dep-tag ${isDirect ? 'cm-dep-direct' : 'cm-dep-chain'}">${isDirect ? 'Direct' : 'Chain'}</span>
          </div>`;
        }).join('')}
      </div>
    </div>

    <div class="cm-panel-action d-flex flex-column gap-2">
      <a href="/Student/ImpactAnalyzer?courseId=${result.failedCourseId}" class="cm-btn-view-report text-decoration-none d-flex align-items-center justify-content-center gap-2">
        <span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">open_in_new</span>
        View Full Impact Report
      </a>
      <button id="btn-clear-impact" class="cm-btn-clear w-100">
        <span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">refresh</span>
        Clear Simulation
      </button>
    </div>
    <div class="cm-panel-ask-ai">
      <a href="/Student/AiAdvisor" class="d-flex align-items-center gap-1" style="font-size:12px;color:var(--c-primary);text-decoration:none;font-weight:600;">
        <span class="material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 0,'wght' 300">auto_awesome</span>
        Ask AI Advisor for recovery plan
      </a>
    </div>
  `;

  document.getElementById('btn-close-impact').addEventListener('click', () => { closePanel(); clearSimAnimated(); });
  document.getElementById('btn-clear-impact').addEventListener('click', clearSimAnimated);
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
      <a href="/Student/AiAdvisor" class="d-flex align-items-center gap-1" style="font-size:12px;color:var(--c-primary);text-decoration:none;font-weight:600;">
        <span class="material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 0,'wght' 300">auto_awesome</span>
        Ask AI Advisor about this course
      </a>
    </div>
  `;

  document.getElementById('btn-close-panel').addEventListener('click', closePanel);
  document.getElementById('btn-simulate').addEventListener('click', startSim);
  document.getElementById('btn-clear-panel').addEventListener('click', clearSimAnimated);
}

function updateGraphFilter(filterType) {
  if (simActive) {
    clearSimAnimated();
  }
  closePanel();

  let filteredCourses = COURSES;
  if (filterType === 'core') {
    filteredCourses = COURSES.filter(c => c.courseType === 0);
  } else if (filterType === 'elective') {
    filteredCourses = COURSES.filter(c => c.courseType === 1 || c.courseType === 2);
  } else if (filterType === 'uni') {
    filteredCourses = COURSES.filter(c => c.courseType === 3);
  }

  const elements = buildElements(filteredCourses);
  const positionMap = computeSemesterPositions(filteredCourses);

  cy.elements().remove();
  cy.add(elements);

  const layout = cy.layout(buildPresetLayout(positionMap, true));
  layout.run();
  updateSemesterHeaders();

  setTimeout(() => {
    cy.animate({ fit: { padding: 60 }, duration: 300, easing: 'ease-out-sine' });
  }, 450);
}

function wireFilterControls() {
  const chips = document.querySelectorAll('.cm-filter-chip');
  chips.forEach(chip => {
    chip.addEventListener('click', () => {
      chips.forEach(c => c.classList.remove('active'));
      chip.classList.add('active');
      const filterType = chip.getAttribute('data-type');
      updateGraphFilter(filterType);
    });
  });
}

})();



