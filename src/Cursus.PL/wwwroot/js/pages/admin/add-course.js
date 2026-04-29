/* ── Sample course catalog for prerequisite search ──────── */
const COURSE_CATALOG = [
  { code: 'CS101', name: 'Introduction to Computer Science', dept: 'Computer Science' },
  { code: 'CS201', name: 'Data Structures & Algorithms',     dept: 'Computer Science' },
  { code: 'CS202', name: 'Object-Oriented Programming',      dept: 'Computer Science' },
  { code: 'CS301', name: 'Operating Systems',                dept: 'Computer Science' },
  { code: 'CS302', name: 'Computer Networks',                dept: 'Computer Science' },
  { code: 'CS401', name: 'Software Engineering',             dept: 'Computer Science' },
  { code: 'CS402', name: 'Database Systems',                 dept: 'Computer Science' },
  { code: 'MTH101', name: 'Calculus I',                      dept: 'Mathematics' },
  { code: 'MTH102', name: 'Linear Algebra',                  dept: 'Mathematics' },
  { code: 'MTH201', name: 'Calculus II',                     dept: 'Mathematics' },
  { code: 'MTH202', name: 'Discrete Mathematics',            dept: 'Mathematics' },
  { code: 'PH101',  name: 'Physics I',                       dept: 'Engineering' },
  { code: 'PH102',  name: 'Physics II',                      dept: 'Engineering' },
  { code: 'ENG101', name: 'Engineering Drawing',             dept: 'Engineering' },
  { code: 'BUS101', name: 'Business Fundamentals',           dept: 'Business' },
  { code: 'BUS201', name: 'Accounting Principles',           dept: 'Business' },
];

let selectedPrereqs = [];
let currentStep = 1;


/* ── Step navigation ─────────────────────────────────────── */
function goToStep(targetStep) {
  if (targetStep > currentStep && !validateStep(currentStep)) {
    const nextBtn = document.querySelector(`#step-${currentStep} .btn-step-next`);
    if (nextBtn) {
      nextBtn.classList.add('btn-shake');
      setTimeout(() => nextBtn.classList.remove('btn-shake'), 500);
    }
    return;
  }

  // Mark current step as done
  const prevItem = document.querySelector(`.step-item[data-step="${currentStep}"]`);
  if (prevItem && targetStep > currentStep) {
    prevItem.classList.remove('step-active');
    prevItem.classList.add('step-done');
  }

  // If going back, remove done from target
  if (targetStep < currentStep) {
    const futureItem = document.querySelector(`.step-item[data-step="${currentStep}"]`);
    if (futureItem) {
      futureItem.classList.remove('step-done', 'step-active');
    }
  }

  // Hide current, show target
  document.getElementById(`step-${currentStep}`).classList.add('d-none');
  const nextPanel = document.getElementById(`step-${targetStep}`);
  nextPanel.classList.remove('d-none');

  // Activate target step circle
  document.querySelectorAll('.step-item').forEach(item => {
    const s = parseInt(item.dataset.step);
    if (s === targetStep) {
      item.classList.add('step-active');
      item.classList.remove('step-done');
    } else if (s < targetStep) {
      item.classList.add('step-done');
      item.classList.remove('step-active');
    }
  });

  currentStep = targetStep;

  if (targetStep === 4) populateReview();

  window.scrollTo({ top: 0, behavior: 'smooth' });
}


