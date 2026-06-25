const advisorPage = document.querySelector('[data-advisor-chat-url]');
const ADVISOR_CHAT_ENDPOINT = advisorPage?.dataset.advisorChatUrl || '/Student/AiAdvisorChat';
const ADVISOR_HISTORY_ENDPOINT = advisorPage?.dataset.advisorHistoryUrl || '/Student/AiAdvisorHistory';
const ADVISOR_CLEAR_ENDPOINT = advisorPage?.dataset.advisorClearUrl || '/Student/AiAdvisorClearHistory';
const DEFAULT_ERROR_MESSAGE = 'Please try again in a moment.';

/* ── Chat state ─────────────────────────────────────────── */
let chatHistory = [];        
let isAwaitingResponse = false;
let hasDateDivider = false;

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
  if (hasDateDivider) return;

  const d = document.createElement('div');
  d.className = 'date-divider';
  d.textContent = 'Today';
  messagesArea.appendChild(d);
  hasDateDivider = true;
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
    av.textContent = 'You';
  }

  const body = document.createElement('div');
  body.className = 'msg-body';

  const sender = document.createElement('div');
  sender.className = 'msg-sender';
  sender.textContent = isAI ? 'AI Advisor' : 'You';

  const bubble = document.createElement('div');
  bubble.className = `msg-bubble ${isAI ? 'ai' : 'user'}`;
  bubble.innerHTML = isAI
    ? formatAdvisorText(text)
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
  const fallbackMessage = normalizeFallbackMessage(message);
  wrap.innerHTML = `
    <div class="error-icon">
      <span class="material-symbols-outlined">warning</span>
    </div>
    <div>
      <p class="fw-800 mb-1" style="font-size:13px;color:var(--alert-warn-title);">AI Advisor is temporarily unavailable</p>
      <p class="mb-0" style="font-size:12.5px;color:var(--alert-warn-text);line-height:1.6;">
        ${escapeHTML(fallbackMessage)} You can also review your
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
  return String(str ?? '')
    .replace(/&/g,'&amp;')
    .replace(/</g,'&lt;')
    .replace(/>/g,'&gt;')
    .replace(/"/g,'&quot;');
}

function formatAdvisorText(text) {
  const safeLines = escapeHTML(text)
    .split(/\n+/)
    .map(line => line
      .replace(/^\s*[-*]\s+/, '&bull; ')
      .replace(/^\s*(\d+)\.\s+/, '$1. ')
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      .replace(/\b([A-Z]{2,4}\d{3}[A-Z]?)\b/g, '<span class="course-ref">$1</span>'));

  return safeLines.join('<br>');
}

function clearFollowUps() {
  messagesArea.querySelectorAll('.followup-row').forEach(row => row.remove());
}

function appendFollowUps(questions) {
  clearFollowUps();

  const safeQuestions = (questions || [])
    .map(question => String(question || '').trim())
    .filter(Boolean)
    .slice(0, 4);

  if (safeQuestions.length === 0) return;

  const row = document.createElement('div');
  row.className = 'followup-row';

  safeQuestions.forEach(question => {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'followup-chip';
    button.textContent = question;
    button.addEventListener('click', () => {
      chatInput.value = question;
      autoResize(chatInput);
      sendBtn.disabled = isAwaitingResponse;
      sendMessage();
    });
    row.appendChild(button);
  });

  messagesArea.appendChild(row);
  scrollToBottom();
}

function buildLocalFollowUps(userText, advisorText) {
  const combined = `${userText || ''} ${advisorText || ''}`.toLowerCase();
  const questions = [];

  const add = question => {
    if (!questions.includes(question)) questions.push(question);
  };

  if (combined.includes('gpa') || combined.includes('cgpa') || combined.includes('grade')) {
    add('Which courses can raise my GPA the most?');
    add('What grades should I target this semester?');
  }

  if (combined.includes('fail') || combined.includes('drop') || combined.includes('impact')) {
    add('Which courses would be delayed if this goes wrong?');
    add('How should I recover if I fail this course?');
  }

  if (combined.includes('next semester') || combined.includes('available') || combined.includes('take next')) {
    add('Can you rank my available courses for next semester?');
    add('What is a balanced course load for me?');
  }

  add('Am I still on track to graduate?');
  add('What should I focus on this week?');
  add('What should I ask my faculty advisor?');

  return questions.slice(0, 4);
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
    errorCode: payload.errorCode ?? payload.ErrorCode ?? null,
    suggestedQuestions: payload.suggestedQuestions ?? payload.SuggestedQuestions ?? []
  };
}

function buildGenericErrorMessage(response) {
  if (response.status === 401 || response.status === 403 || response.redirected) {
    return 'Your session may have expired. Refresh the page and sign in again.';
  }

  if (response.status === 400) {
    return 'The advisor could not read that message. Please check it and try again.';
  }

  if (response.status >= 500) {
    return 'The advisor service is temporarily unavailable. Please try again later.';
  }

  return DEFAULT_ERROR_MESSAGE;
}

function normalizeFallbackMessage(message) {
  const safeMessage = String(message || DEFAULT_ERROR_MESSAGE).trim();
  if (!safeMessage || /<(!doctype|html|head|body|form)\b/i.test(safeMessage)) {
    return DEFAULT_ERROR_MESSAGE;
  }

  return safeMessage.length > 240
    ? `${safeMessage.slice(0, 237)}...`
    : safeMessage;
}

async function parseAdvisorPayload(response) {
  const contentType = response.headers.get('content-type') || '';
  if (!contentType.includes('application/json')) {
    return {
      succeeded: false,
      message: buildGenericErrorMessage(response)
    };
  }

  try {
    return readAdvisorResponse(await response.json());
  } catch {
    return {
      succeeded: false,
      message: buildGenericErrorMessage(response)
    };
  }
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

  return result;
}

async function loadSavedHistory() {
  try {
    const response = await fetch(ADVISOR_HISTORY_ENDPOINT, {
      method: 'GET',
      credentials: 'same-origin',
      headers: {
        'Accept': 'application/json'
      }
    });

    if (!response.ok) return;

    const payload = await response.json();
    const messages = Array.isArray(payload)
      ? payload
      : (payload.messages ?? payload.Messages ?? []);

    const safeMessages = messages
      .map(message => ({
        role: normalizeHistoryRole(message.role ?? message.Role),
        content: String(message.content ?? message.Content ?? '').trim()
      }))
      .filter(message => message.role && message.content);

    if (safeMessages.length === 0) return;

    activateChat();
    appendDateDivider();

    safeMessages.forEach(message => {
      appendMessage(message.role === 'assistant' ? 'ai' : 'user', message.content);
    });

    chatHistory = safeMessages.map(message => ({
      role: message.role,
      content: message.content
    }));

    const lastAssistant = [...safeMessages].reverse()
      .find(message => message.role === 'assistant');

    if (lastAssistant) {
      appendFollowUps(buildLocalFollowUps('', lastAssistant.content));
    }
  } catch {
    // Loading history is helpful, but the page should still be usable if it fails.
  }
}

function normalizeHistoryRole(role) {
  const safeRole = String(role || '').toLowerCase();
  if (safeRole === 'assistant' || safeRole === 'ai' || safeRole === 'model') return 'assistant';
  if (safeRole === 'user' || safeRole === 'student') return 'user';
  return '';
}

/* ── Main send function ─────────────────────────────────── */
async function sendMessage() {
  const text = chatInput.value.trim();
  if (!text || isAwaitingResponse) return;

  if (chatHistory.length === 0) {
    activateChat();
    appendDateDivider();
  }

  clearFollowUps();
  appendMessage('user', text);
  chatHistory.push({ role: 'user', content: text });

  chatInput.value = '';
  chatInput.style.height = 'auto';
  sendBtn.disabled = true;
  isAwaitingResponse = true;

  appendTyping();

  try {
    const result = await requestAdvisorReply(text);
    const reply = result.message;

    removeTyping();
    appendMessage('ai', reply);
    chatHistory.push({ role: 'assistant', content: reply });
    appendFollowUps(result.suggestedQuestions || buildLocalFollowUps(text, reply));

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
async function clearChat() {
  if (isAwaitingResponse) return;

  const token = getAntiForgeryToken();
  if (!token) {
    appendErrorBanner('The secure chat token is missing. Refresh the page and try again.');
    return;
  }

  const response = await fetch(ADVISOR_CLEAR_ENDPOINT, {
    method: 'POST',
    credentials: 'same-origin',
    headers: {
      'Accept': 'application/json',
      'RequestVerificationToken': token
    }
  });

  if (!response.ok) {
    appendErrorBanner(buildGenericErrorMessage(response));
    return;
  }

  chatHistory = [];
  hasDateDivider = false;

  const toRemove = messagesArea.querySelectorAll('.msg-row, .date-divider, .error-banner, .followup-row');
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

loadSavedHistory();
