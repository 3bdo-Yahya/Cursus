const IMPACT_STORAGE_KEY = 'cursusImpactReport';

window.addEventListener('DOMContentLoaded', () => {
  document.getElementById('btn-new-sim')?.addEventListener('click', () => { location.href = '/Student/CourseMap'; });
  document.getElementById('btn-new-sim-2')?.addEventListener('click', () => { location.href = '/Student/CourseMap'; });

  const stored = sessionStorage.getItem(IMPACT_STORAGE_KEY);
  if (stored) {
    try {
      showReport(JSON.parse(stored));
      return;
    } catch {
      sessionStorage.removeItem(IMPACT_STORAGE_KEY);
    }
  }

  const courseId = new URLSearchParams(window.location.search).get('courseId');
  if (courseId) {
    const parsedId = parseInt(courseId, 10);
    if (!isNaN(parsedId)) {
      fetchAndShowReport(parsedId);
    }
  }
});

function showReport(report) {
  document.getElementById('ia-idle').classList.add('d-none');
  document.getElementById('ia-report').classList.remove('d-none');
  loadReport(report);
}

function getAntiForgeryToken() {
  const input = document.querySelector('input[name="__RequestVerificationToken"]');
  return input ? input.value : '';
}

async function fetchAndShowReport(courseId) {
  try {
    const res = await fetch('/Student/SimulateFailure', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'RequestVerificationToken': getAntiForgeryToken()
      },
      body: JSON.stringify({ courseId }),
    });
    if (!res.ok) {
      showFetchError('Unable to load the simulation report. Please run a simulation from the Course Map.');
      return;
    }
    const report = await res.json();
    sessionStorage.setItem(IMPACT_STORAGE_KEY, JSON.stringify(report));
    showReport(report);
  } catch {
    showFetchError('A network error occurred. Please check your connection and try again.');
  }
}

function showFetchError(message) {
  const idle = document.getElementById('ia-idle');
  if (idle) {
    const notice = idle.querySelector('.ia-error-notice') || document.createElement('p');
    notice.className = 'ia-error-notice';
    notice.style.cssText = 'margin-top:12px;font-size:13px;color:#b91c1c;';
    notice.textContent = message;
    idle.appendChild(notice);
  }
}

function formatStanding(standing) {
  const map = { 0: 'Good Standing', 1: 'Academic Warning', 2: 'Probation' };
  return map[standing] ?? String(standing);
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text ?? '';
  return div.innerHTML;
}

function formatCourseLabel(course, { retake = false, unlocked = false } = {}) {
  const code = course.code || '';
  const name = course.name || code;
  const primary = name && name !== code ? name : code;
  const safePrimary = escapeHtml(primary);
  const safeCode = escapeHtml(code);
  const suffix = code && name && name !== code
    ? ` <span style="color:var(--c-muted);font-weight:500;">(${safeCode})</span>`
    : '';
  if (retake) return `<strong>${safePrimary}</strong>${suffix} <span style="color:#d97706;">(retake)</span>`;
  if (unlocked) return `<strong>${safePrimary}</strong>${suffix} <span style="color:#10b981;">unlocks</span>`;
  return `<strong>${safePrimary}</strong>${suffix}`;
}

