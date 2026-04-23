/* ── Demo ─────────────────────── */
const DEMO_SRC = { id:'CS311', name:'Algorithms Analysis & Design', credits:3, avail:'Fall', type:'Core', dept:'CS' };
const DEMO_BLOCKED = [
  { id:'AI301',  name:'Artificial Intelligence',      credits:3, prereqs:['CS311'], avail:'Fall'  },
  { id:'AI413',  name:'Machine Learning',             credits:3, prereqs:['CS311'], avail:'Fall'  },
  { id:'CS401',  name:'Natural Language Processing',  credits:3, prereqs:['CS311'], avail:'All'   },
  { id:'CS491',  name:'Senior Project I',             credits:3, prereqs:['CS391'], avail:'Fall & Spring' },
];

window.addEventListener('DOMContentLoaded', () => {
  const hasReport = true;
  if (hasReport) {
    document.getElementById('ia-idle').classList.add('d-none');
    document.getElementById('ia-report').classList.remove('d-none');
    loadReport(DEMO_SRC, DEMO_BLOCKED);
  }

  document.getElementById('btn-new-sim')?.addEventListener('click', () => location.href='course-map.html');
  document.getElementById('btn-new-sim-2')?.addEventListener('click', () => location.href='course-map.html');
});

function loadReport(src, blocked) {
  const delay = blocked.length > 3 ? 2 : 1;
  const semAff = Math.min(blocked.length + 1, 3);
  const severity = blocked.length >= 4 ? 'CRITICAL' : blocked.length >= 2 ? 'HIGH' : 'LOW';

  document.getElementById('report-severity').textContent = severity;
  document.getElementById('report-severity').className = 'ia-severity-badge ia-sev-' + severity.toLowerCase();
  document.getElementById('report-subtitle').textContent = 'Simulating failure of ' + src.id + ' — ' + src.name;

  animCount('kpi-blocked',   blocked.length);
  animCount('kpi-semesters', semAff);
  document.getElementById('kpi-delay').textContent    = '+' + delay + ' sem';
  document.getElementById('kpi-new-grad').textContent = 'Fall 20' + (27 + (delay-1));

  document.getElementById('fc-code').textContent    = src.id;
  document.getElementById('fc-name').textContent    = src.name;
  document.getElementById('fc-credits').textContent = src.credits + ' credit hours';
  document.getElementById('fc-avail').textContent   = src.avail;
  document.getElementById('fc-type').textContent    = src.type;

  const listEl = document.getElementById('blocked-list');
  listEl.innerHTML = '';
  blocked.forEach((b, i) => {
    const isDirect = b.prereqs.includes(src.id);
    const row = document.createElement('div');
    row.className = 'ia-blocked-row';
    row.style.animationDelay = (i * 70) + 'ms';
    row.innerHTML = `
      <div class="ia-blocked-num">${i+1}</div>
      <div class="flex-fill min-w-0">
        <p class="fw-700 mb-0" style="font-size:13px;color:var(--c-text);">${b.id}
          <span style="font-weight:500;color:var(--c-text-sub);">— ${b.name}</span>
        </p>
        <p style="font-size:11px;color:var(--c-muted);margin:2px 0 0;">${isDirect ? 'Direct dependency' : 'Chain dependency'} · ${b.credits} cr · ${b.avail}</p>
      </div>
      <span class="ia-dep-tag ${isDirect ? 'ia-dep-direct' : 'ia-dep-chain'}">${isDirect ? 'Direct' : 'Chain'}</span>
    `;
    listEl.appendChild(row);
  });
  document.getElementById('blocked-count-badge').textContent = blocked.length + ' courses';

  const totalBlocked = blocked.reduce((s,b) => s+b.credits, 0);
  document.getElementById('risk-avail').textContent    = 'Next ' + src.avail + ' 2026';
  document.getElementById('risk-credits').textContent  = totalBlocked + ' cr';
  document.getElementById('risk-cgpa').textContent     = '−0.' + (delay*18);
  document.getElementById('risk-standing').textContent = blocked.length > 4 ? 'Warning Risk' : 'Good Standing';
  document.getElementById('risk-standing').style.color = blocked.length > 4 ? '#d97706' : '#10b981';

  const tlEl = document.getElementById('ia-timeline');
  tlEl.innerHTML = '';
  const steps = [
    { color:'#ef4444', icon:'bolt',       sem:'Now',          label:`<strong>${src.id}</strong> failure cascades — ${blocked.length} courses blocked`, type:'fail'     },
    { color:'#d97706', icon:'refresh',    sem:'Fall 2026',    label:`Retake <strong>${src.id}</strong> · ${src.avail} offering`,                       type:'retake'   },
    ...blocked.slice(0,2).map((b,i) => ({
      color: '#10b981',
      icon:  'lock_open',
      sem:   i===0 ? 'Spring 2027' : 'Fall 2027',
      label: `<strong>${b.id}</strong> — ${b.name} unlocks`,
      type:  'unlock',
    })),
    { color:'var(--c-primary)', icon:'school', sem:'New Graduation', label:`<strong>Fall 20${27+(delay-1)}</strong> — delayed ${delay} semester${delay>1?'s':''}`, type:'grad' },
  ];
  steps.forEach((s, i) => {
    const el = document.createElement('div');
    el.className = 'ia-tl-item ia-tl-' + s.type;
    el.style.animationDelay = (i * 80) + 'ms';
    el.innerHTML = `
      <div class="ia-tl-marker">
        <div class="ia-tl-dot" style="background:${s.color};"></div>
        ${i < steps.length-1 ? '<div class="ia-tl-line"></div>' : ''}
      </div>
      <div class="ia-tl-content">
        <p class="ia-tl-sem">${s.sem}</p>
        <p class="ia-tl-action">${s.label}</p>
      </div>
    `;
    tlEl.appendChild(el);
  });

  const recEl = document.getElementById('ia-recommendations');
  const recs = [
    { icon:'priority_high', color:'#ef4444', text:`Prioritize <strong>${src.id}</strong> — register for Fall 2026 immediately.` },
    { icon:'school',         color:'#d97706', text:`Speak to your advisor about the ${delay}-semester delay impact.` },
    { icon:'auto_awesome',   color:'var(--c-primary)', text:`Use the GPA Simulator to see how the retake affects your CGPA.` },
  ];
  recEl.innerHTML = '';
  recs.forEach(r => {
    const el = document.createElement('div');
    el.className = 'ia-rec-item';
    el.innerHTML = `
      <span class="material-symbols-outlined flex-shrink-0" style="font-size:17px;color:${r.color};font-variation-settings:'FILL' 1,'wght' 400">${r.icon}</span>
      <span style="font-size:12.5px;color:var(--c-text-sub);line-height:1.55;">${r.text}</span>
    `;
    recEl.appendChild(el);
  });
}

function animCount(id, target) {
  const el = document.getElementById(id);
  let current = 0;
  const step = Math.ceil(target / 12);
  const interval = setInterval(() => {
    current = Math.min(current + step, target);
    el.textContent = current;
    if (current >= target) clearInterval(interval);
  }, 45);
}