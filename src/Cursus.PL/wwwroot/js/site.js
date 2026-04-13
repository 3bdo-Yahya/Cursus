// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/* ── Dark Mode Toggle ────────────────────────────────────── */
const darkToggle = document.getElementById('dark-toggle');
const darkIcon   = document.getElementById('dark-icon');
if (darkToggle) {
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