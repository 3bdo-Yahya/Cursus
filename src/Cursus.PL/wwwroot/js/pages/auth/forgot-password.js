/* ── Forgot Password ── */
const formRequest  = document.getElementById('form-request');
const stepRequest  = document.getElementById('step-request');
const stepSuccess  = document.getElementById('step-success');
const resetBtn     = document.getElementById('reset-btn');
const resetLabel   = document.getElementById('reset-label');
const emailInput   = document.getElementById('email');
const emailError   = document.getElementById('email-error');
const sentToEmail  = document.getElementById('sent-to-email');

if (formRequest) {
  formRequest.addEventListener('submit', (e) => {
    e.preventDefault();

    /* Client-Side Validation */
    const val = emailInput ? emailInput.value.trim() : '';
    if (!val || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val)) {
      if (emailError) emailError.textContent = 'Please enter a valid email address.';
      if (emailInput) {
        emailInput.style.borderColor = '#dc3545';
        emailInput.focus();
      }
      return;
    }
    if (emailError) emailError.textContent = '';

    /* Loading state */
    if (resetBtn)   { resetBtn.disabled = true; resetBtn.style.opacity = '0.75'; }
    if (resetLabel) resetLabel.textContent = 'Sending…';

    /* Simulate API call — will then replaced with real call */
    setTimeout(() => {
      if (sentToEmail) sentToEmail.textContent = val;
      if (stepRequest) stepRequest.style.display = 'none';
      if (stepSuccess) {
        stepSuccess.style.display = 'block';
        /* Re-trigger animations */
        stepSuccess.querySelectorAll('[class*="anim-"]').forEach(el => {
          el.style.animation = 'none';
          void el.offsetWidth; 
          el.style.animation = '';
        });
      }
    }, 1200);
  });

  /* Clear error on input */
  if (emailInput) {
    emailInput.addEventListener('input', () => {
      if (emailError) emailError.textContent = '';
      emailInput.style.borderColor = '';
    });
  }
}

/* ── Resend button ── */
const resendBtn = document.getElementById('resend-btn');
if (resendBtn) {
  let cooldown = false;
  resendBtn.addEventListener('click', () => {
    if (cooldown) return;
    cooldown = true;
    resendBtn.textContent = 'Sent!';
    resendBtn.style.color = '#059669';
    setTimeout(() => {
      resendBtn.textContent = 'Resend email';
      resendBtn.style.color = '';
      cooldown = false;
    }, 3000);
  });
}

/* ── Animated SVG connectors (shared with login) ── */
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