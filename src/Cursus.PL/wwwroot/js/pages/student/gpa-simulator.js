/* ── Grade Scale ────────────────────────────────────────── */
const GRADE_SCALE = {
  'A+': 4.00, 'A': 4.00, 'A-': 3.67,
  'B+': 3.33, 'B': 3.00, 'B-': 2.67,
  'C+': 2.33, 'C': 2.00, 'C-': 1.67,
  'D+': 1.33, 'D': 1.00, 'F':  0.00,
};
const GRADE_OPTIONS = ['—', 'A+', 'A', 'A-', 'B+', 'B', 'B-', 'C+', 'C', 'C-', 'D+', 'D', 'F'];

/* ── Student Data ───────────────────────────────────────── */
const COMPLETED_CREDITS = 84;
const COMPLETED_QP      = 272.16;   // 84 × 3.24
const CURRENT_CGPA      = 3.24;

const CURRENT_COURSES = [
  { id: 'CS202',  name: 'Discrete Mathematics', credits: 3 },
  { id: 'MTH201', name: 'Linear Algebra',        credits: 3 },
  { id: 'CS301',  name: 'Operating Systems',     credits: 3 },
  { id: 'ENG201', name: 'Technical Writing',     credits: 2 },
  { id: 'CS303',  name: 'Computer Networks',     credits: 3 },
];

const IMPROVABLE_COURSES = [
  { id: 'MTH102', name: 'Calculus II',    credits: 3, originalGrade: 'D',  originalPoints: 1.00 },
  { id: 'CS102',  name: 'Programming I',  credits: 3, originalGrade: 'D+', originalPoints: 1.33 },
];

/* ── Custom grade dropdown ───────────────────── */
function buildCustomSelect(grades, idx, type) {
  const id = type === 'improve' ? `improve-sel-${idx}` : `course-sel-${idx}`;
  const dataAttr = type === 'improve' ? `data-improve-index="${idx}"` : `data-index="${idx}"`;
  const options = grades.map(g => {
    const pts = GRADE_SCALE[g];
    const cls = g === '—' ? '' : gradeClass(g);
    const label = (type === 'improve' && g === '—') ? '— Keep' : g;
    return `<div class="cg-option ${cls}" data-val="${g}">${label}</div>`;
  }).join('');
  return `
    <div class="custom-grade-select grade-select-wrap" id="${id}" ${dataAttr} tabindex="0">
      <div class="cg-trigger">
        <span class="cg-value">—</span>
        <span class="material-symbols-outlined cg-arrow" style="font-size:14px;font-variation-settings:'FILL' 0,'wght' 300">expand_more</span>
      </div>
      <div class="cg-dropdown">
        ${options}
      </div>
    </div>`;
}

/* ── Grade ──────────────────────────────────── */
function gradeClass(grade) {
  const pts = GRADE_SCALE[grade];
  if (pts === undefined) return '';
  if (pts >= 3.67) return 'grade-a';
  if (pts >= 3.00) return 'grade-b';
  if (pts >= 2.00) return 'grade-c';
  if (pts >= 1.00) return 'grade-d';
  return 'grade-f';
}

/* ── Render Current Courses Table ───────────────────────── */
function renderCoursesTable() {
  const body = document.getElementById('courses-body');
  body.innerHTML = '';

  CURRENT_COURSES.forEach((c, i) => {
    const row = document.createElement('div');
    row.className = 'grade-table-row';
    row.innerHTML = `
      <span class="grade-row-code">${c.id}</span>
      <span class="grade-row-name">${c.name}</span>
      <span class="grade-row-credits">${c.credits} cr</span>
      ${buildCustomSelect(GRADE_OPTIONS, i, 'course')}`;
    body.appendChild(row);
  });
}

