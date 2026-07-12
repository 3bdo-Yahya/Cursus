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

/** Matches GraduationDelayCalculator.SafetyLimit — delay at/above this means path projection failed. */
const RECOVERY_SAFETY_LIMIT = 60;

/**
 * Derive badge + graduation copy from existing report fields.
 * - ontrack: delay === 0
 * - delayed: delay > 0 but still under safety limit
 * - uncertain: simulator hit safety limit / incomplete path
 */
function resolveRecoveryStatus({ delay, originalGrad, projectedGrad }) {
  if (delay >= RECOVERY_SAFETY_LIMIT) {
    return {
      key: 'uncertain',
      badgeLabel: 'Needs review',
      badgeIcon: 'warning',
      badgeClass: 'ia-status-badge ia-status-uncertain',
      graduationHtml: 'Unable to project a complete path — meet your advisor',
      hint: 'Simulation could not finish a full recovery path. Confirm next steps with your advisor.'
    };
  }

  if (delay > 0) {
    return {
      key: 'delayed',
      badgeLabel: 'Recoverable · delayed',
      badgeIcon: 'schedule',
      badgeClass: 'ia-status-badge ia-status-delayed',
      graduationHtml: `<strong>${escapeHtml(originalGrad)}</strong> → <strong>${escapeHtml(projectedGrad)}</strong> (+${delay} sem)`,
      hint: 'What to do next: retake, affected courses, and what you can take instead.'
    };
  }

  return {
    key: 'ontrack',
    badgeLabel: 'Recoverable',
    badgeIcon: 'check_circle',
    badgeClass: 'ia-status-badge ia-status-ontrack',
    graduationHtml: `<strong>${escapeHtml(projectedGrad)}</strong> — no graduation delay`,
    hint: 'What to do next: retake, affected courses, and what you can take instead.'
  };
}

function applyRecoveryBadge(status) {
  const badge = document.getElementById('ia-recovery-badge');
  const badgeText = document.getElementById('ia-recovery-badge-text');
  const hint = document.getElementById('ia-recovery-hint');
  if (badge) {
    badge.className = status.badgeClass;
    const icon = badge.querySelector('.material-symbols-outlined');
    if (icon) icon.textContent = status.badgeIcon;
  }
  if (badgeText) badgeText.textContent = status.badgeLabel;
  if (hint) hint.textContent = status.hint;
}

function renderLeanRecovery({ src, retakeLabel, blockedRaw, replacements, recoveryStatus }) {
  const leanEl = document.getElementById('ia-lean-recovery');
  if (!leanEl) return;

  const failedLabel = formatCourseLabel({ code: src.id, name: src.name });
  const directAffected = (blockedRaw || []).filter(b => (b.depth ?? 1) === 1);
  const affectedItems = directAffected.map(b => {
    const label = formatCourseLabel({ code: b.code, name: b.name });
    let timing = '';
    if (b.newTermLabel && b.normalTermLabel && b.newTermLabel !== b.normalTermLabel) {
      timing = ` <span class="ia-lean-course-meta">· was ${escapeHtml(b.normalTermLabel)}, unlocks ${escapeHtml(b.newTermLabel)}</span>`;
    } else if (b.newTermLabel) {
      timing = ` <span class="ia-lean-course-meta">· unlocks ${escapeHtml(b.newTermLabel)}</span>`;
    }
    return `<li>${label}${timing}</li>`;
  }).join('');

  const replacementItems = (replacements || []).map(c =>
    `<li>${formatCourseLabel(c)} <span class="ia-lean-course-meta">· ${c.creditHours ?? c.credits ?? 3} cr</span></li>`
  ).join('');

  leanEl.innerHTML = `
    <div class="ia-lean-block">
      <p class="ia-lean-block-title">
        <span class="material-symbols-outlined" style="font-size:14px;color:#d97706;font-variation-settings:'FILL' 1,'wght' 400">refresh</span>
        Do now
      </p>
      <p class="ia-lean-block-body">Retake ${failedLabel} in <strong>${escapeHtml(retakeLabel)}</strong> (assume pass).</p>
    </div>
    <div class="ia-lean-block">
      <p class="ia-lean-block-title">
        <span class="material-symbols-outlined" style="font-size:14px;color:#ef4444;font-variation-settings:'FILL' 1,'wght' 400">block</span>
        Directly affected
      </p>
      ${affectedItems
        ? `<ul class="ia-lean-course-list">${affectedItems}</ul>`
        : `<p class="ia-lean-empty">No direct dependents are blocked. See Blocked Courses for the full cascade.</p>`}
    </div>
    <div class="ia-lean-block">
      <p class="ia-lean-block-title">
        <span class="material-symbols-outlined" style="font-size:14px;color:#10b981;font-variation-settings:'FILL' 1,'wght' 400">swap_horiz</span>
        Take instead
      </p>
      ${replacementItems
        ? `<ul class="ia-lean-course-list">${replacementItems}</ul>`
        : `<p class="ia-lean-empty">No eligible replacements found while you wait for the retake.</p>`}
    </div>
    <div class="ia-lean-block">
      <p class="ia-lean-block-title">
        <span class="material-symbols-outlined" style="font-size:14px;color:var(--c-primary);font-variation-settings:'FILL' 1,'wght' 400">school</span>
        Graduation
      </p>
      <p class="ia-lean-block-body">${recoveryStatus.graduationHtml}</p>
    </div>
  `;
}

