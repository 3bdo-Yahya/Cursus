const ADVISOR_CHAT_ENDPOINT = '/Student/AiAdvisorChat';

/* ── Chat state ─────────────────────────────────────────── */
let chatHistory = [];        
let isAwaitingResponse = false;

const messagesArea = document.getElementById('messages-area');
const emptyState   = document.getElementById('empty-state');
const chipsRow     = document.getElementById('chips-row');
const chatInput    = document.getElementById('chat-input');
const sendBtn      = document.getElementById('send-btn');

/* ── Enable / disable send button ──────────────────────── */
chatInput.addEventListener('input', () => {
  sendBtn.disabled = chatInput.value.trim() === '' || isAwaitingResponse;
});

/* ── Auto-resize textarea ───────────────────────────────── */
function autoResize(el) {
  el.style.height = 'auto';
  el.style.height = Math.min(el.scrollHeight, 120) + 'px';
}

/* ── Enter to send / Shift+Enter = newline ──────────────── */
function handleKey(e) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault();
    if (!sendBtn.disabled) sendMessage();
  }
}

/* ── Send from suggestion chip ──────────────────────────── */
function sendSuggestion(btn) {
  chatInput.value = btn.textContent.trim();
  sendMessage();
}

/* ── Hide empty state + chips on first message ──────────── */
function activateChat() {
  if (emptyState && !emptyState.classList.contains('d-none')) {
    emptyState.style.animation = 'none';
    emptyState.style.opacity   = '0';
    emptyState.style.transition = 'opacity 0.2s ease';
    setTimeout(() => emptyState.classList.add('d-none'), 200);
  }
  if (chipsRow) chipsRow.classList.add('d-none');
}

/* ── Append date divider ────────────────────────────────── */
function appendDateDivider() {
  const d = document.createElement('div');
  d.className = 'date-divider';
  d.textContent = 'Today';
  messagesArea.appendChild(d);
}

/* ── Append a message bubble ────────────────────────────── */
function appendMessage(role, text) {
  const isAI = role === 'ai';

  const row = document.createElement('div');
  row.className = `msg-row ${isAI ? '' : 'user'}`;

  const av = document.createElement('div');
  av.className = `msg-avatar ${isAI ? 'ai-av' : 'user-av'}`;
  if (isAI) {
    av.innerHTML = `<span class="material-symbols-outlined">smart_toy</span>`;
  } else {
    av.textContent = 'AK';
  }

  const body = document.createElement('div');
  body.className = 'msg-body';

  const sender = document.createElement('div');
  sender.className = 'msg-sender';
  sender.textContent = isAI ? 'AI Advisor' : 'You';

  const bubble = document.createElement('div');
  bubble.className = `msg-bubble ${isAI ? 'ai' : 'user'}`;
  const safeText = escapeHTML(text);
  bubble.innerHTML = isAI
    ? safeText.replace(/\b([A-Z]{2,4}\d{3}[A-Z]?)\b/g, '<span class="course-ref">$1</span>')
              .replace(/\n/g, '<br>')
    : safeText.replace(/\n/g, '<br>');

  body.appendChild(sender);
  body.appendChild(bubble);

  if (isAI) {
    row.appendChild(av);
    row.appendChild(body);
  } else {
    row.appendChild(body);
    row.appendChild(av);
  }

  messagesArea.appendChild(row);
  scrollToBottom();
  return bubble;
}

/* ── Typing indicator ───────────────────────────────────── */
function appendTyping() {
  const row = document.createElement('div');
  row.className = 'msg-row';
  row.id        = 'typing-row';

  const av = document.createElement('div');
  av.className = 'msg-avatar ai-av';
  av.innerHTML = `<span class="material-symbols-outlined">smart_toy</span>`;

  const body = document.createElement('div');
  body.className = 'msg-body';

  const sender = document.createElement('div');
  sender.className = 'msg-sender';
  sender.textContent = 'AI Advisor';

  const bubble = document.createElement('div');
  bubble.className = 'typing-bubble';
  bubble.innerHTML = `<div class="typing-dot"></div><div class="typing-dot"></div><div class="typing-dot"></div>`;

  body.appendChild(sender);
  body.appendChild(bubble);
  row.appendChild(av);
  row.appendChild(body);
  messagesArea.appendChild(row);
  scrollToBottom();
}

function removeTyping() {
  const t = document.getElementById('typing-row');
  if (t) t.remove();
}

