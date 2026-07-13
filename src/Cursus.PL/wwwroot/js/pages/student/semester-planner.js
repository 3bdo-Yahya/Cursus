const CATALOG = window.STUDENT_DATA.catalog || [];
const TERMS = window.STUDENT_DATA.terms || [];
const COMPLETED_COURSES = window.STUDENT_DATA.completedCourses || [];
const IN_PROGRESS_COURSES = window.STUDENT_DATA.inProgressCourses || [];
const COMPLETED_CREDITS = window.STUDENT_DATA.completedCredits || 0;
const TOTAL_CREDITS = window.STUDENT_DATA.totalCredits || 132;
const CREDIT_LIMIT = window.STUDENT_DATA.creditLimit || 18;

const TERM = window.STUDENT_DATA.term || {};
const courseIdByCode = Object.fromEntries(CATALOG.map(c => [c.id, c.courseId]));

const termCache = new Map();
let activeTermKey = '';
let termState = {
  academicYear: '',
  semester: 0,
  forcedInProgressCredits: 0,
  plannedCredits: 0,
  remainingRoom: CREDIT_LIMIT
};
let plannedIds = [];

function termKey(year, semester) {
  return `${year}|${semester}`;
}

function semesterLabel(semester) {
  return ['Fall', 'Spring', 'Summer'][semester] || String(semester);
}

function getAntiForgeryToken() {
  const input = document.querySelector('input[name="__RequestVerificationToken"]');
  return input ? input.value : '';
}

function requestHeaders() {
  return {
    'Content-Type': 'application/json',
    'RequestVerificationToken': getAntiForgeryToken()
  };
}

function mapCourseTypeFromEnum(courseType) {
  switch (courseType) {
    case 'DeptElective': return { type: 'Dept. Elective', typeClass: 'type-elec' };
    case 'FreeElective': return { type: 'Free Elective', typeClass: 'type-free' };
    case 'UniversityReq': return { type: 'University Req.', typeClass: 'type-univ' };
    default: return { type: 'Core', typeClass: 'type-core' };
  }
}

function normalizePlannedCourse(pc) {
  const code = pc.code || pc.id;
  const catalogCourse = CATALOG.find(c => c.id === code);
  const mapped = pc.type && pc.typeClass
    ? { type: pc.type, typeClass: pc.typeClass }
    : mapCourseTypeFromEnum(pc.courseType || pc.type);

  return {
    id: code,
    courseId: pc.courseId || courseIdByCode[code] || 0,
    name: pc.name,
    credits: pc.credits ?? pc.creditHours ?? catalogCourse?.credits ?? 0,
    type: catalogCourse?.type || mapped.type,
    typeClass: catalogCourse?.typeClass || mapped.typeClass,
    prereqs: catalogCourse?.prereqs || []
  };
}

function getActivePlannedCourses() {
  const cached = termCache.get(activeTermKey);
  return (cached?.plannedCourses || []).map(normalizePlannedCourse);
}

function findPlannedCourse(code) {
  return getActivePlannedCourses().find(pc => pc.id === code)
    || normalizePlannedCourse({ code, ...(CATALOG.find(c => c.id === code) || {}) });
}

function getAllPlannedCodes() {
  const codes = new Set();
  termCache.forEach(state => state.plannedIds.forEach(id => codes.add(id)));
  return codes;
}

function prereqSatisfied(code) {
  return COMPLETED_COURSES.includes(code)
    || IN_PROGRESS_COURSES.includes(code)
    || getAllPlannedCodes().has(code);
}

function resolveCourseId(code) {
  const cached = termCache.get(activeTermKey);
  const fromTerm = cached?.plannedCourses?.find(pc => pc.code === code)?.courseId;
  return fromTerm || courseIdByCode[code] || 0;
}

function updateEmptyState() {
  const list = document.getElementById('planned-courses-list');
  const empty = document.getElementById('planned-empty-state');
  if (!list || !empty) return;
  const hasRows = list.querySelectorAll('.planned-row').length > 0;
  empty.style.display = hasRows ? 'none' : '';
}

function clearPlannedList() {
  document.querySelectorAll('#planned-courses-list .planned-row').forEach(row => row.remove());
}

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

function renderPlannedRows() {
  clearPlannedList();
  getActivePlannedCourses().forEach(course => appendPlannedRow(course));
  plannedIds = getActivePlannedCourses().map(course => course.id);
}