function renderFullSchedule({ recoverySchedule, recoveryStatus, src, retakeLabel, blockedCount, timelineId = 'ia-timeline', detailsId = 'ia-full-schedule' }) {
  const tlEl = document.getElementById(timelineId);
  const detailsEl = document.getElementById(detailsId);
  if (!tlEl) return;

  tlEl.innerHTML = '';

  if (!recoverySchedule.length) {
    if (detailsEl) detailsEl.classList.toggle('d-none', recoveryStatus.key !== 'uncertain');
    if (recoveryStatus.key === 'uncertain') {
      tlEl.innerHTML = `
        <div class="ia-tl-item ia-tl-retake" style="opacity:1;">
          <div class="ia-tl-marker"><div class="ia-tl-dot" style="background:#ef4444;"></div></div>
          <div class="ia-tl-content">
            <p class="ia-tl-sem">Projection</p>
            <p class="ia-tl-action">Complete recovery schedule unavailable — confirm course sequence with your advisor</p>
          </div>
        </div>`;
    }
    return;
  }

  if (detailsEl) detailsEl.classList.remove('d-none');

  const steps = [
    {
      color: '#ef4444',
      sem: 'Now',
      label: `<strong>${escapeHtml(src.name || src.id)}</strong>${src.name && src.id ? ` <span style="color:var(--c-muted);font-weight:500;">(${escapeHtml(src.id)})</span>` : ''} failure — ${blockedCount} course${blockedCount === 1 ? '' : 's'} blocked`,
      type: 'fail',
      collapsible: false
    }
  ];

  recoverySchedule.forEach((term) => {
    const termCourses = term.courses || [];
    const courseLabels = termCourses.map(c => formatCourseLabel(c, {
      retake: c.isRetake,
      unlocked: c.isNewlyUnlocked
    }));
    const count = termCourses.length;
    const summary = count === 0
      ? 'Continue planned courses'
      : `${count} course${count === 1 ? '' : 's'}`;

    steps.push({
      color: term.isRetakeTerm ? '#d97706' : '#10b981',
      sem: term.label,
      label: courseLabels.length
        ? `<details class="ia-term-courses"><summary class="ia-term-courses-summary">${summary}</summary><ul class="ia-term-courses-list">${courseLabels.map(l => `<li>${l}</li>`).join('')}</ul></details>`
        : summary,
      type: term.isRetakeTerm ? 'retake' : 'unlock',
      collapsible: true
    });
  });

  steps.push({
    color: recoveryStatus.key === 'uncertain' ? '#ef4444' : 'var(--c-primary)',
    sem: 'Graduation',
    label: recoveryStatus.graduationHtml,
    type: 'grad',
    collapsible: false
  });

  steps.forEach((s, i) => {
    const el = document.createElement('div');
    el.className = 'ia-tl-item ia-tl-' + s.type;
    el.style.animationDelay = (i * 80) + 'ms';
    el.innerHTML = `
      <div class="ia-tl-marker">
        <div class="ia-tl-dot" style="background:${s.color};"></div>
        ${i < steps.length - 1 ? '<div class="ia-tl-line"></div>' : ''}
      </div>
      <div class="ia-tl-content">
        <p class="ia-tl-sem">${escapeHtml(s.sem)}</p>
        <div class="ia-tl-action">${s.label}</div>
      </div>
    `;
    tlEl.appendChild(el);
  });
}

