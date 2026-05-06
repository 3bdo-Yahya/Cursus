const STUD_UNI_DEPTS = {
  svu: [
    { value: 'cs', label: 'Computer Science', icon: 'computer', color: 'cs' },
    { value: 'is', label: 'Information Systems', icon: 'storage', color: 'is' },
    { value: 'ai', label: 'Artificial Intelligence', icon: 'psychology', color: 'ai' },
    { value: 'it', label: 'Information Technology', icon: 'devices', color: 'it' }
  ],
  auc: [
    { value: 'cs', label: 'Computer Science', icon: 'computer', color: 'cs' }
  ],
  su: [
    { value: 'it', label: 'Information Technology', icon: 'devices', color: 'it' },
    { value: 'csse', label: 'Computer Science & Software Eng.', icon: 'code', color: 'cs' },
    { value: 'idss', label: 'Intelligent & Data Systems', icon: 'psychology', color: 'ai' }
  ]
};

let selectedUni = null;
let selectedDept = null;
let selectedSem = 'spring';
let selectedYear = '1';

function togglePassword() {
  const input = document.getElementById('student-password');
  const icon = document.getElementById('pass-toggle-icon');
  if (!input || !icon) return;

  const isMasked = input.type === 'password';
  input.type = isMasked ? 'text' : 'password';
  icon.textContent = isMasked ? 'visibility_off' : 'visibility';
}

function selectStudentUni(item) {
  const universitySelect = document.getElementById('student-university');
  if (!universitySelect) return;

  selectedUni = item.dataset.value;
  universitySelect.value = selectedUni;
  universitySelect.dispatchEvent(new Event('change', { bubbles: true }));

  item.closest('.custom-dropdown')
    .querySelectorAll('.custom-dropdown-item')
    .forEach((dropdownItem) => dropdownItem.classList.remove('selected'));
  item.classList.add('selected');

  const uniValueEl = document.getElementById('stud-uni-value');
  uniValueEl.textContent = item.querySelector('.custom-dropdown-label').textContent;
  uniValueEl.style.color = '';

  document.getElementById('stud-uni-dropdown').classList.remove('open');
  document.getElementById('stud-uni-btn').classList.remove('open');
  clearError('fg-university');
}

function rebuildStudentDeptDropdown(uniValue) {
  const departments = STUD_UNI_DEPTS[uniValue] || [];
  const dropdown = document.getElementById('stud-dept-dropdown');
  if (!dropdown) return;

  dropdown.innerHTML = departments
    .map((department) => `
      <div class="custom-dropdown-item"
           data-value="${department.value}"
           data-icon="${department.icon}"
           data-color="${department.color}"
           onclick="selectStudentDept(this)">
        <span class="custom-dropdown-check material-symbols-outlined" style="font-size:15px;font-variation-settings:'FILL' 1,'wght' 400">check_circle</span>
        <div><div class="custom-dropdown-label">${department.label}</div></div>
        <span class="dept-dot dept-dot-${department.color}"></span>
      </div>
    `)
    .join('');

  selectedDept = null;
  const deptValue = document.getElementById('stud-dept-value');
  deptValue.textContent = 'Select department…';
  deptValue.style.color = 'var(--c-muted)';

  const iconEl = document.getElementById('stud-dept-icon-el');
  if (iconEl) {
    iconEl.className = 'custom-select-icon';
    iconEl.innerHTML = '<span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:\'FILL\' 0,\'wght\' 300">apartment</span>';
  }
}

function selectStudentDept(item) {
  selectedDept = item.dataset.value;

  item.closest('.custom-dropdown')
    .querySelectorAll('.custom-dropdown-item')
    .forEach((dropdownItem) => dropdownItem.classList.remove('selected'));
  item.classList.add('selected');

  const deptValueEl = document.getElementById('stud-dept-value');
  deptValueEl.textContent = item.querySelector('.custom-dropdown-label').textContent;
  deptValueEl.style.color = '';

  const iconEl = document.getElementById('stud-dept-icon-el');
  if (iconEl) {
    iconEl.className = `custom-select-icon dept-icon-${item.dataset.color}`;
    iconEl.innerHTML = `<span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">${item.dataset.icon}</span>`;
  }

  document.getElementById('stud-dept-dropdown').classList.remove('open');
  document.getElementById('stud-dept-btn').classList.remove('open');
  clearError('fg-department');
}

