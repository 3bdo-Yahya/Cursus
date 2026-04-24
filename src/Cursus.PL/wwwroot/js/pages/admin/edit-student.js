const STUDENT_EDIT_UNI_DEPTS = {
  svu: [
    { value: 'cs', label: 'Computer Science', icon: 'computer', color: 'cs' },
    { value: 'is', label: 'Information Systems', icon: 'storage', color: 'is' },
    { value: 'ai', label: 'Artificial Intelligence', icon: 'psychology', color: 'ai' },
    { value: 'it', label: 'Information Technology', icon: 'devices', color: 'it' }
  ],
  auc: [{ value: 'cs', label: 'Computer Science', icon: 'computer', color: 'cs' }],
  su: [
    { value: 'it', label: 'Information Technology', icon: 'devices', color: 'it' },
    { value: 'csse', label: 'Computer Science & Software Eng.', icon: 'code', color: 'cs' },
    { value: 'idss', label: 'Intelligent & Data Systems', icon: 'psychology', color: 'ai' }
  ]
};

let selectedUni = 'svu';
let selectedDept = 'cs';

function togglePassword() {
  const input = document.getElementById('student-password');
  const icon = document.getElementById('pass-toggle-icon');
  const isMasked = input.type === 'password';
  input.type = isMasked ? 'text' : 'password';
  icon.textContent = isMasked ? 'visibility_off' : 'visibility';
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
  group.querySelectorAll('.field-error').forEach((errorNode) => { errorNode.style.display = 'none'; });
}

function rebuildStudentDeptDropdown(uniValue) {
  const departments = STUDENT_EDIT_UNI_DEPTS[uniValue] || [];
  const dropdown = document.getElementById('stud-dept-dropdown');
  dropdown.innerHTML = departments.map((department) => `
    <div class="custom-dropdown-item ${department.value === selectedDept ? 'selected' : ''}"
         data-value="${department.value}" data-icon="${department.icon}" data-color="${department.color}"
         onclick="selectStudentDept(this)">
      <span class="custom-dropdown-check material-symbols-outlined" style="font-size:15px;">check_circle</span>
      <div><div class="custom-dropdown-label">${department.label}</div></div>
      <span class="dept-dot dept-dot-${department.color}"></span>
    </div>
  `).join('');
}

function selectStudentUni(item) {
  selectedUni = item.dataset.value;
  document.getElementById('student-university').value = selectedUni;
  document.getElementById('student-university').dispatchEvent(new Event('change', { bubbles: true }));

  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item').forEach((node) => node.classList.remove('selected'));
  item.classList.add('selected');
  document.getElementById('stud-uni-value').textContent = item.querySelector('.custom-dropdown-label').textContent;

  document.getElementById('stud-uni-dropdown').classList.remove('open');
  document.getElementById('stud-uni-btn').classList.remove('open');
  clearError('fg-university');
}

function selectStudentDept(item) {
  selectedDept = item.dataset.value;

  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item').forEach((node) => node.classList.remove('selected'));
  item.classList.add('selected');

  document.getElementById('stud-dept-value').textContent = item.querySelector('.custom-dropdown-label').textContent;
  const iconEl = document.getElementById('stud-dept-icon-el');
  iconEl.className = `custom-select-icon dept-icon-${item.dataset.color}`;
  iconEl.innerHTML = `<span class="material-symbols-outlined" style="font-size:16px;">${item.dataset.icon}</span>`;

  document.getElementById('stud-dept-dropdown').classList.remove('open');
  document.getElementById('stud-dept-btn').classList.remove('open');
  clearError('fg-department');
}

function validateEditStudentForm() {
  let isValid = true;

  if (!document.getElementById('student-fullname').value.trim()) {
    showError('fg-fullname', 'err-fullname');
    isValid = false;
  } else {
    clearError('fg-fullname');
  }

  const email = document.getElementById('student-email').value.trim();
  if (!email || !email.includes('@')) {
    showError('fg-email', 'err-email');
    isValid = false;
  } else {
    clearError('fg-email');
  }

  if (document.getElementById('student-password').value.length < 8) {
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

  if (!document.getElementById('student-acyear').value.trim()) {
    showError('fg-acyear', 'err-acyear');
    isValid = false;
  } else {
    clearError('fg-acyear');
  }

  return isValid;
}

function updateStudent() {
  const feedback = document.getElementById('update-feedback');
  if (feedback) feedback.classList.add('d-none');

  if (!validateEditStudentForm()) return;

  const button = document.getElementById('update-student-btn');
  const studentName = document.getElementById('student-fullname').value.trim();
  window.CursusFormHelpers?.setButtonLoading(button, 'Updating…');

  setTimeout(() => {
    console.log('Mock student update payload', {
      fullName: document.getElementById('student-fullname').value.trim(),
      email: document.getElementById('student-email').value.trim(),
      password: document.getElementById('student-password').value,
      university: selectedUni,
      department: selectedDept,
      academicYear: document.getElementById('student-acyear').value.trim()
    });

    if (feedback) {
      feedback.textContent = 'Student profile updated successfully.';
      feedback.classList.remove('d-none');
    }

    window.CursusFormHelpers?.resetButton(button, '<span class="material-symbols-outlined" style="font-size:18px;">save</span>Update Student');
  }, 700);
}

function resetEditStudentForm() {
  document.getElementById('update-success-state').classList.add('d-none');
  document.getElementById('edit-student-form').classList.remove('d-none');
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function initEditStudentPage() {
  rebuildStudentDeptDropdown(selectedUni);

  document.getElementById('student-university').addEventListener('change', (event) => {
    const departmentBtn = document.getElementById('stud-dept-btn');
    departmentBtn.disabled = !event.target.value;

    if (event.target.value) {
      if (!STUDENT_EDIT_UNI_DEPTS[event.target.value].find((item) => item.value === selectedDept)) {
        selectedDept = STUDENT_EDIT_UNI_DEPTS[event.target.value][0].value;
      }
      rebuildStudentDeptDropdown(event.target.value);
      const selectedDeptItem = document.querySelector(`#stud-dept-dropdown .custom-dropdown-item[data-value="${selectedDept}"]`);
      if (selectedDeptItem) {
        selectStudentDept(selectedDeptItem);
      }
    }
  });

  document.getElementById('update-student-btn').addEventListener('click', updateStudent);

  if (window.CursusFormHelpers?.initJqueryValidation) {
    window.CursusFormHelpers.initJqueryValidation('#edit-student-form', {
      'student-fullname': { required: true },
      'student-email': { required: true, email: true },
      'student-password': { required: true, minlength: 8 },
      'student-acyear': { required: true }
    });
  }
}

initEditStudentPage();