/* ── Per-step validation ──────────────────────────────────── */
function validateStep(step) {
  let ok = true;

  if (step === 1) {
    if (!validateField('course-code', 'fg-code', 'err-code'))   ok = false;
    if (!validateField('course-name', 'fg-name', 'err-name'))   ok = false;
    if (!validateSelect('course-dept', 'fg-dept', 'err-dept'))  ok = false;
    if (!validateSelect('course-type', 'fg-type', 'err-type'))  ok = false;

    // Scroll to first error if any
    if (!ok) {
      const firstErr = document.querySelector('.form-group-fancy.is-error');
      if (firstErr) firstErr.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  if (step === 2) {

  }

  return ok;
}

function validateField(inputId, groupId, errId) {
  const el = document.getElementById(inputId);
  const val = el ? el.value.trim() : '';
  if (!val) { showError(groupId, errId); return false; }
  clearError(groupId); return true;
}

function validateSelect(selectId, groupId, errId) {
  const el = document.getElementById(selectId);
  const val = el ? el.value : '';
  if (!val) { showError(groupId, errId); return false; }
  clearError(groupId); return true;
}

function showError(groupId, errId) {
  const grp = document.getElementById(groupId);
  const err = document.getElementById(errId);
  if (grp) grp.classList.add('is-error');
  if (err) err.style.display = 'block';
}
function clearError(groupId) {
  const grp = document.getElementById(groupId);
  if (grp) {
    grp.classList.remove('is-error');
    grp.querySelectorAll('.field-error').forEach(e => e.style.display = 'none');
  }
}

document.querySelectorAll('.fancy-input').forEach(el => {
  const evt = (el.tagName === 'SELECT') ? 'change' : 'input';
  el.addEventListener(evt, () => {
    const fg = el.closest('.form-group-fancy');
    if (fg) {
      fg.classList.remove('is-error');
      fg.querySelectorAll('.field-error').forEach(e => e.style.display = 'none');
    }
  });
});


/* ── Credit selector ─────────────────────────────────────── */
function selectCredit(btn) {
  document.querySelectorAll('.credit-btn').forEach(b => b.classList.remove('credit-btn-selected'));
  btn.classList.add('credit-btn-selected');
  document.getElementById('course-credits').value = btn.dataset.val;
}


/* ── Availability radio styling ──────────────────────────── */
document.querySelectorAll('input[name="avail"]').forEach(radio => {
  radio.addEventListener('change', () => {
    document.querySelectorAll('.avail-label').forEach(l => l.classList.remove('avail-label-checked'));
    if (radio.checked) radio.nextElementSibling.classList.add('avail-label-checked');
  });
});


/* ── Toggle active status text ───────────────────────────── */
const toggleInput = document.getElementById('course-active');
const toggleText  = document.getElementById('toggle-text');
if (toggleInput && toggleText) {
  toggleInput.addEventListener('change', () => {
    toggleText.textContent = toggleInput.checked ? 'Active' : 'Inactive';
    toggleText.style.color = toggleInput.checked ? '#059669' : 'var(--c-muted)';
  });
}


/* ── Prerequisite search ─────────────────────────────────── */
function filterPrereqSearch(query) {
  const dropdown = document.getElementById('prereq-dropdown');
  query = query.trim().toLowerCase();

  if (!query) { dropdown.classList.add('d-none'); return; }

  const results = COURSE_CATALOG.filter(c =>
    (c.code.toLowerCase().includes(query) || c.name.toLowerCase().includes(query)) &&
    !selectedPrereqs.find(p => p.code === c.code)
  ).slice(0, 6);

  if (!results.length) {
    dropdown.innerHTML = '<div class="prereq-option" style="cursor:default;"><span style="font-size:12px;color:var(--c-muted);">No matching courses found</span></div>';
  } else {
    dropdown.innerHTML = results.map(c => `
      <div class="prereq-option" onclick="addPrereq('${c.code}', '${c.name.replace(/'/g, "\\'")}')">
        <span class="prereq-option-code">${c.code}</span>
        <span class="prereq-option-name">${c.name}</span>
        <span class="prereq-option-dept">${c.dept}</span>
      </div>
    `).join('');
  }

  dropdown.classList.remove('d-none');
}

function addPrereq(code, name) {
  selectedPrereqs.push({ code, name });
  renderPrereqChips();
  document.getElementById('prereq-search').value = '';
  document.getElementById('prereq-dropdown').classList.add('d-none');
}

function removePrereq(code) {
  selectedPrereqs = selectedPrereqs.filter(p => p.code !== code);
  renderPrereqChips();
}

function renderPrereqChips() {
  const wrap  = document.getElementById('selected-prereqs');
  const empty = document.getElementById('prereq-empty');

  if (!selectedPrereqs.length) {
    wrap.innerHTML = '';
    wrap.appendChild(empty);
    empty.style.display = '';
    return;
  }

  empty.style.display = 'none';
  const chips = selectedPrereqs.map(p => `
    <span class="prereq-chip">
      ${p.code}
      <button class="prereq-chip-remove" onclick="removePrereq('${p.code}')" title="Remove">×</button>
    </span>
  `).join('');
  wrap.innerHTML = chips;
  wrap.appendChild(empty);
}

// Close dropdown on outside click
document.addEventListener('click', e => {
  if (!e.target.closest('.prereq-search-wrap')) {
    const dd = document.getElementById('prereq-dropdown');
    if (dd) dd.classList.add('d-none');
  }
});


/* ── Populate review step ────────────────────────────────── */
const DEPT_LABELS = { cs: 'Computer Science', math: 'Mathematics', eng: 'Engineering', bus: 'Business' };
const TYPE_LABELS = {
  'core': 'Core',
  'dept-elective': 'Department Elective',
  'free-elective': 'Free Elective',
  'university-req': 'University Requirement'
};
const AVAIL_LABELS = { fall: 'Fall Only', spring: 'Spring Only', both: 'Fall & Spring', all: 'All Semesters' };

function populateReview() {
  set('rv-code',    val('course-code'));
  set('rv-name',    val('course-name'));
  set('rv-dept',    DEPT_LABELS[val('course-dept')] || '—');
  set('rv-type',    TYPE_LABELS[val('course-type')] || '—');
  set('rv-credits', val('course-credits') + ' credit hour(s)');
  const avail = document.querySelector('input[name="avail"]:checked');
  set('rv-avail',   avail ? AVAIL_LABELS[avail.value] : '—');
  set('rv-pass',    val('course-pass-grade'));
  const active = document.getElementById('course-active');
  set('rv-status',  active && active.checked ? 'Active' : 'Inactive');

  const preEl = document.getElementById('rv-prereqs');
  if (selectedPrereqs.length) {
    preEl.innerHTML = selectedPrereqs.map(p =>
      `<span style="display:inline-block;margin:2px 3px;padding:3px 9px;border-radius:6px;font-size:12px;font-weight:700;background:var(--c-soft);color:var(--c-primary);border:1px solid rgba(79,70,229,.2);">${p.code}</span>`
    ).join('');
  } else {
    preEl.innerHTML = '<span style="color:var(--c-muted);font-style:italic;font-size:13px;">None</span>';
  }
}

function val(id)      { const el = document.getElementById(id); return el ? el.value.trim() : ''; }
function set(id, txt) { const el = document.getElementById(id); if (el) el.textContent = txt || '—'; }


/* ── Submit ──────────────────────────────────────────────── */
function submitCourse() {
  const btn = document.getElementById('submit-btn');
  btn.disabled = true;
  btn.innerHTML = '<span class="material-symbols-outlined" style="font-size:16px;animation:spin .7s linear infinite">progress_activity</span> Saving…';

  setTimeout(() => {
    const code = val('course-code');
    document.getElementById('success-code').textContent = code;

    document.getElementById('step-4').classList.add('d-none');
    document.getElementById('success-state').classList.remove('d-none');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, 900);
}

const spinStyle = document.createElement('style');
spinStyle.textContent = '@keyframes spin { to { transform: rotate(360deg); } }';
document.head.appendChild(spinStyle);


/* ── Reset form ──────────────────────────────────────────── */
function resetForm() {
  ['course-code','course-name','course-desc','course-dept','course-type',
   'course-pass-grade','course-min-credits','coreq-input'].forEach(id => {
    const el = document.getElementById(id);
    if (el) el.value = '';
  });
  document.getElementById('course-credits').value = '3';
  document.querySelectorAll('.credit-btn').forEach(b => b.classList.remove('credit-btn-selected'));
  document.querySelector('.credit-btn[data-val="3"]').classList.add('credit-btn-selected');

  const avail = document.querySelector('input[name="avail"][value="both"]');
  if (avail) { avail.checked = true; avail.nextElementSibling.classList.add('avail-label-checked'); }
  document.querySelectorAll('input[name="avail"]:not([value="both"])').forEach(r => {
    r.checked = false;
    r.nextElementSibling.classList.remove('avail-label-checked');
  });

  const toggle = document.getElementById('course-active');
  if (toggle) toggle.checked = true;
  if (toggleText) { toggleText.textContent = 'Active'; toggleText.style.color = ''; }

  selectedPrereqs = [];
  renderPrereqChips();

  currentStep = 1;
  document.getElementById('success-state').classList.add('d-none');
  document.getElementById('step-1').classList.remove('d-none');
  document.querySelectorAll('.step-item').forEach(item => {
    item.classList.remove('step-active', 'step-done');
    if (parseInt(item.dataset.step) === 1) item.classList.add('step-active');
  });

  const submitBtn = document.getElementById('submit-btn');
  submitBtn.disabled = false;
  submitBtn.innerHTML = '<span class="material-symbols-outlined" style="font-size:18px;font-variation-settings:\'FILL\' 1,\'wght\' 400">add_circle</span> Save Course';

  window.scrollTo({ top: 0, behavior: 'smooth' });
}


/* User menu: CursusUI.toggleDropdown in site.js */