/* ── Available course catalog ───────────────────────────── */
const CATALOG = [
  { id:'CS401',  name:'Compiler Design',         credits:3, type:'Core',         typeClass:'type-core', prereqs:['CS201','CS202'] },
  { id:'CS499',  name:'Senior Capstone',          credits:6, type:'Core',         typeClass:'type-core', prereqs:['CS401'] },
  { id:'AI501',  name:'Deep Learning',            credits:3, type:'Dept. Elective',typeClass:'type-elec', prereqs:['AI402'] },
  { id:'SEC301', name:'Cybersecurity Fundamentals',credits:3,type:'Dept. Elective',typeClass:'type-elec', prereqs:['CS303'] },
  { id:'DS305',  name:'Database Systems',         credits:3, type:'Dept. Elective',typeClass:'type-elec', prereqs:['CS201'] },
  { id:'NET410', name:'Cloud Computing',          credits:3, type:'Dept. Elective',typeClass:'type-elec', prereqs:['CS303'] },
  { id:'PSYC101',name:'Intro to Psychology',      credits:3, type:'Free Elective', typeClass:'type-free', prereqs:[] },
  { id:'ECON101',name:'Principles of Economics',  credits:3, type:'Free Elective', typeClass:'type-free', prereqs:[] },
  { id:'PHIL105',name:'Ethics in Tech',            credits:2, type:'University Req.',typeClass:'type-univ', prereqs:[] },
  { id:'MATH301',name:'Probability & Statistics',  credits:3, type:'Core',         typeClass:'type-core', prereqs:['MTH201'] },
  { id:'SE401',  name:'Software Engineering',     credits:3, type:'Core',         typeClass:'type-core', prereqs:['CS201','CS301'] },
];

/* ── Student data ──────────────── */
const COMPLETED_COURSES  = ['CS101','CS201','CS202','AI402','WEB200','MUS101','ART200','ENG102','HIST201','PHYS101','MATH101'];
const COMPLETED_CREDITS  = 84;
const TOTAL_CREDITS      = 132;
const CREDIT_LIMIT       = 18;
const OVERLOAD_LIMIT     = 21;
const STUDENT_CGPA       = 3.24;

/* ── Planned courses state ──────────────────────────────── */
let plannedIds = [];

/* ── Read planned courses from DOM ────────────────── */
function init() {
  document.querySelectorAll('#planned-courses-list .planned-row').forEach(row => {
    plannedIds.push(row.dataset.courseId);
  });
  updateSummary();

  const obs = new IntersectionObserver(entries => {
    entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('in-view'); obs.unobserve(e.target); } });
  }, { threshold: 0.08 });
  document.querySelectorAll('[data-scroll],[data-scroll-group]').forEach(el => obs.observe(el));
}

/* ── Switch semester tab ────────────────────────────────── */
function switchSemester(btn, sem) {
  document.querySelectorAll('.sem-tab').forEach(t => t.classList.remove('active'));
  btn.classList.add('active');
  // In a real MVC app this would load the correct semester plan.
  // For demo we just show a toast.
  showToast(`Switched to ${btn.textContent}`, 'event_note');
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
      updateSummary();
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
    if (plannedIds.includes(c.id)) return false; // already in plan
    if (query && !c.id.toLowerCase().includes(query) && !c.name.toLowerCase().includes(query)) return false;
    return true;
  });

  if (available.length === 0) {
    list.innerHTML = `<div class="add-dropdown-item" style="color:var(--c-muted);cursor:default;">No courses match</div>`;
    return;
  }

  available.forEach(c => {
    const prereqsMet = c.prereqs.every(p => COMPLETED_COURSES.includes(p) || plannedIds.includes(p));
    const item = document.createElement('div');
    item.className = `add-dropdown-item${prereqsMet ? '' : ' disabled'}`;
    item.title = prereqsMet ? '' : `Requires: ${c.prereqs.join(', ')}`;
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
  // Close dropdown
  document.getElementById('add-dropdown').classList.remove('open');
  document.getElementById('add-search-input').value = '';

  const currentCr = getTotalCredits();
  if (currentCr + course.credits > OVERLOAD_LIMIT) {
    showToast('Exceeds maximum credit limit!', 'error', true);
    return;
  }

  plannedIds.push(course.id);

  const row = document.createElement('div');
  row.className = 'planned-row';
  row.dataset.courseId = course.id;
  row.style.opacity   = '0';
  row.style.transform = 'translateX(-10px)';
  row.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
  row.innerHTML = `
    <span class="planned-code">${course.id}</span>
    <span class="planned-name">${course.name}</span>
    <span class="planned-type-badge ${course.typeClass}">${course.type}</span>
    <span class="planned-credits">${course.credits} cr</span>
    <button class="remove-course-btn" onclick="removeCourse('${course.id}',event)" title="Remove course">
      <span class="material-symbols-outlined">close</span>
    </button>`;

  document.getElementById('planned-courses-list').appendChild(row);

  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      row.style.opacity   = '1';
      row.style.transform = 'translateX(0)';
    });
  });

  updateSummary();
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
    c.prereqs.forEach(p => {
      if (!COMPLETED_COURSES.includes(p) && !plannedIds.includes(p)) {
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

  const pct = Math.min((totalAfter / TOTAL_CREDITS) * 100, 100).toFixed(1);
  const bar  = document.getElementById('plan-bar');
  bar.style.setProperty('--bar-w', pct + '%');
  bar.style.animation = 'none';
  void bar.offsetWidth;
  bar.style.animation = '';

  const semLeft = Math.ceil((TOTAL_CREDITS - totalAfter) / 15);
  const gradText = semLeft <= 0 ? 'Graduation requirements met! 🎓'
    : `~${semLeft} semester${semLeft > 1 ? 's' : ''} remaining after this plan`;
  document.getElementById('proj-grad-text').innerHTML =
    `<strong style="color:var(--c-primary)">${gradText}</strong>`;
}

/* ── Save plan ──────────────────────────────────────────── */
function savePlan() {
  showToast('Semester plan saved!', 'check_circle');
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
