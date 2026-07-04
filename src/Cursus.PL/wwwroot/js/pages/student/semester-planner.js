/* ── Available course catalog ───────────────────────────── */
const CATALOG = window.STUDENT_DATA.catalog || [];

/* ── Student data ──────────────── */
const COMPLETED_COURSES  = window.STUDENT_DATA.completedCourses || [];
const IN_PROGRESS_COURSES = window.STUDENT_DATA.inProgressCourses || [];
const COMPLETED_CREDITS  = window.STUDENT_DATA.completedCredits || 0;
const TOTAL_CREDITS      = window.STUDENT_DATA.totalCredits || 132;
const CREDIT_LIMIT       = window.STUDENT_DATA.creditLimit || 18;
const OVERLOAD_LIMIT     = window.STUDENT_DATA.overloadLimit || 21;

/* ── Planned courses state ──────────────────────────────── */
let plannedIds = [];

const STUDENT_ID = window.STUDENT_DATA.studentId || '';
const LS_KEY = STUDENT_ID ? `cursus.plannedCourses.${STUDENT_ID}` : '';

function persistPlan() {
  if (!LS_KEY) return false;
  try {
    const planned = plannedIds.map(id => {
      const c = CATALOG.find(x => x.id === id);
      return c ? { id: c.id, name: c.name, credits: c.credits } : null;
    }).filter(Boolean);
    localStorage.setItem(LS_KEY, JSON.stringify(planned));
    return true;
  } catch {
    return false;
  }
}

function prereqSatisfied(code) {
  return COMPLETED_COURSES.includes(code)
    || IN_PROGRESS_COURSES.includes(code)
    || plannedIds.includes(code);
}

function updateEmptyState() {
  const list = document.getElementById('planned-courses-list');
  const empty = document.getElementById('planned-empty-state');
  if (!list || !empty) return;
  const hasRows = list.querySelectorAll('.planned-row').length > 0;
  empty.style.display = hasRows ? 'none' : '';
}

/* ── Read planned courses from DOM / localStorage ────────────────── */
function appendPlannedRow(course) {
  const row = document.createElement('div');
  row.className = 'planned-row';
  row.dataset.courseId = course.id;
  row.innerHTML = `
    <span class="planned-code">${course.id}</span>
    <span class="planned-name">${course.name}</span>
    <span class="planned-type-badge ${course.typeClass}">${course.type}</span>
    <span class="planned-credits">${course.credits} cr</span>
    <button class="remove-course-btn" onclick="removeCourse('${course.id}',event)" title="Remove course">
      <span class="material-symbols-outlined">close</span>
    </button>`;
  document.getElementById('planned-courses-list').appendChild(row);
  return row;
}

function pruneStoredPlan(planned) {
  const blocked = new Set([...COMPLETED_COURSES, ...IN_PROGRESS_COURSES]);
  return planned.filter(p => p && p.id && !blocked.has(p.id));
}

function loadSavedPlan() {
  if (!LS_KEY) return;
  try {
    const raw = localStorage.getItem(LS_KEY);
    if (!raw) return;
    const stored = JSON.parse(raw);
    if (!Array.isArray(stored)) return;

    const pruned = pruneStoredPlan(stored);
    if (pruned.length !== stored.length) {
      localStorage.setItem(LS_KEY, JSON.stringify(pruned));
    }

    pruned.forEach(p => {
      if (plannedIds.includes(p.id)) return;
      const course = CATALOG.find(c => c.id === p.id);
      if (!course) return;
      plannedIds.push(course.id);
      appendPlannedRow(course);
    });
  } catch {
    // ignore corrupt storage
  }
}

function init() {
  loadSavedPlan();
  document.querySelectorAll('#planned-courses-list .planned-row').forEach(row => {
    if (!plannedIds.includes(row.dataset.courseId)) {
      plannedIds.push(row.dataset.courseId);
    }
  });
  updateEmptyState();
  updateSummary();

  const obs = new IntersectionObserver(entries => {
    entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('in-view'); obs.unobserve(e.target); } });
  }, { threshold: 0.08 });
  document.querySelectorAll('[data-scroll],[data-scroll-group]').forEach(el => obs.observe(el));
}