/* ── Render Grade Improvement Table ─────────────────────── */
function renderImprovementTable() {
  const body = document.getElementById('improvement-body');
  body.innerHTML = '';

  IMPROVABLE_COURSES.forEach((c, i) => {
    const eligibleGrades = ['—', ...Object.keys(GRADE_SCALE).filter(
      g => GRADE_SCALE[g] > c.originalPoints
    )];

    const row = document.createElement('div');
    row.className = 'grade-table-row';
    row.innerHTML = `
      <span class="grade-row-code">${c.id}</span>
      <span class="grade-row-name">${c.name}</span>
      <span class="grade-row-credits">${c.credits} cr</span>
      <span class="original-grade-chip">${c.originalGrade}</span>
      ${buildCustomSelect(eligibleGrades, i, 'improve')}`;
    body.appendChild(row);
  });
}

/* ── Color Code ───── */
function styleSelect(sel, grade) {
  const trigger = sel.querySelector ? sel.querySelector('.cg-trigger') : sel;
  if (!trigger) return;
  trigger.classList.remove('grade-a', 'grade-b', 'grade-c', 'grade-d', 'grade-f');
  if (grade && grade !== '—') trigger.classList.add(gradeClass(grade));
}

/* ── Custom grade dropdowns ────────────────────────── */
function initCustomSelects() {
}

function setupGradeSelectDelegation() {

  function closeAll() {
    document.querySelectorAll('.custom-grade-select.open').forEach(s => {
      s.classList.remove('open');
      const arr = s.querySelector('.cg-arrow');
      if (arr) arr.style.transform = '';
    });
  }

  document.addEventListener('click', e => {
    const trigger = e.target.closest('.cg-trigger');
    if (trigger) {
      e.stopPropagation();
      const sel    = trigger.closest('.custom-grade-select');
      const isOpen = sel.classList.contains('open');
      closeAll();
      if (!isOpen) {
        sel.classList.add('open');
        const arr = sel.querySelector('.cg-arrow');
        if (arr) arr.style.transform = 'rotate(180deg)';
      }
      return;
    }

    const opt = e.target.closest('.cg-option');
    if (opt) {
      e.stopPropagation();
      const dropdown = opt.closest('.cg-dropdown');
      const sel      = opt.closest('.custom-grade-select');
      const val      = opt.dataset.val;
      const valueEl  = sel.querySelector('.cg-value');
      if (valueEl) valueEl.textContent = opt.textContent.trim();
      sel.dataset.currentVal = val;
      dropdown.querySelectorAll('.cg-option').forEach(o => o.classList.remove('active'));
      opt.classList.add('active');
      styleSelect(sel, val);
      closeAll();
      calculate();
      return;
    }

    if (!e.target.closest('.custom-grade-select')) closeAll();
  });

  document.addEventListener('keydown', e => {
    if (e.key === 'Escape') closeAll();
  });
}

/* ── Calculation Engine ────────────────────────────── */
function calculate() {
  const courseSelects  = document.querySelectorAll('#courses-body .custom-grade-select');
  const improveSelects = document.querySelectorAll('#improvement-body .custom-grade-select');

  let semCredits = 0;
  let semQP      = 0;
  let anySelected = false;

  // ── Current semester courses
  courseSelects.forEach((sel, i) => {
    const grade = sel.dataset.currentVal || '—';
    if (grade === '—') return;
    anySelected = true;
    const credits = CURRENT_COURSES[i].credits;
    semCredits += credits;
    semQP      += credits * GRADE_SCALE[grade];
  });

  let improveQPDelta = 0;
  improveSelects.forEach((sel, i) => {
    const grade = sel.dataset.currentVal || '—';
    if (grade === '—') return;
    const c = IMPROVABLE_COURSES[i];
    improveQPDelta += c.credits * (GRADE_SCALE[grade] - c.originalPoints);
  });

  const totalSemCredits = CURRENT_COURSES.reduce((s, c) => s + c.credits, 0);
  document.getElementById('stat-semester-credits').textContent = `${totalSemCredits} cr`;

  if (!anySelected) {
    setHeroEmpty();
    updateTargetResult();
    return;
  }

  const sgpa = semCredits > 0 ? (semQP / semCredits) : 0;
  const totalQP = COMPLETED_QP + semQP + improveQPDelta;
  const totalCr = COMPLETED_CREDITS + semCredits;
  const cgpa    = totalCr > 0 ? (totalQP / totalCr) : 0;
  const delta   = cgpa - CURRENT_CGPA;

  setHeroValues(sgpa, cgpa, delta);
  updateStandingPill(sgpa);
  updateHonorBadge(sgpa);
  updateCgpaImpact(delta);
  updateTargetResult();
}