function selectSemester(item) {
  selectedSem = item.dataset.value;

  item.closest('.custom-dropdown')
    .querySelectorAll('.custom-dropdown-item')
    .forEach((dropdownItem) => dropdownItem.classList.remove('selected'));
  item.classList.add('selected');

  document.getElementById('stud-sem-value').textContent =
    item.querySelector('.custom-dropdown-label').textContent;

  document.getElementById('stud-sem-dropdown').classList.remove('open');
  document.getElementById('stud-sem-btn').classList.remove('open');
}

function selectYearLevel(button) {
  document.querySelectorAll('.credit-btn').forEach((el) => el.classList.remove('credit-btn-selected'));
  button.classList.add('credit-btn-selected');
  selectedYear = button.dataset.val;
  document.getElementById('student-year-level').value = selectedYear;
}


function showError(groupId, errId) {
  const group = document.getElementById(groupId);
  if (group) group.classList.add('is-error');

  const error = document.getElementById(errId);
  if (error) error.style.display = 'block';
}

function clearError(groupId) {
  const group = document.getElementById(groupId);
  if (!group) return;

  group.classList.remove('is-error');
  group.querySelectorAll('.field-error').forEach((errorNode) => {
    errorNode.style.display = 'none';
  });
}

function markFieldDirty(el) {
  const group = el.closest('.form-group-fancy');
  if (!group) return;

  group.classList.remove('is-error');
  group.querySelectorAll('.field-error').forEach((errorNode) => {
    errorNode.style.display = 'none';
  });
}

function validateStudentForm() {
  let isValid = true;
  if (window.CursusFormHelpers?.validateFormWithJquery) {
    isValid = window.CursusFormHelpers.validateFormWithJquery('#form-view') && isValid;
  }

  const fullname = document.getElementById('student-fullname').value.trim();
  const email = document.getElementById('student-email').value.trim();
  const password = document.getElementById('student-password').value;
  const academicYear = document.getElementById('student-acyear').value.trim();

  if (!fullname) {
    showError('fg-fullname', 'err-fullname');
    isValid = false;
  } else {
    clearError('fg-fullname');
  }

  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailPattern.test(email)) {
    showError('fg-email', 'err-email');
    isValid = false;
  } else {
    clearError('fg-email');
  }

  if (password.length < 8) {
    showError('fg-password', 'err-password');
    isValid = false;
  } else {
    clearError('fg-password');
  }

  if (!selectedUni) {
    showError('fg-university', 'err-university');
    isValid = false;
  } else {
    clearError('fg-university');
  }

  if (!selectedDept) {
    showError('fg-department', 'err-department');
    isValid = false;
  } else {
    clearError('fg-department');
  }

  const academicYearPattern = /^\d{4}-\d{4}$/;
  if (!academicYearPattern.test(academicYear)) {
    showError('fg-acyear', 'err-acyear');
    isValid = false;
  } else {
    clearError('fg-acyear');
  }

  if (!isValid) {
    const firstError = document.querySelector('.form-group-fancy.is-error');
    if (firstError) firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  return isValid;
}

