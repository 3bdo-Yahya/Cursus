/* ── Helpers ────────────────────────────────────────────── */
function setFieldError(inputEl, errorEl, msg) {
  errorEl.textContent = msg;
  inputEl.style.borderColor = '#dc3545';
  inputEl.style.boxShadow   = '0 0 0 3px rgba(220,53,69,0.15)';
}
function clearFieldError(inputEl, errorEl) {
  errorEl.textContent      = '';
  inputEl.style.borderColor = '';
  inputEl.style.boxShadow   = '';
}

/* ── Password toggle ────────────────────────────────────── */
const pwInput   = document.getElementById('password');
const toggleBtn = document.getElementById('toggle-pw');
if (pwInput && toggleBtn) {
  const eyeIcon = toggleBtn.querySelector('span');
  toggleBtn.addEventListener('click', () => {
    const isHidden = pwInput.type === 'password';
    pwInput.type        = isHidden ? 'text' : 'password';
    eyeIcon.textContent = isHidden ? 'visibility_off' : 'visibility';
  });
}

/* ── Live clear-on-type ─────────────────────────────────── */
const emailInput  = document.getElementById('email');
const emailError  = document.getElementById('email-error');
const pwError     = document.getElementById('password-error');

if (emailInput && emailError) {
  emailInput.addEventListener('input', () => clearFieldError(emailInput, emailError));
}
if (pwInput && pwError) {
  pwInput.addEventListener('input', () => clearFieldError(pwInput, pwError));
}

/* ── Form submit + validation ───────────────────────────── */
const loginForm = document.getElementById('login-form');
const btnLabel  = document.getElementById('btn-label');
const signinBtn = document.getElementById('signin-btn');

if (loginForm) {
  loginForm.addEventListener('submit', (e) => {
    e.preventDefault();
    let valid = true;

    /* Email validation */
    const emailVal = emailInput ? emailInput.value.trim() : '';
    if (!emailVal) {
      setFieldError(emailInput, emailError, 'Email address is required.');
      valid = false;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailVal)) {
      setFieldError(emailInput, emailError, 'Please enter a valid email address.');
      valid = false;
    }

    /* Password validation */
    const pwVal = pwInput ? pwInput.value : '';
    if (!pwVal) {
      setFieldError(pwInput, pwError, 'Password is required.');
      valid = false;
    } else if (pwVal.length < 6) {
      setFieldError(pwInput, pwError, 'Password must be at least 6 characters.');
      valid = false;
    }

    if (!valid) return;

    /* Loading state — will then replaced with real submit */
    if (signinBtn) { signinBtn.disabled = true; signinBtn.style.opacity = '0.75'; }
    if (btnLabel)  btnLabel.textContent = 'Signing in…';

    setTimeout(() => {
      if (btnLabel)  btnLabel.textContent = 'Sign In';
      if (signinBtn) { signinBtn.disabled = false; signinBtn.style.opacity = '1'; }
    }, 2000);
  });
}

/* ── Animated SVG connectors ────────────────────────────── */
const graphSvg = document.getElementById('graph-svg');
if (graphSvg) {
  const nodeGroups = graphSvg.querySelectorAll('g[style*="transform-origin"]');
  const lineMap = {
    c1:[0,1], c2:[1,2], c3:[1,3], c4:[2,4],
    c5:[3,5], c6:[4,5], c7:[0,3], c8:[5,6],
    c9:[7,3], c10:[8,2]
  };
  function getCenter(g) {
    const circle = g.querySelectorAll('circle')[1];
    const cx = parseFloat(circle.getAttribute('cx'));
    const cy = parseFloat(circle.getAttribute('cy'));
    const mat = new DOMMatrix(getComputedStyle(g).transform);
    return { x: cx + mat.m41, y: cy + mat.m42 };
  }
  function updateLines() {
    const centers = Array.from(nodeGroups).map(getCenter);
    Object.entries(lineMap).forEach(([id, [a, b]]) => {
      const line = document.getElementById(id);
      if (!line || !centers[a] || !centers[b]) return;
      line.setAttribute('x1', centers[a].x);
      line.setAttribute('y1', centers[a].y);
      line.setAttribute('x2', centers[b].x);
      line.setAttribute('y2', centers[b].y);
    });
    requestAnimationFrame(updateLines);
  }
  requestAnimationFrame(updateLines);
}