/* ── Hero: show placeholder ─────────────────────────────── */
function setHeroEmpty() {
  document.getElementById('predicted-sgpa').textContent = '—';
  document.getElementById('predicted-cgpa').textContent = '—';
  document.getElementById('cgpa-change-label').textContent = '';
  document.getElementById('stat-cgpa-impact').textContent = '—';
  const pill = document.getElementById('standing-pill');
  pill.className = 'standing-pill';
  pill.textContent = 'Select grades to see prediction';
  const badge = document.getElementById('honor-badge-wrap');
  badge.className = 'honor-badge-wrap';
}

/* ── Hero: set live values ──────────────────────────────── */
function setHeroValues(sgpa, cgpa, delta) {
  const sgpaEl = document.getElementById('predicted-sgpa');
  const cgpaEl = document.getElementById('predicted-cgpa');

  animatePop(sgpaEl, sgpa.toFixed(2));
  animatePop(cgpaEl, cgpa.toFixed(2));

  const changeEl = document.getElementById('cgpa-change-label');
  if (delta > 0.001) {
    changeEl.innerHTML = `<span class="change-positive">▲ +${delta.toFixed(2)}</span>`;
  } else if (delta < -0.001) {
    changeEl.innerHTML = `<span class="change-negative">▼ ${delta.toFixed(2)}</span>`;
  } else {
    changeEl.innerHTML = `<span class="change-neutral">±0.00</span>`;
  }
}

/* ── Number pop animation ───────────────────────────────── */
function animatePop(el, newValue) {
  if (el.textContent === newValue) return;
  el.textContent = newValue;
  el.classList.remove('gpa-pop');
  void el.offsetWidth;
  el.classList.add('gpa-pop');
}

/* ── Standing pill ──────────────────────────────────────── */
function updateStandingPill(sgpa) {
  const pill = document.getElementById('standing-pill');
  pill.className = 'standing-pill';

  if (sgpa < 1.0) {
    pill.classList.add('danger');
    pill.innerHTML = '<span class="material-symbols-outlined" style="font-size:15px;vertical-align:middle;font-variation-settings:\'FILL\' 1,\'wght\' 400">crisis_alert</span> Dismissal Risk — SGPA below 1.0';
  } else if (sgpa < 2.0) {
    pill.classList.add('warning');
    pill.innerHTML = '<span class="material-symbols-outlined" style="font-size:15px;vertical-align:middle;font-variation-settings:\'FILL\' 1,\'wght\' 400">warning</span> Predicted: Academic Probation';
  } else {
    pill.classList.add('good');
    pill.innerHTML = '<span class="material-symbols-outlined" style="font-size:15px;vertical-align:middle;font-variation-settings:\'FILL\' 1,\'wght\' 400">check_circle</span> Predicted: Good Standing';
  }
}

/* ── Honor badge ────────────────────────────────────────── */
function updateHonorBadge(sgpa) {
  const wrap  = document.getElementById('honor-badge-wrap');
  const inner = document.getElementById('honor-badge-inner');
  wrap.className = 'honor-badge-wrap';

  if (sgpa >= 3.75) {
    wrap.classList.add('visible', 'honor-president');
    inner.innerHTML = '<span class="material-symbols-outlined" style="font-size:15px;vertical-align:middle;font-variation-settings:\'FILL\' 1,\'wght\' 400">workspace_premium</span> President\'s List';
  } else if (sgpa >= 3.50) {
    wrap.classList.add('visible', 'honor-dean');
    inner.innerHTML = '<span class="material-symbols-outlined" style="font-size:15px;vertical-align:middle;font-variation-settings:\'FILL\' 1,\'wght\' 400">military_tech</span> Dean\'s List';
  } else if (sgpa >= 3.00) {
    wrap.classList.add('visible', 'honor-list');
    inner.innerHTML = '<span class="material-symbols-outlined" style="font-size:15px;vertical-align:middle;font-variation-settings:\'FILL\' 1,\'wght\' 400">star</span> Honor List';
  }
}