function loadReport(report) {
  const src = report.src || {
    id: report.failedCourseCode,
    name: report.failedCourseName,
    credits: report.failedCourseCredits,
    avail: report.retakeSemesterLabel || 'See schedule',
    type: 'Core',
  };
  const blocked = (report.blockedCourses || []).map(b => ({
    id: b.code,
    name: b.name,
    credits: b.creditHours,
    depth: b.depth,
    avail: b.newTermLabel ? `Delayed to ${b.newTermLabel}` : '',
  }));

  const delay = report.graduationDelaySemesters ?? 0;
  const semAff = report.semestersAffected ?? delay;
  const severity = (report.severity || 'Low').toUpperCase();
  const originalGrad = report.originalGraduationLabel || 'On track';
  const projectedGrad = report.projectedGraduationLabel || originalGrad;
  const retakeLabel = report.retakeSemesterLabel || 'Next Summer';
  const creditsAtRisk = report.creditsAtRisk ?? blocked.reduce((s, b) => s + b.credits, 0);
  const recoverySchedule = report.recoverySchedule || [];
  const recommendations = report.recommendations || [];
  const projectedCgpa = report.projectedCgpa ?? report.currentCgpa;
  const cgpaDelta = report.cgpaDelta ?? 0;
  const standingWouldChange = report.standingWouldChange === true;
  const projectedStanding = formatStanding(report.projectedStanding);

  document.getElementById('report-severity').textContent = severity;
  document.getElementById('report-severity').className = 'ia-severity-badge ia-sev-' + severity.toLowerCase();
  document.getElementById('report-subtitle').textContent =
    `Simulating failure of ${src.name || src.id}${src.id ? ` (${src.id})` : ''}`;

  const scenarioEl = document.getElementById('ia-scenario-callout');
  const scenarioText = document.getElementById('ia-scenario-text');
  if (report.scenarioSummary && scenarioEl && scenarioText) {
    scenarioText.textContent = report.scenarioSummary;
    scenarioEl.classList.remove('d-none');
  } else if (scenarioEl) {
    scenarioEl.classList.add('d-none');
  }

  animCount('kpi-blocked', report.blockedCoursesCount ?? blocked.length);
  animCount('kpi-semesters', semAff);

  const delayEl = document.getElementById('kpi-delay');
  delayEl.textContent = delay > 0 ? `+${delay} sem` : 'None';
  delayEl.style.color = delay > 0 ? '#ef4444' : '#10b981';

  const gradEl = document.getElementById('kpi-new-grad');
  const gradDetail = document.getElementById('kpi-grad-detail');
  if (delay > 0) {
    gradEl.textContent = projectedGrad;
    if (gradDetail) gradDetail.textContent = `Was ${originalGrad}`;
  } else {
    gradEl.textContent = projectedGrad;
    if (gradDetail) gradDetail.textContent = 'On track — no graduation delay';
  }

  const cgpaKpi = document.getElementById('kpi-cgpa');
  if (cgpaKpi) {
    cgpaKpi.textContent = cgpaDelta < 0
      ? `${projectedCgpa.toFixed(2)} (${cgpaDelta.toFixed(2)})`
      : projectedCgpa.toFixed(2);
    cgpaKpi.style.color = cgpaDelta < 0 ? '#d97706' : '#10b981';
  }

  const standingKpi = document.getElementById('kpi-standing');
  if (standingKpi) {
    standingKpi.textContent = standingWouldChange ? `${projectedStanding} (at risk)` : projectedStanding;
    standingKpi.style.color = standingWouldChange ? '#d97706' : '#10b981';
  }

  document.getElementById('fc-code').textContent    = src.id;
  document.getElementById('fc-name').textContent    = src.name;
  document.getElementById('fc-credits').textContent = src.credits + ' credit hours';
  document.getElementById('fc-avail').textContent   = retakeLabel;
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

  const tlEl = document.getElementById('ia-timeline');
  tlEl.innerHTML = '';

  const steps = [
    {
      color: '#ef4444',
      icon: 'bolt',
      sem: 'Now',
      label: `<strong>${src.name || src.id}</strong>${src.name && src.id ? ` <span style="color:var(--c-muted);font-weight:500;">(${src.id})</span>` : ''} failure — ${blocked.length} course${blocked.length === 1 ? '' : 's'} blocked`,
      type: 'fail'
    }
  ];

  if (recoverySchedule.length > 0) {
    recoverySchedule.forEach((term) => {
      const visible = (term.courses || []).slice(0, 5);
      const overflow = (term.courses || []).length - visible.length;
      const courseLabels = visible.map(c => formatCourseLabel(c, {
        retake: c.isRetake,
        unlocked: c.isNewlyUnlocked
      })).join(', ');

      steps.push({
        color: term.isRetakeTerm ? '#d97706' : '#10b981',
        icon: term.isRetakeTerm ? 'refresh' : 'calendar_month',
        sem: term.label,
        label: courseLabels
          ? courseLabels + (overflow > 0 ? `, <span style="color:var(--c-muted);">+${overflow} more</span>` : '')
          : 'Continue planned courses',
        type: term.isRetakeTerm ? 'retake' : 'unlock'
      });
    });
  } else {
    steps.push({
      color: '#d97706',
      icon: 'refresh',
      sem: retakeLabel,
      label: `Retake ${formatCourseLabel({ code: src.id, name: src.name })}`,
      type: 'retake'
    });
  }

  steps.push({
    color: 'var(--c-primary)',
    icon: 'school',
    sem: 'Graduation',
    label: delay > 0
      ? `<strong>${originalGrad}</strong> → <strong>${projectedGrad}</strong>`
      : `<strong>${projectedGrad}</strong> on schedule`,
    type: 'grad'
  });

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
  recEl.innerHTML = '';
  const recList = recommendations.length > 0 ? recommendations : [
    `Prioritize <strong>${src.name || src.id}</strong> — register for ${retakeLabel}.`,
    `Speak to your advisor about the ${delay}-semester graduation impact.`
  ];

  recList.forEach(r => {
    const el = document.createElement('div');
    el.className = 'ia-rec-item';
    el.innerHTML = `
      <span class="material-symbols-outlined flex-shrink-0" style="font-size:17px;color:var(--c-primary);font-variation-settings:'FILL' 1,'wght' 400">tips_and_updates</span>
      <span style="font-size:12.5px;color:var(--c-text-sub);line-height:1.55;">${r}</span>
    `;
    recEl.appendChild(el);
  });
}

function animCount(id, target) {
  const el = document.getElementById(id);
  let current = 0;
  const step = Math.ceil(target / 12) || 1;
  const interval = setInterval(() => {
    current = Math.min(current + step, target);
    el.textContent = current;
    if (current >= target) clearInterval(interval);
  }, 45);
}