/* ── Remove a course ────────────────────────────────────── */
function removeCourse(id, e) {
  e.stopPropagation();
  const row = document.querySelector(`#planned-courses-list [data-course-id="${id}"]`);
  if (!row) return;

  row.style.transition = 'opacity 0.2s ease, transform 0.2s ease, max-height 0.25s ease';
  row.style.opacity  = '0';
  row.style.transform = 'translateX(12px)';
  row.style.maxHeight = row.offsetHeight + 'px';
  row.style.overflow  = 'hidden';

  setTimeout(() => {
    row.style.maxHeight = '0';
    row.style.padding   = '0';
    setTimeout(() => {
      row.remove();
      plannedIds = plannedIds.filter(x => x !== id);
      updateEmptyState();
      updateSummary();
      persistPlan();
      showToast(`Removed ${id}`, 'remove_circle');
    }, 250);
  }, 200);
}

/* ── Add-course dropdown ─────────────────────── */
function toggleAddDropdown() {
  const dd    = document.getElementById('add-dropdown');
  const input = document.getElementById('add-search-input');
  const isOpen = dd.classList.contains('open');
  dd.classList.toggle('open');
  if (!isOpen) {
    renderAddList('');
    setTimeout(() => input.focus(), 80);
  }
}

/* Close dropdown on outside click */
document.addEventListener('click', e => {
  if (!e.target.closest('.add-dropdown-wrap')) {
    document.getElementById('add-dropdown')?.classList.remove('open');
  }
});

/* ── Filter the add list ────────────────────────────────── */
function filterAddList(q) {
  renderAddList(q.toLowerCase().trim());
}

function renderAddList(query) {
  const list = document.getElementById('add-dropdown-list');
  list.innerHTML = '';

  const available = CATALOG.filter(c => {
    if (plannedIds.includes(c.id)) return false;
    if (IN_PROGRESS_COURSES.includes(c.id)) return false;
    if (COMPLETED_COURSES.includes(c.id)) return false;
    if (query && !c.id.toLowerCase().includes(query) && !c.name.toLowerCase().includes(query)) return false;
    return true;
  });

  if (available.length === 0) {
    list.innerHTML = `<div class="add-dropdown-item" style="color:var(--c-muted);cursor:default;">No courses match</div>`;
    return;
  }

  available.forEach(c => {
    const prereqs = c.prereqs || [];
    const prereqsMet = prereqs.every(p => prereqSatisfied(p));
    const item = document.createElement('div');
    item.className = `add-dropdown-item${prereqsMet ? '' : ' disabled'}`;
    item.title = prereqsMet ? '' : `Requires: ${prereqs.join(', ')}`;
    item.innerHTML = `
      <span class="add-item-code">${c.id}</span>
      <span class="add-item-name">${c.name}</span>
      <span class="add-item-credits">${c.credits} cr</span>
      ${!prereqsMet ? `<span class="material-symbols-outlined" style="font-size:14px!important;color:#b45309;font-variation-settings:'FILL' 1,'wght' 500">lock</span>` : ''}
    `;
    if (prereqsMet) {
      item.onclick = () => addCourse(c);
    }
    list.appendChild(item);
  });
}

/* ── Add a course to the plan ───────────────────────────── */
function addCourse(course) {
  document.getElementById('add-dropdown').classList.remove('open');
  document.getElementById('add-search-input').value = '';

  const currentCr = getTotalCredits();
  if (currentCr + course.credits > OVERLOAD_LIMIT) {
    showToast('Exceeds maximum credit limit!', 'error', true);
    return;
  }

  plannedIds.push(course.id);

  const row = appendPlannedRow(course);
  row.style.opacity   = '0';
  row.style.transform = 'translateX(-10px)';
  row.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
  updateEmptyState();

  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      row.style.opacity   = '1';
      row.style.transform = 'translateX(0)';
    });
  });

  updateSummary();
  persistPlan();
  showToast(`Added ${course.id} — ${course.name}`, 'add_circle');
}

/* ── Compute total planned credits ──────────────────────── */
function getTotalCredits() {
  return plannedIds.reduce((sum, id) => {
    const c = CATALOG.find(x => x.id === id);
    return sum + (c ? c.credits : 0);
  }, 0);
}

