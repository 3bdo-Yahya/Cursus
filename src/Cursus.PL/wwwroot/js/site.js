window.CursusUI = window.CursusUI || {};

/* ── Dark Mode Toggle ────────────────────────────────────── */
(function () {
  const darkToggle = document.getElementById('dark-toggle');
  const darkIcon   = document.getElementById('dark-icon');
  if (!darkToggle || !darkIcon) return;

  function applyTheme(isDark) {
    document.documentElement.classList.toggle('dark', isDark);
    document.documentElement.classList.toggle('light', !isDark);
    document.documentElement.setAttribute('data-bs-theme', isDark ? 'dark' : 'light');
    darkIcon.textContent = isDark ? 'light_mode' : 'dark_mode';
    try { localStorage.setItem('theme', isDark ? 'dark' : 'light'); } catch (e) {}
  }

  // Sync icon/theme attrs to current class (set by inline script in _Layout)
  applyTheme(document.documentElement.classList.contains('dark'));

  darkToggle.addEventListener('click', () => {
    applyTheme(!document.documentElement.classList.contains('dark'));
  });
})();

/* ── Notifications ────────────────────────────────────────── */
const notifBtn   = document.getElementById('notif-btn');
const notifPanel = document.getElementById('notif-panel');
if (notifBtn && notifPanel) {
  notifBtn.addEventListener('click', e => { e.stopPropagation(); notifPanel.classList.toggle('d-none'); });
  document.addEventListener('click', () => notifPanel.classList.add('d-none'));
}

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
/* ── Mobile Drawer ────────────────────────────────── */
(function () {
  const btn      = document.getElementById('mobile-menu-btn');
  const drawer   = document.getElementById('mobile-drawer');
  const backdrop = document.getElementById('mobile-drawer-backdrop');
  const closeBtn = document.getElementById('mobile-drawer-close');

  if (!btn || !drawer) return;

  function openDrawer() {
    drawer.classList.add('open');
    backdrop.classList.add('open');
    btn.setAttribute('aria-expanded', 'true');
    document.body.style.overflow = 'hidden';
  }

  function closeDrawer() {
    drawer.classList.remove('open');
    backdrop.classList.remove('open');
    btn.setAttribute('aria-expanded', 'false');
    document.body.style.overflow = '';
  }

  btn.addEventListener('click', openDrawer);
  if (closeBtn)  closeBtn.addEventListener('click', closeDrawer);
  if (backdrop)  backdrop.addEventListener('click', closeDrawer);

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeDrawer();
  });

  window.addEventListener('resize', () => {
    if (window.innerWidth >= 768) closeDrawer();
  });
}());

