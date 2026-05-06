/* ── Show More / Collapse ───────────────────────────────── */
function toggleExpand(btn) {
  const card   = btn.closest('.category-card');
  const extras = card.querySelectorAll('.extra-item');
  const isOpen = btn.classList.contains('expanded');
  const label  = btn.querySelector('span:first-child');

  extras.forEach((el, i) => {
    if (isOpen) {
      el.classList.add('d-none');
    } else {
      el.classList.remove('d-none');
      el.style.opacity = '0';
      el.style.transform = 'translateY(8px)';
      el.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
      setTimeout(() => {
        el.style.opacity = '1';
        el.style.transform = 'translateY(0)';
      }, i * 60 + 10);
    }
  });

  btn.classList.toggle('expanded');
  label.textContent = isOpen
    ? `Show ${extras.length} more`
    : 'Show less';
}

/* ── Scroll Reveal ──────────────────────────────────────── */
const observer = new IntersectionObserver(entries => {
  entries.forEach(e => {
    if (e.isIntersecting) {
      e.target.classList.add('in-view');
      observer.unobserve(e.target);
    }
  });
}, { threshold: 0.08 });

document.querySelectorAll('[data-scroll],[data-scroll-group]').forEach(el => observer.observe(el));