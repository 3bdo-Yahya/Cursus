const IMPACT_STORAGE_KEY = 'cursusImpactReport';

window.addEventListener('DOMContentLoaded', () => {
  const stored = sessionStorage.getItem(IMPACT_STORAGE_KEY);
  if (stored) {
    try {
      const report = JSON.parse(stored);
      document.getElementById('ia-idle').classList.add('d-none');
      document.getElementById('ia-report').classList.remove('d-none');
      loadReport(report);
      return;
    } catch {
      sessionStorage.removeItem(IMPACT_STORAGE_KEY);
    }
  }

  document.getElementById('btn-new-sim')?.addEventListener('click', () => { location.href = '/Student/CourseMap'; });
  document.getElementById('btn-new-sim-2')?.addEventListener('click', () => { location.href = '/Student/CourseMap'; });
});

function loadReport(report) {
  const src = report.src || {
    id: report.failedCourseCode,
    name: report.failedCourseName,
    credits: report.failedCourseCredits,
    avail: 'See schedule',
    type: 'Core',
  };
  const blocked = (report.blockedCourses || []).map(b => ({
    id: b.code,
    name: b.name,
    credits: b.creditHours,
    depth: b.depth,
    avail: '',
  }));

  const delay = report.graduationDelaySemesters ?? 1;
  const semAff = report.semestersAffected ?? delay;
  const severity = (report.severity || 'Low').toUpperCase();
  const projectedGrad = report.projectedGraduationLabel || ('Fall 20' + (27 + (delay - 1)));
  const retakeLabel = report.retakeSemesterLabel || ('Next ' + (src.avail || 'term'));
  const creditsAtRisk = report.creditsAtRisk ?? blocked.reduce((s, b) => s + b.credits, 0);

  document.getElementById('report-severity').textContent = severity;
  document.getElementById('report-severity').className = 'ia-severity-badge ia-sev-' + severity.toLowerCase();
  document.getElementById('report-subtitle').textContent = 'Simulating failure of ' + src.id + ' — ' + src.name;

  animCount('kpi-blocked', report.blockedCoursesCount ?? blocked.length);
  animCount('kpi-semesters', semAff);
  document.getElementById('kpi-delay').textContent    = '+' + delay + ' sem';
  document.getElementById('kpi-new-grad').textContent = projectedGrad;

  document.getElementById('fc-code').textContent    = src.id;
  document.getElementById('fc-name').textContent    = src.name;
  document.getElementById('fc-credits').textContent = src.credits + ' credit hours';
  document.getElementById('fc-avail').textContent   = src.avail || retakeLabel;
  document.getElementById('fc-type').textContent    = src.type || 'Core';

  const listEl = document.getElementById('blocked-list');
  listEl.innerHTML = '';
  blocked.forEach((b, i) => {
    const isDirect = (b.depth ?? 1) === 1;
    const row = document.createElement('div');
    row.className = 'ia-blocked-row';
    row.style.animationDelay = (i * 70) + 'ms';
    row.innerHTML = `
      <div class="ia-blocked-num">${i+1}</div>
      <div class="flex-fill min-w-0">
        <p class="fw-700 mb-0" style="font-size:13px;color:var(--c-text);">${b.id}
          <span style="font-weight:500;color:var(--c-text-sub);">— ${b.name}</span>
        </p>
        <p style="font-size:11px;color:var(--c-muted);margin:2px 0 0;">${isDirect ? 'Direct dependency' : 'Chain dependency'} · ${b.credits} cr${b.avail ? ' · ' + b.avail : ''}</p>
      </div>
      <span class="ia-dep-tag ${isDirect ? 'ia-dep-direct' : 'ia-dep-chain'}">${isDirect ? 'Direct' : 'Chain'}</span>
    `;
    listEl.appendChild(row);
  });
  document.getElementById('blocked-count-badge').textContent = blocked.length + ' courses';

  document.getElementById('risk-avail').textContent    = retakeLabel;
  document.getElementById('risk-credits').textContent  = creditsAtRisk + ' cr';
  document.getElementById('risk-cgpa').textContent     = '−0.' + (delay * 18);
  document.getElementById('risk-standing').textContent = blocked.length > 4 ? 'Warning Risk' : 'Good Standing';
  document.getElementById('risk-standing').style.color = blocked.length > 4 ? '#d97706' : '#10b981';

  const tlEl = document.getElementById('ia-timeline');
  tlEl.innerHTML = '';
  const steps = [
    { color:'#ef4444', icon:'bolt',       sem:'Now',          label:`<strong>${src.id}</strong> failure cascades — ${blocked.length} courses blocked`, type:'fail'     },
    { color:'#d97706', icon:'refresh',    sem:retakeLabel,    label:`Retake <strong>${src.id}</strong> · ${retakeLabel}`, type:'retake'   },
    ...blocked.slice(0, 2).map((b, i) => ({
      color: '#10b981',
      icon:  'lock_open',
      sem:   i === 0 ? 'After retake' : 'Following term',
      label: `<strong>${b.id}</strong> — ${b.name} unlocks`,
      type:  'unlock',
    })),
    { color:'var(--c-primary)', icon:'school', sem:'New Graduation', label:`<strong>${projectedGrad}</strong> — delayed ${delay} semester${delay > 1 ? 's' : ''}`, type:'grad' },
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
    { icon:'priority_high', color:'#ef4444', text:`Prioritize <strong>${src.id}</strong> — register for ${retakeLabel} immediately.` },
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

  document.getElementById('btn-new-sim')?.addEventListener('click', () => { location.href = '/Student/CourseMap'; });
  document.getElementById('btn-new-sim-2')?.addEventListener('click', () => { location.href = '/Student/CourseMap'; });
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
