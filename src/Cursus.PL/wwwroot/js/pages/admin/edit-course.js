const COURSE_CATALOG = [
  { code: 'CS101', name: 'Introduction to Computer Science', dept: 'Computer Science' },
  { code: 'CS201', name: 'Data Structures & Algorithms', dept: 'Computer Science' },
  { code: 'CS202', name: 'Object-Oriented Programming', dept: 'Computer Science' },
  { code: 'CS301', name: 'Operating Systems', dept: 'Computer Science' },
  { code: 'CS302', name: 'Computer Networks', dept: 'Computer Science' },
  { code: 'CS401', name: 'Software Engineering', dept: 'Computer Science' },
  { code: 'MTH101', name: 'Calculus I', dept: 'Mathematics' }
];

let selectedPrereqs = [
  { code: 'CS101', name: 'Introduction to Computer Science' },
  { code: 'MTH101', name: 'Calculus I' }
];
let isDirty = false;

function showError(groupId, errId) {
  const group = document.getElementById(groupId);
  const error = document.getElementById(errId);
  if (group) group.classList.add('is-error');
  if (error) error.style.display = 'block';
}

function clearError(groupId) {
  const group = document.getElementById(groupId);
  if (!group) return;
  group.classList.remove('is-error');
  group.querySelectorAll('.field-error').forEach((node) => {
    node.style.display = 'none';
  });
}

function markDirty() {
  if (isDirty) return;
  isDirty = true;
  document.getElementById('unsaved-banner').classList.remove('d-none');
}

function filterPrereqSearch(query) {
  const dropdown = document.getElementById('prereq-dropdown');
  const value = query.trim().toLowerCase();
  if (!value) {
    dropdown.classList.add('d-none');
    return;
  }

  const matches = COURSE_CATALOG
    .filter((course) => {
      const isSelected = selectedPrereqs.some((item) => item.code === course.code);
      return !isSelected && (course.code.toLowerCase().includes(value) || course.name.toLowerCase().includes(value));
    })
    .slice(0, 6);

  dropdown.innerHTML = matches.length
    ? matches.map((course) => `
        <div class="prereq-option" onclick="addPrereq('${course.code}', '${course.name.replace(/'/g, "\\'")}')">
          <span class="prereq-option-code">${course.code}</span>
          <span class="prereq-option-name">${course.name}</span>
          <span class="prereq-option-dept">${course.dept}</span>
        </div>`).join('')
    : '<div class="prereq-option" style="cursor:default;"><span style="font-size:12px;color:var(--c-muted);">No matching courses found</span></div>';

  dropdown.classList.remove('d-none');
}

function renderPrereqChips() {
  const wrapper = document.getElementById('selected-prereqs');
  const emptyState = document.getElementById('prereq-empty');

  if (!selectedPrereqs.length) {
    wrapper.innerHTML = '';
    wrapper.appendChild(emptyState);
    emptyState.style.display = '';
    return;
  }

  emptyState.style.display = 'none';
  wrapper.innerHTML = selectedPrereqs.map((course) => `
    <span class="prereq-chip">
      ${course.code}
      <button class="prereq-chip-remove" onclick="removePrereq('${course.code}')" title="Remove">×</button>
    </span>
  `).join('');
  wrapper.appendChild(emptyState);
}

function addPrereq(code, name) {
  selectedPrereqs.push({ code, name });
  renderPrereqChips();
  document.getElementById('prereq-search').value = '';
  document.getElementById('prereq-dropdown').classList.add('d-none');
  document.getElementById('prereq-change-warn').classList.remove('d-none');
  document.getElementById('prereq-change-warn').style.display = 'flex';
  markDirty();
}

function removePrereq(code) {
  selectedPrereqs = selectedPrereqs.filter((course) => course.code !== code);
  renderPrereqChips();
  document.getElementById('prereq-change-warn').classList.remove('d-none');
  document.getElementById('prereq-change-warn').style.display = 'flex';
  markDirty();
}

