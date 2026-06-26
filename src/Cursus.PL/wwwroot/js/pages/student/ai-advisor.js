/* ── Chat state ─────────────────────────────────────────── */
let chatHistory = [];
let isAwaitingResponse = false;

const messagesArea = document.getElementById('messages-area');
const emptyState = document.getElementById('empty-state');
const chipsRow = document.getElementById('chips-row');
const chatInput = document.getElementById('chat-input');
const sendBtn = document.getElementById('send-btn');

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
    emptyState.style.opacity = '0';
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
  bubble.innerHTML = isAI
    ? text.replace(/\b([A-Z]{2,4}\d{3}[A-Z]?)\b/g, '<span class="course-ref">$1</span>')
      .replace(/\n/g, '<br>')
    : escapeHTML(text).replace(/\n/g, '<br>');

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
  row.id = 'typing-row';

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
function appendErrorBanner() {
  const wrap = document.createElement('div');
  wrap.className = 'error-banner';
  wrap.innerHTML = `
    <div class="error-icon">
      <span class="material-symbols-outlined">warning</span>
    </div>
    <div>
      <p class="fw-800 mb-1" style="font-size:13px;color:var(--alert-warn-title);">AI Advisor is temporarily unavailable</p>
      <p class="mb-0" style="font-size:12.5px;color:var(--alert-warn-text);line-height:1.6;">
        Please try again in a moment. While we reconnect, you can review your
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
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

/* ── Get CSRF token from hidden input ───────────────────── */
function getAntiForgeryToken() {
  const input = document.querySelector('input[name="__RequestVerificationToken"]');
  return input ? input.value : '';
}

/* ── Real backend call ───────────────────────────────────── */
async function fetchAdvisorReply(userText) {
  const historyToSend = chatHistory.slice(0, -1);

  const response = await fetch('/Student/AiAdvisorChat', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'RequestVerificationToken': getAntiForgeryToken()
    },
    body: JSON.stringify({
      message: userText,
      history: historyToSend
    })
  });

  if (!response.ok) {
    throw new Error('Request failed: ' + response.status);
  }

  const data = await response.json();
  return data.reply;
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
    // Do not call third-party LLM APIs from the browser (keys would be exposed). Production will use a
    // server endpoint, e.g. POST /api/ai-advisor/chat, with the key stored only on the server.
    const reply = await fetchAdvisorReply(text);

    removeTyping();
    appendMessage('ai', reply);
    chatHistory.push({ role: 'assistant', content: reply });

  } catch (err) {
    removeTyping();
    appendErrorBanner();
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