const PROJECTION_SWAP_MS = 180;
let projectionViews = null;
let activeProjectionView = 'actual';
let projectionSwapTimer = null;
let projectionToggleBound = false;

function getSwapTargets() {
  return [
    document.getElementById('ia-recovery-swap'),
    document.getElementById('kpi-delay'),
    document.getElementById('kpi-new-grad'),
    document.getElementById('kpi-grad-detail'),
    document.getElementById('kpi-semesters'),
    document.getElementById('risk-avail'),
    document.getElementById('fc-avail'),
  ].filter(Boolean);
}

function setProjectionToggle(view) {
  const toggle = document.getElementById('ia-projection-toggle');
  if (!toggle) return;
  toggle.querySelectorAll('.ia-projection-btn').forEach(btn => {
    const isActive = btn.dataset.view === view;
    btn.classList.toggle('is-active', isActive);
    btn.setAttribute('aria-pressed', isActive ? 'true' : 'false');
  });
}

function bindProjectionToggle() {
  if (projectionToggleBound) return;
  const toggle = document.getElementById('ia-projection-toggle');
  if (!toggle) return;
  toggle.addEventListener('click', (e) => {
    const btn = e.target.closest('.ia-projection-btn');
    if (!btn || !projectionViews) return;
    const view = btn.dataset.view;
    if (!view || view === activeProjectionView || !projectionViews[view]) return;
    swapProjectionView(view);
  });
  projectionToggleBound = true;
}

function applyProjectionView(view, { animate = false } = {}) {
  if (!projectionViews || !projectionViews[view]) return;

  const paint = () => {
    const v = projectionViews[view];
    activeProjectionView = view;
    setProjectionToggle(view);

    const summerNote = document.getElementById('ia-summer-note');
    if (summerNote) summerNote.classList.toggle('d-none', view !== 'summer');

    const delayEl = document.getElementById('kpi-delay');
    if (delayEl) {
      if (v.recoveryStatus.key === 'uncertain') {
        delayEl.textContent = 'Uncertain';
        delayEl.style.color = '#ef4444';
      } else {
        delayEl.textContent = v.delay > 0 ? `+${v.delay} sem` : 'None';
        delayEl.style.color = v.delay > 0 ? '#ef4444' : '#10b981';
      }
    }

    const gradEl = document.getElementById('kpi-new-grad');
    const gradDetail = document.getElementById('kpi-grad-detail');
    if (gradEl) {
      if (v.recoveryStatus.key === 'uncertain') {
        gradEl.textContent = 'Needs review';
        if (gradDetail) gradDetail.textContent = 'Path projection incomplete';
      } else if (v.delay > 0) {
        gradEl.textContent = v.projectedGrad;
        if (gradDetail) gradDetail.textContent = `Was ${v.originalGrad}`;
      } else {
        gradEl.textContent = v.projectedGrad;
        if (gradDetail) gradDetail.textContent = 'On track — no graduation delay';
      }
    }

    const semEl = document.getElementById('kpi-semesters');
    if (semEl) semEl.textContent = v.semestersAffected;

    applyRecoveryBadge(v.recoveryStatus);

    const riskAvail = document.getElementById('risk-avail');
    if (riskAvail) riskAvail.textContent = v.retakeLabel;
    const fcAvail = document.getElementById('fc-avail');
    if (fcAvail) fcAvail.textContent = v.retakeLabel;

    renderLeanRecovery({
      src: v.src,
      retakeLabel: v.retakeLabel,
      blockedRaw: v.blockedRaw,
      replacements: v.replacements,
      recoveryStatus: v.recoveryStatus
    });

    renderFullSchedule({
      recoverySchedule: v.recoverySchedule,
      recoveryStatus: v.recoveryStatus,
      src: v.src,
      retakeLabel: v.retakeLabel,
      blockedCount: v.blockedCount,
      timelineId: 'ia-timeline',
      detailsId: 'ia-full-schedule'
    });
  };

  if (!animate) {
    paint();
    return;
  }

  const targets = getSwapTargets();
  targets.forEach(el => el.classList.add('ia-swapping'));
  if (projectionSwapTimer) clearTimeout(projectionSwapTimer);
  projectionSwapTimer = setTimeout(() => {
    paint();
    requestAnimationFrame(() => {
      targets.forEach(el => el.classList.remove('ia-swapping'));
    });
  }, PROJECTION_SWAP_MS);
}

