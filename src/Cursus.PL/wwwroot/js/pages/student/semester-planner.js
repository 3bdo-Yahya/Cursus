const CATALOG = window.STUDENT_DATA.catalog || [];
const COMPLETED_COURSES = window.STUDENT_DATA.completedCourses || [];
const IN_PROGRESS_COURSES = window.STUDENT_DATA.inProgressCourses || [];
const COMPLETED_CREDITS = window.STUDENT_DATA.completedCredits || 0;
const TOTAL_CREDITS = window.STUDENT_DATA.totalCredits || 132;
const CREDIT_LIMIT = window.STUDENT_DATA.creditLimit || 18;

const TERM = window.STUDENT_DATA.term || {};
let termState = {
  academicYear: TERM.academicYear || '',
  semester: TERM.semester || 0,
  forcedInProgressCredits: TERM.forcedInProgressCredits || 0,
  plannedCredits: TERM.plannedCredits || 0,
  remainingRoom: TERM.remainingRoom ?? CREDIT_LIMIT
};

let plannedIds = (window.STUDENT_DATA.plannedCourses || []).map(c => c.code);

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

function hydratePlannedRows() {
  const planned = window.STUDENT_DATA.plannedCourses || [];
  planned.forEach(p => {
    const catalogCourse = CATALOG.find(c => c.id === p.code);
    if (!catalogCourse) return;
    appendPlannedRow(catalogCourse);
  });
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

  const bodyId = `cat-${category}`;
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
      courseId: (window.STUDENT_DATA.plannedCourses || []).find(c => c.code === course.id)?.courseId || CATALOG.find(c => c.id === course.id)?.courseId || 0,
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

  if (payload.capacity) {
    termState.forcedInProgressCredits = payload.capacity.forcedInProgressCredits;
    termState.plannedCredits = payload.capacity.plannedCredits;
    termState.remainingRoom = payload.capacity.remainingRoom;
  }

  updateEmptyState();
  updateSummary();
  renderAddList('');
  showToast(`Added ${course.id} — ${course.name}`, 'add_circle');
}

async function removeCourse(id, e) {
  e.stopPropagation();

  const course = CATALOG.find(c => c.id === id);
  if (!course) return;

  const response = await fetch('/Student/RemovePlannedCourse', {
    method: 'POST',
    headers: requestHeaders(),
    body: JSON.stringify({
      courseId: (window.STUDENT_DATA.plannedCourses || []).find(c => c.code === id)?.courseId || course.courseId || 0,
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

  if (payload.capacity) {
    termState.forcedInProgressCredits = payload.capacity.forcedInProgressCredits;
    termState.plannedCredits = payload.capacity.plannedCredits;
    termState.remainingRoom = payload.capacity.remainingRoom;
  }

  updateEmptyState();
  updateSummary();
  renderAddList('');
  showToast(`Removed ${id}`, 'remove_circle');
}

function getPlannedCredits() {
  return plannedIds.reduce((sum, id) => {
    const c = CATALOG.find(x => x.id === id);
    return sum + (c ? c.credits : 0);
  }, 0);
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
  plannedIds.forEach(id => {
    const c = CATALOG.find(x => x.id === id);
    if (!c) return;
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
  hydratePlannedRows();
  updateEmptyState();
  updateSummary();
}

window.toggleAddDropdown = toggleAddDropdown;
window.filterAddList = filterAddList;
window.removeCourse = removeCourse;
window.savePlan = savePlan;

init();
