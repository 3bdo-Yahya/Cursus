/* ── Greeting Text ───────────────────────────────────────── */
const greetingEl = document.getElementById('greeting-text');

if (greetingEl) {
  const username = greetingEl.dataset.username || 'User';

  const hour = new Date().getHours();
  let salutation;

  if      (hour >= 5  && hour < 12) salutation = 'Good Morning';
  else if (hour >= 12 && hour < 17) salutation = 'Good Afternoon';
  else if (hour >= 17 && hour < 21) salutation = 'Good Evening';
  else                              salutation = 'Good Night';

  greetingEl.textContent = `${salutation}, ${username}!`;
}

/* ── GPA Ring ─────────────────────────────────────────────── */
const gpaRing = document.getElementById('gpa-ring');

if (gpaRing) {
  const gpa = 3.2;
  const ratio = Math.min(gpa / 4.0, 1);

  const run = () => {
    gpaRing.style.strokeDashoffset = (2 * Math.PI * 19) * (1 - ratio);
  };

  window.addEventListener('load', () => {
    setTimeout(run, 400);
  });
}

function updateGpaDisplay(sgpa, cgpa) {
  const sgpaEl = document.getElementById('predicted-sgpa');
  const cgpaEl = document.getElementById('predicted-cgpa');
  const ring   = document.getElementById('gpa-ring');

  if (sgpaEl) sgpaEl.textContent = sgpa ?? '—';
  if (cgpaEl) cgpaEl.textContent = cgpa ?? '—';

  const value = parseFloat(cgpa);

  if (ring && !isNaN(value)) {
    const radius = 19; 
    const circumference = 2 * Math.PI * radius;
    const ratio = Math.min(value / 4.0, 1);

    ring.style.strokeDashoffset = circumference * (1 - ratio);
  }
}

/* ── Scroll Reveal ────────────────────────────────────────── */
const scrollObserver = new IntersectionObserver((entries) => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      entry.target.classList.add('in-view');
      scrollObserver.unobserve(entry.target);
    }
  });
}, { threshold: 0.12 });

document.querySelectorAll('[data-scroll], [data-scroll-group]')
  .forEach(el => scrollObserver.observe(el));