function saveChanges() {
  if (window.CursusFormHelpers?.validateFormWithJquery && !window.CursusFormHelpers.validateFormWithJquery('#edit-course-form')) {
    return;
  }

  let isValid = true;
  const code = document.getElementById('course-code').value.trim();
  const name = document.getElementById('course-name').value.trim();
  const credits = Number(document.getElementById('course-credits').value);

  if (!code) { showError('fg-code', 'err-code'); isValid = false; } else { clearError('fg-code'); }
  if (!name) { showError('fg-name', 'err-name'); isValid = false; } else { clearError('fg-name'); }
  if (Number.isNaN(credits) || credits < 1 || credits > 6) {
    showError('fg-credits', 'err-credits');
    isValid = false;
  } else {
    clearError('fg-credits');
  }

  if (!isValid) {
    const firstErr = document.querySelector('.form-group-fancy.is-error');
    if (firstErr) firstErr.scrollIntoView({ behavior: 'smooth', block: 'center' });
    return;
  }

  const button = document.getElementById('save-btn');
  window.CursusFormHelpers?.setButtonLoading(button, 'Saving…');

  setTimeout(() => {
    console.log('Mock course update payload', {
      code,
      name,
      dept: document.getElementById('course-dept').value,
      type: document.getElementById('course-type').value,
      credits,
      passGrade: document.getElementById('course-pass-grade').value,
      minCredits: document.getElementById('course-min-credits').value,
      active: document.getElementById('course-active').checked,
      prerequisites: selectedPrereqs,
      corequisites: document.getElementById('coreq-input').value.trim()
    });

    const hint = document.getElementById('save-hint');
    hint.textContent = 'Last saved just now';
    hint.style.color = '#059669';

    isDirty = false;
    document.getElementById('unsaved-banner').classList.add('d-none');

    button.innerHTML = '<span class="material-symbols-outlined" style="font-size:17px;font-variation-settings:\'FILL\' 1,\'wght\' 400">check_circle</span> Saved!';
    button.style.background = 'linear-gradient(135deg,#059669,#047857)';

    setTimeout(() => {
      window.CursusFormHelpers?.resetButton(button, '<span class="material-symbols-outlined" style="font-size:17px;font-variation-settings:\'FILL\' 1,\'wght\' 400">save</span> Save Changes');
      button.style.removeProperty('background');
      hint.textContent = 'All changes will be applied immediately';
      hint.style.color = '';
    }, 1800);
  }, 700);
}

function discardChanges() {
  isDirty = false;
  document.getElementById('unsaved-banner').classList.add('d-none');
  document.getElementById('course-code').value = 'CS201';
  document.getElementById('course-name').value = 'Data Structures & Algorithms';
  document.getElementById('course-credits').value = 3;
}

function confirmArchive() {
  const code = document.getElementById('course-code').value.trim() || 'this course';
  document.getElementById('modal-course-code').textContent = code;
  document.getElementById('archive-modal').classList.remove('d-none');
}

function closeArchiveModal() {
  document.getElementById('archive-modal').classList.add('d-none');
}

function archiveCourse() {
  closeArchiveModal();
  window.location.href = 'courses.html';
}

function initEditCoursePage() {
  const toggleInput = document.getElementById('course-active');
  const toggleText = document.getElementById('toggle-text');
  if (toggleInput && toggleText) {
    toggleText.style.color = toggleInput.checked ? '#059669' : 'var(--c-muted)';
    toggleInput.addEventListener('change', () => {
      toggleText.textContent = toggleInput.checked ? 'Active' : 'Inactive';
      toggleText.style.color = toggleInput.checked ? '#059669' : 'var(--c-muted)';
      markDirty();
    });
  }

  document.querySelectorAll('.fancy-input, textarea, select').forEach((input) => {
    input.addEventListener(input.tagName === 'SELECT' ? 'change' : 'input', () => {
      const group = input.closest('.form-group-fancy');
      if (group) clearError(group.id);
      markDirty();
    });
  });

  document.querySelectorAll('input[name="avail"]').forEach((radio) => {
    radio.addEventListener('change', () => {
      document.querySelectorAll('.avail-label').forEach((label) => label.classList.remove('avail-label-checked'));
      if (radio.checked) radio.nextElementSibling.classList.add('avail-label-checked');
      markDirty();
    });
  });

  document.addEventListener('click', (event) => {
    if (!event.target.closest('.prereq-search-wrap')) {
      document.getElementById('prereq-dropdown').classList.add('d-none');
    }
  });

  if (window.CursusFormHelpers?.initJqueryValidation) {
    window.CursusFormHelpers.initJqueryValidation('#edit-course-form', {
      'course-code': { required: true },
      'course-name': { required: true },
      'course-credits': { required: true, min: 1, max: 6 }
    });
  }
}

initEditCoursePage();