/* ── Error banner ───────────────────────────────────────── */
function appendErrorBanner(message) {
  const wrap = document.createElement('div');
  wrap.className = 'error-banner';
  const fallbackMessage = message || 'Please try again in a moment.';
  wrap.innerHTML = `
    <div class="error-icon">
      <span class="material-symbols-outlined">warning</span>
    </div>
    <div>
      <p class="fw-800 mb-1" style="font-size:13px;color:var(--alert-warn-title);">AI Advisor is temporarily unavailable</p>
      <p class="mb-0" style="font-size:12.5px;color:var(--alert-warn-text);line-height:1.6;">
        ${escapeHTML(fallbackMessage)} While we reconnect, you can review your
        <a href="/Student/Progress" style="color:var(--c-primary);font-weight:700;">Progress Tracker</a>
        or use the
        <a href="/Student/GpaSimulator" style="color:var(--c-primary);font-weight:700;">GPA Simulator</a>
        for detailed insights.
      </p>
    </div>`;
  messagesArea.appendChild(wrap);
  scrollToBottom();
}

/* ── Scroll to bottom ───────────────────────────────────── */
function scrollToBottom() {
  messagesArea.scrollTo({ top: messagesArea.scrollHeight, behavior: 'smooth' });
}

/* ── Escape HTML ────────────────────────────────────────── */
function escapeHTML(str) {
  return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

/* ── Server endpoint helpers ───────────────────────────── */
function getAntiForgeryToken() {
  return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

function readAdvisorResponse(payload) {
  if (!payload || typeof payload !== 'object') {
    return {
      succeeded: false,
      message: 'The AI advisor returned an unexpected response.'
    };
  }

  return {
    succeeded: payload.succeeded ?? payload.Succeeded ?? false,
    message: payload.message ?? payload.Message ?? '',
    errorCode: payload.errorCode ?? payload.ErrorCode ?? null
  };
}

async function parseAdvisorPayload(response) {
  const contentType = response.headers.get('content-type') || '';
  if (!contentType.includes('application/json')) {
    return {
      succeeded: false,
      message: await response.text()
    };
  }

  return readAdvisorResponse(await response.json());
}

async function requestAdvisorReply(userText) {
  const token = getAntiForgeryToken();
  if (!token) {
    throw new Error('The secure chat token is missing. Refresh the page and try again.');
  }

  const response = await fetch(ADVISOR_CHAT_ENDPOINT, {
    method: 'POST',
    credentials: 'same-origin',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'RequestVerificationToken': token
    },
    body: JSON.stringify({ message: userText })
  });

  const result = await parseAdvisorPayload(response);
  if (!response.ok || !result.succeeded) {
    throw new Error(result.message || 'The AI advisor is temporarily unavailable. Please try again later.');
  }

  return result.message;
}

/* ── Main send function ─────────────────────────────────── */
async function sendMessage() {
  const text = chatInput.value.trim();
  if (!text || isAwaitingResponse) return;

  if (chatHistory.length === 0) {
    activateChat();
    appendDateDivider();
  }

  appendMessage('user', text);
  chatHistory.push({ role: 'user', content: text });

  chatInput.value = '';
  chatInput.style.height = 'auto';
  sendBtn.disabled = true;
  isAwaitingResponse = true;

  appendTyping();

  try {
    const reply = await requestAdvisorReply(text);

    removeTyping();
    appendMessage('ai', reply);
    chatHistory.push({ role: 'assistant', content: reply });

  } catch (err) {
    removeTyping();
    appendErrorBanner(err.message);
    chatInput.value = text;
    chatHistory.pop(); 
    autoResize(chatInput);
  } finally {
    isAwaitingResponse = false;
    sendBtn.disabled = chatInput.value.trim() === '';
    chatInput.focus();
  }
}

/* ── Clear chat ─────────────────────────────────────────── */
function clearChat() {
  if (isAwaitingResponse) return;
  chatHistory = [];

  const toRemove = messagesArea.querySelectorAll('.msg-row, .date-divider, .error-banner');
  toRemove.forEach(el => {
    el.style.transition = 'opacity 0.2s ease, transform 0.2s ease';
    el.style.opacity = '0';
    el.style.transform = 'translateY(-8px)';
    setTimeout(() => el.remove(), 200);
  });

  setTimeout(() => {
    if (emptyState) {
      emptyState.classList.remove('d-none');
      emptyState.style.opacity = '';
      emptyState.style.transition = '';
    }
    if (chipsRow) chipsRow.classList.remove('d-none');
  }, 220);
}