/* ── CGPA impact ───────────────────────────────────── */
function updateCgpaImpact(delta) {
  const el = document.getElementById('stat-cgpa-impact');
  if (Math.abs(delta) < 0.001) {
    el.textContent = '±0.00';
    el.style.color = 'var(--c-muted)';
  } else if (delta > 0) {
    el.textContent = `+${delta.toFixed(2)}`;
    el.style.color = '#10b981';
  } else {
    el.textContent = delta.toFixed(2);
    el.style.color = '#ef4444';
  }
}

/* ── Target GPA ────────────────────────── */
function updateTargetResult() {
  const target    = parseFloat(document.getElementById('target-slider').value);
  const resultEl  = document.getElementById('target-result');
  const semTotal  = CURRENT_COURSES.reduce((s, c) => s + c.credits, 0);

  document.getElementById('target-value-display').textContent = target.toFixed(2);

  const requiredSemQP   = target * (COMPLETED_CREDITS + semTotal) - COMPLETED_QP;
  const requiredSGPA    = requiredSemQP / semTotal;
  const maxPossibleCGPA = (COMPLETED_QP + semTotal * 4.0) / (COMPLETED_CREDITS + semTotal);

  resultEl.className = 'target-result';

  if (target <= CURRENT_CGPA) {
    resultEl.classList.add('achieved');
    resultEl.innerHTML = `<span class="material-symbols-outlined" style="font-size:14px;vertical-align:middle;font-variation-settings:'FILL' 1,'wght' 400">check_circle</span> You've <strong>already exceeded</strong> a CGPA of <strong>${target.toFixed(2)}</strong>. Any passing grades will keep you above this target.`;
    return;
  }

  if (requiredSGPA > 4.0) {
    resultEl.classList.add('unreachable');
    resultEl.innerHTML = `<span class="material-symbols-outlined" style="font-size:14px;vertical-align:middle;font-variation-settings:'FILL' 1,'wght' 400">warning</span> A CGPA of <strong>${target.toFixed(2)}</strong> is <strong>not achievable</strong> this semester.<br>
      Your maximum possible CGPA this semester is <strong>${maxPossibleCGPA.toFixed(2)}</strong>.`;
    return;
  }

  const closestGrade = Object.entries(GRADE_SCALE)
    .filter(([, pts]) => pts >= requiredSGPA - 0.17)
    .sort((a, b) => a[1] - b[1])[0];

  resultEl.classList.add('reachable');
  resultEl.innerHTML = `To reach <strong>${target.toFixed(2)} CGPA</strong>, you need a semester GPA of at least
    <strong>${requiredSGPA.toFixed(2)}</strong>
    — roughly a <strong>${closestGrade ? closestGrade[0] : 'A'}</strong> average across all courses.`;
}

/* ── Slider ─────────────────────────────────── */
document.getElementById('target-slider').addEventListener('input', updateTargetResult);

/* ── Reset button ───────────────────────────────────────── */
document.getElementById('reset-btn').addEventListener('click', () => {
  document.querySelectorAll('.custom-grade-select').forEach(sel => {
    sel.dataset.currentVal = '—';
    const valueEl = sel.querySelector('.cg-value');
    if (valueEl) valueEl.textContent = '—';
    sel.querySelector('.cg-dropdown')?.querySelectorAll('.cg-option').forEach(o => o.classList.remove('active'));
    const trigger = sel.querySelector('.cg-trigger');
    if (trigger) trigger.classList.remove('grade-a','grade-b','grade-c','grade-d','grade-f');
  });
  setHeroEmpty();
  updateTargetResult();
});

renderCoursesTable();
renderImprovementTable();
setupGradeSelectDelegation();
updateTargetResult();