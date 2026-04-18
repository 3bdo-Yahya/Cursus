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
/* ── User Menu Dropdown ────────────────────────────────── */
const userMenuBtn     = document.getElementById('user-menu-btn');
const userMenuPanel   = document.getElementById('user-menu-panel');
const userMenuChevron = document.getElementById('user-menu-chevron');
if (userMenuBtn && userMenuPanel) {
  userMenuBtn.addEventListener('click', e => {
    e.stopPropagation();
    const isOpen = !userMenuPanel.classList.contains('d-none');
    userMenuPanel.classList.toggle('d-none');
    if (userMenuChevron) userMenuChevron.style.transform = isOpen ? 'rotate(0deg)' : 'rotate(180deg)';
  });
  document.addEventListener('click', () => {
    userMenuPanel.classList.add('d-none');
    if (userMenuChevron) userMenuChevron.style.transform = 'rotate(0deg)';
  });
}
/* ── Scroll reveal ────────────────────────────────── */
const _vcObserver = new IntersectionObserver(entries => {
  entries.forEach(e => {
    if (e.isIntersecting) { e.target.classList.add('in-view'); _vcObserver.unobserve(e.target); }    
  });
}, { threshold: 0.08 });
document.querySelectorAll('[data-scroll],[data-scroll-group]').forEach(el => _vcObserver.observe(el));