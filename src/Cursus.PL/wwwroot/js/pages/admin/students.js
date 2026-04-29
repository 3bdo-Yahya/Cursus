/* ── Student page filter state ───────────────────────── */
const studentFilters = { standing: '', year: '', search: '' };

/* ── User menu toggle ────────────────────────────────── */
function toggleUserMenu() {
  const menu = document.getElementById('user-menu');
  const btn  = document.getElementById('user-btn');
  if (!menu) return;
  const isOpen = !menu.classList.contains('d-none');

  document.querySelectorAll('.custom-dropdown.open').forEach(d => d.classList.remove('open'));
  document.querySelectorAll('.custom-select-btn.open').forEach(b => b.classList.remove('open'));

  if (isOpen) {
    menu.classList.add('d-none');
  } else {
    menu.classList.remove('d-none');
  }
}

document.addEventListener('click', e => {
  const menu = document.getElementById('user-menu');
  if (menu && !e.target.closest('#user-btn') && !e.target.closest('#user-menu')) {
    menu.classList.add('d-none');
  }
});

/* ── University selector ─────────────────────────────── */
function selectUni(item) {
  
  const value = item.dataset.value;
  currentUni  = value;

  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item').forEach(i => i.classList.remove('selected'));
  item.classList.add('selected');

  document.getElementById('uni-value').textContent = UNI_DATA[value].name;

  rebuildDeptDropdown(value);

  updateHeaderSubtitle();

  document.getElementById('uni-dropdown').classList.remove('open');
  document.getElementById('uni-btn').classList.remove('open');
}

function rebuildDeptDropdown(uniValue) {
  const depts    = UNI_DATA[uniValue].depts;
  const dropdown = document.getElementById('dept-ctx-dropdown');

  dropdown.innerHTML = depts.map((d, i) => `
    <div class="custom-dropdown-item ${i === 0 ? 'selected' : ''}"
         data-value="${d.value}" data-icon="${d.icon}" data-color="${d.color}"
         onclick="selectDept(this)">
      <span class="custom-dropdown-check material-symbols-outlined"
            style="font-size:15px;font-variation-settings:'FILL' 1,'wght' 400">check_circle</span>
      <div>
        <div class="custom-dropdown-label">${d.label}</div>
        <div class="custom-dropdown-sub">${d.sub}</div>
      </div>
      <span class="dept-dot dept-dot-${d.color}"></span>
    </div>
  `).join('');


  currentDept = depts[0].value;
  const icon  = document.querySelector('.custom-select-icon');
  if (icon) {
    icon.className = `custom-select-icon dept-icon-${depts[0].color}`;
    icon.innerHTML = `<span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">${depts[0].icon}</span>`;
  }
  document.getElementById('dept-ctx-value').textContent = depts[0].label;
  document.getElementById('header-dept-name').textContent = depts[0].label;
  document.getElementById('ctx-course-count').textContent = depts[0].sub.split(' ')[0];
}


/* ── Department selector ─────────────────────────────── */
function selectDept(item) {
  currentDept = item.dataset.value;
  const label = item.querySelector('.custom-dropdown-label').textContent;
  const icon  = item.dataset.icon;
  const color = item.dataset.color;
  const sub   = item.querySelector('.custom-dropdown-sub')?.textContent || '';

  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item').forEach(i => i.classList.remove('selected'));
  item.classList.add('selected');

  const iconEl = document.querySelector('#dept-ctx-wrap .custom-select-icon');
  if (iconEl) {
    iconEl.className = `custom-select-icon dept-icon-${color}`;
    iconEl.innerHTML = `<span class="material-symbols-outlined" style="font-size:16px;font-variation-settings:'FILL' 0,'wght' 300">${icon}</span>`;
  }
  document.getElementById('dept-ctx-value').textContent = label;
  document.getElementById('header-dept-name').textContent = label;
  document.getElementById('ctx-course-count').textContent = sub.split(' ')[0];

  updateHeaderSubtitle();

  document.getElementById('dept-ctx-dropdown').classList.remove('open');
  document.getElementById('dept-ctx-btn').classList.remove('open');

  filters.dept = currentDept;
  applyFilters();
}