function updatePlanCardTitle() {
  const title = document.getElementById('plan-card-title');
  if (!title) return;
  title.textContent = `${semesterLabel(termState.semester)} ${termState.academicYear}`;
}

function applyCapacity(capacity) {
  if (!capacity) return;
  termState.forcedInProgressCredits = capacity.forcedInProgressCredits;
  termState.plannedCredits = capacity.plannedCredits;
  termState.remainingRoom = capacity.remainingRoom;

  const cached = termCache.get(activeTermKey);
  if (cached) {
    cached.forcedInProgressCredits = capacity.forcedInProgressCredits;
    cached.plannedCredits = capacity.plannedCredits;
    cached.remainingRoom = capacity.remainingRoom;
  }
}

function initTermCache() {
  const primary = TERMS.find(t => t.isPrimary) || TERMS[0];
  if (!primary) return;

  const key = termKey(primary.academicYear, primary.semester);
  const initialPlanned = (window.STUDENT_DATA.plannedCourses || []).map(pc => ({
    courseId: pc.courseId,
    code: pc.code,
    name: pc.name,
    credits: pc.credits,
    type: pc.type,
    typeClass: pc.typeClass
  }));

  termCache.set(key, {
    academicYear: primary.academicYear,
    semester: primary.semester,
    forcedInProgressCredits: TERM.forcedInProgressCredits || 0,
    plannedCredits: TERM.plannedCredits || 0,
    remainingRoom: TERM.remainingRoom ?? CREDIT_LIMIT,
    plannedIds: initialPlanned.map(pc => pc.code),
    plannedCourses: initialPlanned,
    loaded: true
  });

  activeTermKey = key;
  termState = {
    academicYear: primary.academicYear,
    semester: primary.semester,
    forcedInProgressCredits: TERM.forcedInProgressCredits || 0,
    plannedCredits: TERM.plannedCredits || 0,
    remainingRoom: TERM.remainingRoom ?? CREDIT_LIMIT
  };
  plannedIds = [...termCache.get(key).plannedIds];
}

async function loadTermPlan(academicYear, semester) {
  const key = termKey(academicYear, semester);
  const params = new URLSearchParams({
    academicYear,
    semester: String(semester)
  });

  const response = await fetch(`/Student/PlannerPlan?${params}`, {
    headers: { RequestVerificationToken: getAntiForgeryToken() }
  });

  const payload = await response.json();
  if (!response.ok) {
    throw new Error(payload.error || 'Unable to load term plan.');
  }

  const plannedCourses = (payload.plannedCourses || []).map(pc => ({
    courseId: pc.courseId,
    code: pc.code,
    name: pc.name,
    credits: pc.credits,
    courseType: pc.type
  }));
  termCache.set(key, {
    academicYear,
    semester,
    forcedInProgressCredits: payload.capacity?.forcedInProgressCredits || 0,
    plannedCredits: payload.capacity?.plannedCredits || 0,
    remainingRoom: payload.capacity?.remainingRoom ?? CREDIT_LIMIT,
    plannedIds: plannedCourses.map(pc => pc.code),
    plannedCourses,
    loaded: true
  });
}

async function switchTerm(academicYear, semester) {
  const key = termKey(academicYear, semester);
  if (key === activeTermKey) return;

  const select = document.getElementById('semester-select');
  if (select) select.disabled = true;

  try {
    if (!termCache.has(key) || !termCache.get(key).loaded) {
      await loadTermPlan(academicYear, semester);
    }

    activeTermKey = key;
    const state = termCache.get(key);
    termState = {
      academicYear: state.academicYear,
      semester: state.semester,
      forcedInProgressCredits: state.forcedInProgressCredits,
      plannedCredits: state.plannedCredits,
      remainingRoom: state.remainingRoom
    };
    plannedIds = [...state.plannedIds];

    renderPlannedRows();
    updatePlanCardTitle();
    updateEmptyState();
    updateSummary();
    renderAddList(document.getElementById('add-search-input')?.value?.toLowerCase().trim() || '');
    document.getElementById('add-dropdown')?.classList.remove('open');
  } catch (err) {
    showToast(err.message || 'Failed to switch term.', 'error', true);
    if (select) select.value = activeTermKey;
  } finally {
    if (select) select.disabled = false;
  }
}

function toggleAddDropdown() {
  const dd = document.getElementById('add-dropdown');
  const input = document.getElementById('add-search-input');
  const isOpen = dd.classList.contains('open');
  dd.classList.toggle('open');
  if (!isOpen) {
    renderAddList('');
    setTimeout(() => input.focus(), 80);
  }
}