/* ── Update summary sidebar ──────────────────────────────── */
function updateSummary() {
  const cr         = getTotalCredits();
  const courseCount = plannedIds.length;
  const totalAfter  = COMPLETED_CREDITS + cr;

  const chip = document.getElementById('credit-chip');
  chip.textContent = `${cr} / ${CREDIT_LIMIT} cr`;
  chip.className = 'credit-chip ';
  if (cr === 0)                             chip.className += 'credit-chip-empty';
  else if (cr > CREDIT_LIMIT)              chip.className += 'credit-chip-over';
  else if (cr >= CREDIT_LIMIT - 3)         chip.className += 'credit-chip-ok';
  else                                     chip.className += 'credit-chip-warn';

  document.getElementById('stat-planned-cr').textContent  = `${cr} cr`;
  document.getElementById('stat-planned-cr').className = 'summary-stat-value ' +
    (cr > CREDIT_LIMIT ? 'danger' : cr > 0 ? 'ok' : '');
  document.getElementById('stat-total-after').textContent = `${totalAfter} / ${TOTAL_CREDITS}`;
  document.getElementById('stat-course-count').textContent = courseCount;

  let conflicts = [];
  plannedIds.forEach(id => {
    const c = CATALOG.find(x => x.id === id);
    if (!c) return;
    (c.prereqs || []).forEach(p => {
      if (!prereqSatisfied(p)) {
        conflicts.push(`${c.id} needs ${p}`);
      }
    });
  });

  const conflictEl = document.getElementById('stat-conflicts');
  if (conflicts.length === 0) {
    conflictEl.textContent = 'None ✓';
    conflictEl.className = 'summary-stat-value ok';
  } else {
    conflictEl.textContent = `${conflicts.length} conflict${conflicts.length > 1 ? 's' : ''}`;
    conflictEl.className = 'summary-stat-value danger';
  }

  const alertGrad = document.getElementById('alert-grad');
  if (cr > CREDIT_LIMIT) {
    alertGrad.style.display = 'flex';
    alertGrad.querySelector('.warn-text').textContent =
      `This plan (${cr} cr) exceeds your ${CREDIT_LIMIT}-credit limit by ${cr - CREDIT_LIMIT} cr.`;
  } else {
    alertGrad.style.display = 'none';
  }

  const pct = TOTAL_CREDITS > 0 ? Math.min((totalAfter / TOTAL_CREDITS) * 100, 100).toFixed(1) : '0.0';
  const bar  = document.getElementById('plan-bar');
  bar.style.setProperty('--bar-w', pct + '%');
  bar.style.animation = 'none';
  void bar.offsetWidth;
  bar.style.animation = '';

  const pctLabel = document.getElementById('progress-pct-label');
  if (pctLabel) {
    const compPct = TOTAL_CREDITS > 0
      ? Math.min((COMPLETED_CREDITS / TOTAL_CREDITS) * 100, 100).toFixed(1)
      : '0.0';
    pctLabel.textContent = `${compPct}% → ${pct}%`;
  }

  const semLeft = Math.ceil((TOTAL_CREDITS - totalAfter) / 15);
  const gradText = semLeft <= 0 ? 'Graduation requirements met! 🎓'
    : `~${semLeft} semester${semLeft > 1 ? 's' : ''} remaining after this plan`;
  document.getElementById('proj-grad-text').innerHTML =
    `<strong style="color:var(--c-primary)">${gradText}</strong>`;
}

/* ── Save plan ──────────────────────────────────────────── */
function savePlan() {
  if (!LS_KEY) {
    showToast('Unable to save — student ID missing', 'error', true);
    return;
  }

  if (persistPlan()) {
    showToast(`Semester plan saved! (${plannedIds.length} course${plannedIds.length !== 1 ? 's' : ''})`, 'check_circle');
  } else {
    showToast('Unable to save plan to browser storage', 'error', true);
  }
}

/* ── Toast helper ───────────────────────────────────────── */
let toastTimer = null;
function showToast(message, icon = 'info', isError = false) {
  document.querySelector('.toast-cursus')?.remove();
  clearTimeout(toastTimer);

  const toast = document.createElement('div');
  toast.className = 'toast-cursus';
  if (isError) toast.style.background = '#dc2626';
  toast.innerHTML = `
    <span class="material-symbols-outlined" style="font-variation-settings:'FILL' 1,'wght' 500">${icon}</span>
    ${message}`;
  document.body.appendChild(toast);

  toastTimer = setTimeout(() => {
    toast.classList.add('hiding');
    setTimeout(() => toast.remove(), 200);
  }, 2800);
}

init();