function swapProjectionView(view) {
  applyProjectionView(view, { animate: true });
}

function loadReport(report) {
  const src = report.src || {
    id: report.failedCourseCode,
    name: report.failedCourseName,
    credits: report.failedCourseCredits,
    avail: report.retakeSemesterLabel || 'See schedule',
    type: 'Core',
  };
  const blockedRaw = report.blockedCourses || [];
  const blocked = blockedRaw.map(b => ({
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
  const replacements = report.replacementCourses || [];
  const recommendations = report.recommendations || [];
  const projectedCgpa = report.projectedCgpa ?? report.currentCgpa;
  const cgpaDelta = report.cgpaDelta ?? 0;
  const standingWouldChange = report.standingWouldChange === true;
  const projectedStanding = formatStanding(report.projectedStanding);
  const recoveryStatus = resolveRecoveryStatus({
    delay,
    originalGrad,
    projectedGrad
  });

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
  document.getElementById('fc-type').textContent    = src.type || 'Core';

  const listEl = document.getElementById('blocked-list');
  listEl.innerHTML = '';
  blocked.forEach((b, i) => {
    const isDirect = (b.depth ?? 1) === 1;
    const row = document.createElement('div');
    row.className = `ia-blocked-row ${isDirect ? 'ia-blocked-row-direct' : 'ia-blocked-row-chain'}`;
    row.style.animationDelay = (i * 70) + 'ms';
    row.innerHTML = `
      <div class="ia-blocked-num">${i+1}</div>
      <div class="flex-fill min-w-0">
        <p class="fw-700 mb-0" style="font-size:13px;color:var(--c-text);">${escapeHtml(b.id)}
          <span style="font-weight:500;color:var(--c-text-sub);">— ${escapeHtml(b.name)}</span>
        </p>
        <p style="font-size:11px;color:var(--c-muted);margin:2px 0 0;">${isDirect ? 'Direct dependency' : 'Chain dependency'} · ${b.credits} cr${b.avail ? ' · ' + escapeHtml(b.avail) : ''}</p>
      </div>
      <span class="ia-dep-tag ${isDirect ? 'ia-dep-direct' : 'ia-dep-chain'}">${isDirect ? 'Direct' : 'Chain'}</span>
    `;
    listEl.appendChild(row);
  });
  document.getElementById('blocked-count-badge').textContent = blocked.length + ' courses';
  document.getElementById('risk-credits').textContent = creditsAtRisk + ' cr';

  const actualView = {
    delay,
    semestersAffected: semAff,
    originalGrad,
    projectedGrad,
    retakeLabel,
    recoverySchedule,
    recoveryStatus,
    src,
    blockedRaw,
    replacements,
    blockedCount: blocked.length
  };

  const whatIf = report.whatIfSummerRetake;
  let summerView = null;
  if (whatIf) {
    const summerDelay = whatIf.graduationDelaySemesters ?? 0;
    const summerGrad = whatIf.projectedGraduationLabel || originalGrad;
    summerView = {
      delay: summerDelay,
      semestersAffected: whatIf.semestersAffected ?? summerDelay,
      originalGrad,
      projectedGrad: summerGrad,
      retakeLabel: whatIf.retakeSemesterLabel || retakeLabel,
      recoverySchedule: whatIf.recoverySchedule || [],
      recoveryStatus: resolveRecoveryStatus({
        delay: summerDelay,
        originalGrad,
        projectedGrad: summerGrad
      }),
      src,
      blockedRaw,
      replacements,
      blockedCount: blocked.length
    };
  }

  projectionViews = { actual: actualView, summer: summerView };
  activeProjectionView = 'actual';

  const toggle = document.getElementById('ia-projection-toggle');
  if (toggle) toggle.classList.toggle('d-none', !summerView);
  bindProjectionToggle();
  applyProjectionView('actual');

  const recEl = document.getElementById('ia-recommendations');
  recEl.innerHTML = '';
  const recList = recommendations.length > 0 ? recommendations : [
    `Prioritize <strong>${escapeHtml(src.name || src.id)}</strong> — register for ${escapeHtml(retakeLabel)}.`,
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