document.addEventListener('click', e => {
  if (!e.target.closest('.add-dropdown-wrap')) {
    document.getElementById('add-dropdown')?.classList.remove('open');
  }
});

function filterAddList(q) {
  renderAddList(q.toLowerCase().trim());
}

function categoryMeta(category) {
  switch (category) {
    case 0: return { title: 'Core', open: true };
    case 1: return { title: 'Dept Elective', open: true };
    case 2: return { title: 'Free Elective', open: false };
    case 3: return { title: 'University Req', open: false };
    default: return { title: 'Other', open: false };
  }
}

function renderCategorySection(list, category, courses) {
  if (courses.length === 0) return;
  const meta = categoryMeta(category);

  const section = document.createElement('div');
  section.className = 'add-category-section';

  const bodyId = `cat-${category}-${activeTermKey.replace('|', '-')}`;
  section.innerHTML = `
    <button type="button" class="add-category-toggle" data-target="${bodyId}" aria-expanded="${meta.open}">
      <span>${meta.title}</span>
      <span class="material-symbols-outlined">expand_more</span>
    </button>
    <div id="${bodyId}" class="add-category-body${meta.open ? ' open' : ''}"></div>
  `;

  const body = section.querySelector('.add-category-body');
  courses.forEach(c => {
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

    body.appendChild(item);
  });

  list.appendChild(section);
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
    list.innerHTML = '<div class="add-dropdown-item" style="color:var(--c-muted);cursor:default;">No courses match</div>';
    return;
  }

  const grouped = new Map();
  available.forEach(c => {
    const key = typeof c.category === 'number' ? c.category : 0;
    if (!grouped.has(key)) grouped.set(key, []);
    grouped.get(key).push(c);
  });

  [0, 1, 2, 3].forEach(cat => renderCategorySection(list, cat, grouped.get(cat) || []));

  list.querySelectorAll('.add-category-toggle').forEach(btn => {
    btn.onclick = () => {
      const body = document.getElementById(btn.dataset.target);
      const open = body.classList.toggle('open');
      btn.setAttribute('aria-expanded', String(open));
      btn.classList.toggle('open', open);
    };
    if (btn.getAttribute('aria-expanded') === 'true') btn.classList.add('open');
  });
}

async function addCourse(course) {
  const currentTotal = termState.forcedInProgressCredits + getPlannedCredits();
  if (currentTotal + course.credits > CREDIT_LIMIT) {
    showToast('Exceeds credit capacity for this term.', 'error', true);
    return;
  }

  const response = await fetch('/Student/AddPlannedCourse', {
    method: 'POST',
    headers: requestHeaders(),
    body: JSON.stringify({
      courseId: course.courseId || courseIdByCode[course.id] || 0,
      academicYear: termState.academicYear,
      semester: termState.semester
    })
  });

  const payload = await response.json();
  if (!response.ok) {
    showToast(payload.error || 'Unable to add course.', 'error', true);
    return;
  }

  document.getElementById('add-dropdown').classList.remove('open');
  document.getElementById('add-search-input').value = '';

  plannedIds.push(course.id);
  appendPlannedRow(course);
  applyCapacity(payload.capacity);

  const cached = termCache.get(activeTermKey);
  if (cached) {
    cached.plannedIds = [...plannedIds];
    cached.plannedCourses = cached.plannedCourses || [];
    cached.plannedCourses.push({
      courseId: course.courseId || courseIdByCode[course.id],
      code: course.id,
      name: course.name,
      credits: course.credits,
      type: course.type,
      typeClass: course.typeClass
    });
  }

  updateEmptyState();
  updateSummary();
  renderAddList('');
  showToast(`Added ${course.id} — ${course.name}`, 'add_circle');
}

async function removeCourse(id, e) {
  e.stopPropagation();

  const course = findPlannedCourse(id);
  if (!course?.courseId) return;

  const response = await fetch('/Student/RemovePlannedCourse', {
    method: 'POST',
    headers: requestHeaders(),
    body: JSON.stringify({
      courseId: course.courseId || resolveCourseId(id),
      academicYear: termState.academicYear,
      semester: termState.semester
    })
  });

  const payload = await response.json();
  if (!response.ok) {
    showToast(payload.error || 'Unable to remove course.', 'error', true);
    return;
  }

  const row = document.querySelector(`#planned-courses-list [data-course-id="${id}"]`);
  if (row) row.remove();

  plannedIds = plannedIds.filter(x => x !== id);
  applyCapacity(payload.capacity);

  const cached = termCache.get(activeTermKey);
  if (cached) {
    cached.plannedIds = [...plannedIds];
    cached.plannedCourses = (cached.plannedCourses || []).filter(pc => pc.code !== id);
  }

  updateEmptyState();
  updateSummary();
  renderAddList('');
  showToast(`Removed ${id}`, 'remove_circle');
}

