// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/* ── Shared UI namespace (avoid global name collisions with page scripts) ── */
window.CursusUI = window.CursusUI || {};

/* ── Dark Mode Toggle ────────────────────────────────────── */
const darkToggle = document.getElementById('dark-toggle');
const darkIcon   = document.getElementById('dark-icon');
if (darkToggle && darkIcon) {
  darkToggle.addEventListener('click', () => {
    document.documentElement.classList.toggle('dark');
    darkIcon.textContent = document.documentElement.classList.contains('dark')
      ? 'light_mode' : 'dark_mode';
  });
}

/* ── Notifications ────────────────────────────────────────── */
const notifBtn   = document.getElementById('notif-btn');
const notifPanel = document.getElementById('notif-panel');
if (notifBtn && notifPanel) {
  notifBtn.addEventListener('click', e => { e.stopPropagation(); notifPanel.classList.toggle('d-none'); });
  document.addEventListener('click', () => notifPanel.classList.add('d-none'));
}

/**
 * One implementation for all navbars: `${id}-dropdown`, `${id}-btn`, optional #user-menu-chevron
 */
CursusUI.toggleDropdown = function (id) {
  const dropdown = document.getElementById(`${id}-dropdown`);
  const btn = document.getElementById(`${id}-btn`);
  const chevron = document.getElementById('user-menu-chevron');
  if (!dropdown) return;

  const isOpen = dropdown.classList.contains('open');
  document.querySelectorAll('.custom-dropdown.open').forEach((d) => d.classList.remove('open'));
  document.querySelectorAll('.custom-select-btn.open').forEach((b) => b.classList.remove('open'));

  if (!isOpen) {
    dropdown.classList.add('open');
    if (btn) btn.classList.add('open');
    if (chevron) chevron.style.transform = 'rotate(180deg)';
  } else {
    if (chevron) chevron.style.transform = 'rotate(0deg)';
  }
};

window.toggleDropdown = function (id) {
  return CursusUI.toggleDropdown(id);
};

document.addEventListener('click', (e) => {
  if (!e.target.closest('.custom-select-wrap')) {
    document.querySelectorAll('.custom-dropdown.open').forEach((d) => d.classList.remove('open'));
    document.querySelectorAll('.custom-select-btn.open').forEach((b) => b.classList.remove('open'));
    const chevron = document.getElementById('user-menu-chevron');
    if (chevron) chevron.style.transform = 'rotate(0deg)';
  }
});

/* ── Scroll reveal ────────────────────────────────── */
const _vcObserver = new IntersectionObserver(entries => {
  entries.forEach(e => {
    if (e.isIntersecting) { e.target.classList.add('in-view'); _vcObserver.unobserve(e.target); }    
  });
}, { threshold: 0.08 });
document.querySelectorAll('[data-scroll],[data-scroll-group]').forEach(el => _vcObserver.observe(el));