function updateHeaderSubtitle() {
  const uni  = UNI_DATA[currentUni];
  const depts = uni.depts;
  const dept  = depts.find(d => d.value === currentDept) || depts[0];

  document.getElementById('header-dept-name').textContent = dept.label;
  document.getElementById('header-uni-name').textContent = uni.name;
}



/* ── Student table filtering ─────────────────────────── */
function applyStudentFilters() {
  const rows   = document.querySelectorAll('#students-tbody .course-row');
  const search = studentFilters.search.toLowerCase();
  let visible  = 0;

  rows.forEach(row => {
    const name     = (row.dataset.name     || '').toLowerCase();
    const year     = (row.dataset.year     || '');
    const standing = (row.dataset.standing || '').toLowerCase();

    const matchSearch   = !search                  || name.includes(search);
    const matchStanding = !studentFilters.standing || standing === studentFilters.standing;
    const matchYear     = !studentFilters.year     || year === studentFilters.year;

    const show = matchSearch && matchStanding && matchYear;
    row.style.display = show ? '' : 'none';
    if (show) visible++;
  });

  document.getElementById('empty-row').style.display = visible === 0 ? '' : 'none';
}


const _origSelectFilter = window.selectFilter;
window.selectFilter = function(filterKey, value, label, item) {
  if (['standing','year'].includes(filterKey)) {
    studentFilters[filterKey] = value;

    item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item')
        .forEach(i => i.classList.remove('selected'));
    item.classList.add('selected');

    document.getElementById(`${filterKey}-filter-value`).textContent = label;
    document.getElementById(`${filterKey}-filter-dropdown`).classList.remove('open');
    document.getElementById(`${filterKey}-filter-btn`).classList.remove('open');

    updateStudentClearBtn();
    applyStudentFilters();
  } else {
    _origSelectFilter(filterKey, value, label, item);
  }
};

window.clearAllFilters = function() {
  studentFilters.standing = studentFilters.year = studentFilters.search = '';
  document.getElementById('search-input').value = '';

  ['standing-filter','year-filter'].forEach(id => {
    const dropdown = document.getElementById(`${id}-dropdown`);
    if (!dropdown) return;
    dropdown.querySelectorAll('.custom-dropdown-item').forEach((item, i) => {
      item.classList.toggle('selected', i === 0);
    });
    const valueEl     = document.getElementById(`${id}-value`);
    const firstLabel  = dropdown.querySelector('.custom-dropdown-label');
    if (valueEl && firstLabel) valueEl.textContent = firstLabel.textContent;
  });

  updateStudentClearBtn();
  applyStudentFilters();
};

function updateStudentClearBtn() {
  const hasFilter = studentFilters.standing || studentFilters.year || studentFilters.search;
  const btn = document.getElementById('clear-filters-btn');
  if (btn) btn.style.display = hasFilter ? 'flex' : 'none';
}

document.getElementById('search-input').addEventListener('input', e => {
  studentFilters.search = e.target.value;
  updateStudentClearBtn();
  applyStudentFilters();
});


/* ── Sort ────────────────────────────────────────────── */
document.querySelectorAll('.sort-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    const col    = btn.dataset.col;
    const isAsc  = btn.classList.contains('asc');
    const newDir = isAsc ? 'desc' : 'asc';

    document.querySelectorAll('.sort-btn').forEach(b => b.classList.remove('active','asc','desc'));
    btn.classList.add('active', newDir);

    const tbody = document.getElementById('students-tbody');
    const rows  = Array.from(tbody.querySelectorAll('.course-row'));

    rows.sort((a, b) => {
      let av = (a.dataset[col] || '').toLowerCase();
      let bv = (b.dataset[col] || '').toLowerCase();
      if (col === 'gpa' || col === 'year') { av = parseFloat(av)||0; bv = parseFloat(bv)||0; }
      if (av < bv) return newDir === 'asc' ? -1 :  1;
      if (av > bv) return newDir === 'asc' ?  1 : -1;
      return 0;
    });

    rows.forEach(r => tbody.appendChild(r));
  });
});
