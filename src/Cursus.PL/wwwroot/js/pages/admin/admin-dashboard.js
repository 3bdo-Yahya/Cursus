/* ── Notify Dept button feedback ────────────────────────── */
document.querySelectorAll('.notify-btn').forEach(btn => {
  btn.addEventListener('click', function () {
    const original = this.innerHTML;
    this.innerHTML = '<span class="material-symbols-outlined" style="font-size:14px">check_circle</span> Sent';
    this.style.color = '#059669';
    this.disabled = true;
    setTimeout(() => {
      this.innerHTML = original;
      this.style.color = '';
      this.disabled = false;
    }, 3000);
  });
});


const DASH_UNI_DATA = {
  svu: {
    name: 'South Valley University',
    depts: [
      { value:'cs', label:'Computer Science',       sub:'66 courses · 156 students', icon:'computer',   color:'cs', students:156, courses:66,  gpa:'2.91', alerts:2 },
      { value:'is', label:'Information Systems',    sub:'63 courses · 118 students', icon:'storage',    color:'is', students:118, courses:63,  gpa:'2.87', alerts:1 },
      { value:'ai', label:'Artificial Intelligence',sub:'61 courses · 98 students',  icon:'psychology', color:'ai', students:98,  courses:61,  gpa:'3.05', alerts:1 },
      { value:'it', label:'Information Technology', sub:'68 courses · 118 students', icon:'devices',    color:'it', students:118, courses:68,  gpa:'2.78', alerts:1 },
    ]
  },
  auc: {
    name: 'American University in Cairo',
    depts: [
      { value:'cs', label:'Computer Science', sub:'414 courses · 312 students', icon:'computer', color:'cs', students:312, courses:414, gpa:'3.08', alerts:2 },
    ]
  },
  su: {
    name: 'Sinai University',
    depts: [
      { value:'it',   label:'Information Technology',        sub:'102 courses · 74 students', icon:'devices',    color:'it', students:74,  courses:102, gpa:'2.74', alerts:2 },
      { value:'csse', label:'Computer Science & Soft. Eng.', sub:'102 courses · 68 students', icon:'code',       color:'cs', students:68,  courses:102, gpa:'2.81', alerts:1 },
      { value:'idss', label:'Intelligent & Data Systems',    sub:'102 courses · 45 students', icon:'psychology', color:'ai', students:45,  courses:102, gpa:'2.95', alerts:0 },
    ]
  }
};

let dashCurrentUni  = 'svu';
let dashCurrentDept = 'cs';


/* ── Toggle dropdown ──────────────────────────────────── */
function toggleDropdown(id) {
  const dropdown = document.getElementById(`${id}-dropdown`);
  const btn      = document.getElementById(`${id}-btn`);
  if (!dropdown) return;

  const isOpen = dropdown.classList.contains('open');
  document.querySelectorAll('.custom-dropdown.open').forEach(d => d.classList.remove('open'));
  document.querySelectorAll('.custom-select-btn.open').forEach(b => b.classList.remove('open'));

  if (!isOpen) {
    dropdown.classList.add('open');
    if (btn) btn.classList.add('open');
  }
}

document.addEventListener('click', e => {
  if (!e.target.closest('.custom-select-wrap')) {
    document.querySelectorAll('.custom-dropdown.open').forEach(d => d.classList.remove('open'));
    document.querySelectorAll('.custom-select-btn.open').forEach(b => b.classList.remove('open'));
  }
});


/* ── Select university ─────────────────────────────────── */
function selectDashUni(item) {
  const value = item.dataset.value;
  dashCurrentUni = value;

  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item')
      .forEach(i => i.classList.remove('selected'));
  item.classList.add('selected');

  document.getElementById('dash-uni-value').textContent = item.querySelector('.custom-dropdown-label').textContent;

  // Rebuild dept dropdown for this uni
  rebuildDashDeptDropdown(value);

  document.getElementById('dash-uni-dropdown').classList.remove('open');
  document.getElementById('dash-uni-btn').classList.remove('open');
}

function rebuildDashDeptDropdown(uniValue) {
  const depts    = DASH_UNI_DATA[uniValue].depts;
  const dropdown = document.getElementById('dash-dept-dropdown');

  dropdown.innerHTML = depts.map((d, i) => `
    <div class="custom-dropdown-item ${i === 0 ? 'selected' : ''}"
         data-value="${d.value}" data-students="${d.students}" data-courses="${d.courses}"
         data-gpa="${d.gpa}" data-alerts="${d.alerts}"
         data-icon="${d.icon}" data-color="${d.color}" data-name="${d.label}"
         onclick="selectDashDept(this)">
      <span class="custom-dropdown-check material-symbols-outlined"
            style="font-size:15px;font-variation-settings:'FILL' 1,'wght' 400">check_circle</span>
      <div>
        <div class="custom-dropdown-label">${d.label}</div>
        <div class="custom-dropdown-sub">${d.sub}</div>
      </div>
      <span class="dept-dot dept-dot-${d.color}"></span>
    </div>
  `).join('');

  selectDashDept(dropdown.querySelector('.custom-dropdown-item'));
}

/* ── Select department ─────────────────────────────────── */
function selectDashDept(item) {
  dashCurrentDept = item.dataset.value;
  const label    = item.querySelector('.custom-dropdown-label').textContent;
  const icon     = item.dataset.icon;
  const color    = item.dataset.color;
  const students = item.dataset.students;
  const courses  = item.dataset.courses;
  const gpa      = item.dataset.gpa;
  const alerts   = item.dataset.alerts;


  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item')
      .forEach(i => i.classList.remove('selected'));
  item.classList.add('selected');


  const iconEl = document.getElementById('dash-dept-icon');
  if (iconEl) {
    iconEl.className = `custom-select-icon dept-icon-${color}`;
    iconEl.innerHTML = `<span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">${icon}</span>`;
  }
  document.getElementById('dash-dept-value').textContent = label + ` (${item.dataset.value.toUpperCase()})`;

  const deptName = document.getElementById('current-dept-name');
  if (deptName) deptName.textContent = label + ' Department';


  animateMetric('metric-courses',  courses);
  animateMetric('metric-students', students);
  animateMetric('metric-gpa',      gpa);
  animateMetric('metric-alerts',   alerts);

  document.getElementById('dash-dept-dropdown').classList.remove('open');
  document.getElementById('dash-dept-btn').classList.remove('open');
}

function animateMetric(id, newValue) {
  const el = document.getElementById(id);
  if (!el) return;
  el.classList.add('updating');
  setTimeout(() => {
    el.textContent = newValue;
    el.classList.remove('updating');
  }, 250);
}