function getPlannedCredits() {
  return getActivePlannedCourses().reduce((sum, course) => sum + (course.credits || 0), 0);
}

function updateSummary() {
  const plannedCredits = getPlannedCredits();
  const totalInTerm = termState.forcedInProgressCredits + plannedCredits;
  const totalAfter = COMPLETED_CREDITS + plannedCredits;

  const chip = document.getElementById('credit-chip');
  chip.textContent = `${totalInTerm} / ${CREDIT_LIMIT} cr`;
  chip.className = 'credit-chip ';

  if (totalInTerm === 0) chip.className += 'credit-chip-empty';
  else if (totalInTerm > CREDIT_LIMIT) chip.className += 'credit-chip-over';
  else if (totalInTerm >= CREDIT_LIMIT - 3) chip.className += 'credit-chip-ok';
  else chip.className += 'credit-chip-warn';

  const statPlanned = document.getElementById('stat-planned-cr');
  statPlanned.textContent = `${plannedCredits} cr (+ ${termState.forcedInProgressCredits} forced)`;
  statPlanned.className = 'summary-stat-value ' + (totalInTerm > CREDIT_LIMIT ? 'danger' : plannedCredits > 0 ? 'ok' : '');

  document.getElementById('stat-total-after').textContent = `${totalAfter} / ${TOTAL_CREDITS}`;
  document.getElementById('stat-course-count').textContent = plannedIds.length;

  const conflicts = [];
  getActivePlannedCourses().forEach(c => {
    (c.prereqs || []).forEach(p => {
      if (!prereqSatisfied(p)) conflicts.push(`${c.id} needs ${p}`);
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
  if (totalInTerm > CREDIT_LIMIT) {
    alertGrad.style.display = 'flex';
    alertGrad.querySelector('.warn-text').textContent =
      `Term load (${totalInTerm} cr) exceeds ${CREDIT_LIMIT}-credit limit by ${totalInTerm - CREDIT_LIMIT} cr.`;
  } else {
    alertGrad.style.display = 'none';
  }

  const pct = TOTAL_CREDITS > 0 ? Math.min((totalAfter / TOTAL_CREDITS) * 100, 100).toFixed(1) : '0.0';
  const bar = document.getElementById('plan-bar');
  bar.style.setProperty('--bar-w', `${pct}%`);

  const pctLabel = document.getElementById('progress-pct-label');
  if (pctLabel) {
    const compPct = TOTAL_CREDITS > 0 ? Math.min((COMPLETED_CREDITS / TOTAL_CREDITS) * 100, 100).toFixed(1) : '0.0';
    pctLabel.textContent = `${compPct}% → ${pct}%`;
  }

  const semLeft = Math.ceil((TOTAL_CREDITS - totalAfter) / 15);
  const gradText = semLeft <= 0 ? 'Graduation requirements met! 🎓' : `~${semLeft} semester${semLeft > 1 ? 's' : ''} remaining after this plan`;
  document.getElementById('proj-grad-text').innerHTML = `<strong style="color:var(--c-primary)">${gradText}</strong>`;
}

function savePlan() {
  showToast('Plan changes are auto-saved to database.', 'check_circle');
}

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

function init() {
  initTermCache();
  renderPlannedRows();
  updateEmptyState();
  updateSummary();

  const select = document.getElementById('semester-select');
  if (select) {
    select.addEventListener('change', () => {
      const [academicYear, semesterRaw] = select.value.split('|');
      switchTerm(academicYear, parseInt(semesterRaw, 10));
    });
  }

  const obs = new IntersectionObserver(entries => {
    entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('in-view'); obs.unobserve(e.target); } });
  }, { threshold: 0.08 });
  document.querySelectorAll('[data-scroll],[data-scroll-group]').forEach(el => obs.observe(el));
}

window.toggleAddDropdown = toggleAddDropdown;
window.filterAddList = filterAddList;
window.removeCourse = removeCourse;
window.savePlan = savePlan;

init();


