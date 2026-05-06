const filters = { type: '', avail: '', status: '', search: '' };

/* ── University data map ─────────────────────────────── */
const UNI_DATA = {
  svu: {
    name: 'South Valley University',
    depts: [
      { value: 'cs', label: 'Computer Science',      sub: '66 courses',  icon: 'computer',   color: 'cs' },
      { value: 'is', label: 'Information Systems',   sub: '63 courses',  icon: 'storage',    color: 'is' },
      { value: 'ai', label: 'Artificial Intelligence',sub: '61 courses',  icon: 'psychology', color: 'ai' },
      { value: 'it', label: 'Information Technology', sub: '68 courses',  icon: 'devices',    color: 'it' },
    ]
  },
  auc: {
    name: 'American University in Cairo',
    depts: [
      { value: 'cs', label: 'Computer Science', sub: '414 courses', icon: 'computer', color: 'cs' },
    ]
  },
  su: {
    name: 'Sinai University',
    depts: [
      { value: 'it',   label: 'Information Technology',      sub: 'IT track',   icon: 'devices',    color: 'it'   },
      { value: 'csse', label: 'Computer Science & Soft. Eng.',sub: 'CSSE track', icon: 'code',       color: 'cs'   },
      { value: 'idss', label: 'Intelligent & Data Systems',   sub: 'IDSS track', icon: 'psychology', color: 'ai'  },
    ]
  },
};

let currentUni  = 'svu';
let currentDept = 'cs';


/* Dropdown: CursusUI.toggleDropdown in site.js */

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


/* ── Filter dropdowns ────────────────────────────────── */
function selectFilter(filterKey, value, label, item) {
  filters[filterKey] = value;

  item.closest('.custom-dropdown').querySelectorAll('.custom-dropdown-item').forEach(i => i.classList.remove('selected'));
  item.classList.add('selected');

  document.getElementById(`${filterKey}-filter-value`).textContent = label;

  document.getElementById(`${filterKey}-filter-dropdown`).classList.remove('open');
  document.getElementById(`${filterKey}-filter-btn`).classList.remove('open');

  updateClearBtn();
  applyFilters();
}

function updateClearBtn() {
  const hasFilter = filters.type || filters.avail || filters.status || filters.search;
  const btn = document.getElementById('clear-filters-btn');
  if (btn) btn.style.display = hasFilter ? 'flex' : 'none';
}

function clearAllFilters() {
  filters.type = filters.avail = filters.status = filters.search = '';
  document.getElementById('search-input').value = '';

  ['type-filter','avail-filter','status-filter'].forEach(id => {
    const dropdown = document.getElementById(`${id}-dropdown`);
    if (!dropdown) return;
    dropdown.querySelectorAll('.custom-dropdown-item').forEach((item, i) => {
      item.classList.toggle('selected', i === 0);
    });
    const valueEl = document.getElementById(`${id}-value`);
    const firstLabel = dropdown.querySelector('.custom-dropdown-label');
    if (valueEl && firstLabel) valueEl.textContent = firstLabel.textContent;
  });

  updateClearBtn();
  applyFilters();
}


/* ── Table filtering ─────────────────────────────────── */
function applyFilters() {
  const rows    = document.querySelectorAll('#courses-tbody .course-row');
  const search  = filters.search.toLowerCase();
  let   visible = 0;

  rows.forEach(row => {
    const code   = (row.dataset.code   || '').toLowerCase();
    const name   = (row.dataset.name   || '').toLowerCase();
    const type   = (row.dataset.type   || '').toLowerCase();
    const avail  = (row.dataset.avail  || '').toLowerCase();
    const status = (row.dataset.status || '').toLowerCase();
    const dept   = (row.dataset.dept   || '').toLowerCase();

    const matchSearch = !search  || code.includes(search) || name.includes(search);
    const matchType   = !filters.type   || type === filters.type;
    const matchAvail  = !filters.avail  || avail === filters.avail;
    const matchStatus = !filters.status || status === filters.status;

    const show = matchSearch && matchType && matchAvail && matchStatus;
    row.style.display = show ? '' : 'none';
    if (show) visible++;
  });

  document.getElementById('empty-row').style.display = visible === 0 ? '' : 'none';
}

document.getElementById('search-input').addEventListener('input', e => {
  filters.search = e.target.value;
  updateClearBtn();
  applyFilters();
});


/* ── Sort ────────────────────────────────────────────── */
document.querySelectorAll('.sort-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    const col     = btn.dataset.col;
    const isAsc   = btn.classList.contains('asc');
    const newDir  = isAsc ? 'desc' : 'asc';

    document.querySelectorAll('.sort-btn').forEach(b => b.classList.remove('active','asc','desc'));
    btn.classList.add('active', newDir);

    const tbody = document.getElementById('courses-tbody');
    const rows  = Array.from(tbody.querySelectorAll('.course-row'));

    rows.sort((a, b) => {
      let av = (a.dataset[col] || '').toLowerCase();
      let bv = (b.dataset[col] || '').toLowerCase();
      if (col === 'credits') { av = parseInt(av)||0; bv = parseInt(bv)||0; }
      if (av < bv) return newDir === 'asc' ? -1 :  1;
      if (av > bv) return newDir === 'asc' ?  1 : -1;
      return 0;
    });

    rows.forEach(r => tbody.appendChild(r));
  });
});