function createStudent() {
  const successAlert = document.getElementById('student-success-alert');
  if (successAlert) successAlert.classList.add('d-none');

  if (!validateStudentForm()) {
    const button = document.getElementById('create-student-btn');
    button.classList.add('btn-shake');
    setTimeout(() => button.classList.remove('btn-shake'), 500);
    return;
  }

  const button = document.getElementById('create-student-btn');
  const studentName = document.getElementById('student-fullname').value.trim();

  window.CursusFormHelpers?.setButtonLoading(button, 'Creating…');

  setTimeout(() => {
    console.log('Mock student create payload', {
      fullName: studentName,
      email: document.getElementById('student-email').value.trim(),
      password: document.getElementById('student-password').value,
      university: selectedUni,
      department: selectedDept,
      academicYear: document.getElementById('student-acyear').value.trim(),
      semester: selectedSem,
      yearLevel: selectedYear
    });

    if (successAlert) {
      successAlert.textContent = `${studentName} was added successfully.`;
      successAlert.classList.remove('d-none');
    }
    window.scrollTo({ top: 0, behavior: 'smooth' });

    window.CursusFormHelpers?.resetButton(button);
  }, 800);
}

function resetStudentForm() {
  ['student-fullname', 'student-email', 'student-password', 'student-acyear'].forEach((id) => {
    const el = document.getElementById(id);
    if (el) el.value = '';
  });

  selectedUni = null;
  selectedDept = null;
  selectedSem = 'spring';
  selectedYear = '1';

  document.getElementById('student-university').value = '';
  document.getElementById('stud-uni-value').textContent = 'Select university…';
  document.getElementById('stud-uni-value').style.color = 'var(--c-muted)';

  document.getElementById('stud-dept-value').textContent = 'Select university first…';
  document.getElementById('stud-dept-value').style.color = 'var(--c-muted)';
  document.getElementById('stud-dept-btn').disabled = true;
  document.getElementById('stud-dept-dropdown').innerHTML = '';

  document.querySelectorAll('.credit-btn').forEach((button) => button.classList.remove('credit-btn-selected'));
  document.querySelector('.credit-btn[data-val="1"]').classList.add('credit-btn-selected');
  document.getElementById('student-year-level').value = '1';

  document.getElementById('stud-uni-dropdown').querySelectorAll('.custom-dropdown-item')
    .forEach((item) => item.classList.remove('selected'));

  const button = document.getElementById('create-student-btn');
  window.CursusFormHelpers?.resetButton(button);

  const successAlert = document.getElementById('student-success-alert');
  if (successAlert) successAlert.classList.add('d-none');

  document.getElementById('success-state').classList.add('d-none');
  document.getElementById('form-view').classList.remove('d-none');
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function bindStudentFormEvents() {
  const form = document.getElementById('form-view');
  const createButton = document.getElementById('create-student-btn');
  const universitySelect = document.getElementById('student-university');

  if (createButton) {
    createButton.addEventListener('click', createStudent);
  }

  if (form) {
    form.addEventListener('submit', (event) => {
      event.preventDefault();
      createStudent();
    });
  }

  if (universitySelect) {
    universitySelect.addEventListener('change', () => {
      const hasUniversity = Boolean(universitySelect.value);
      const departmentBtn = document.getElementById('stud-dept-btn');
      departmentBtn.disabled = !hasUniversity;

      if (hasUniversity) {
        rebuildStudentDeptDropdown(universitySelect.value);
      } else {
        selectedDept = null;
        departmentBtn.disabled = true;
        document.getElementById('stud-dept-dropdown').innerHTML = '';
        document.getElementById('stud-dept-value').textContent = 'Select university first…';
        document.getElementById('stud-dept-value').style.color = 'var(--c-muted)';
      }
    });
  }

  if (window.CursusFormHelpers?.initJqueryValidation) {
    window.CursusFormHelpers.initJqueryValidation('#form-view', {
      'student-fullname': { required: true },
      'student-email': { required: true, email: true },
      'student-password': { required: true, minlength: 8 },
      'student-acyear': { required: true }
    });
  }

  ['student-fullname', 'student-email', 'student-password', 'student-acyear'].forEach((id) => {
    const field = document.getElementById(id);
    if (!field) return;

    field.addEventListener('input', () => markFieldDirty(field));
    field.addEventListener('blur', () => validateStudentForm());
  });
}

bindStudentFormEvents();