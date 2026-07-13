const GPA_DATA = Array.isArray(window.GPA_HISTORY) ? window.GPA_HISTORY : [];

if (GPA_DATA.length === 0) {
  const chart = document.querySelector('.sp-gpa-chart');
  if (chart) {
    chart.innerHTML = '<p class="text-muted text-center py-4 mb-0" style="font-size:13px;">No GPA history recorded yet.</p>';
  }
} else {
  const MAX_GPA = 4.0;
  const barsEl = document.getElementById('gpa-bars');
  const labelsEl = document.getElementById('gpa-x-labels');

  if (barsEl && labelsEl) {
    GPA_DATA.forEach((d, i) => {
      const pct = (d.sgpa / MAX_GPA) * 100;
      const bar = document.createElement('div');
      bar.className = 'sp-gpa-bar-wrap';
      bar.innerHTML = `
    <div class="sp-gpa-bar-tooltip">${d.sgpa.toFixed(2)}</div>
    <div class="sp-gpa-bar" style="--h:${pct}%;--delay:${i * 60}ms;"
         data-sgpa="${d.sgpa}"></div>`;
      barsEl.appendChild(bar);

      const lbl = document.createElement('div');
      lbl.className = 'sp-gpa-x-label';
      lbl.textContent = d.sem.replace('\n', ' ');
      labelsEl.appendChild(lbl);
    });

    requestAnimationFrame(() => {
      document.querySelectorAll('.sp-gpa-bar').forEach(b => b.classList.add('animated'));
    });
  }
}

function openEditModal() {
  document.getElementById('edit-modal').classList.add('open');
  document.body.style.overflow = 'hidden';
}
function closeEditModal(e) {
  if (e && e.target !== document.getElementById('edit-modal')) return;
  document.getElementById('edit-modal').classList.remove('open');
  document.body.style.overflow = '';
}
function saveProfile() {
  closeEditModal();
}
