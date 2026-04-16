/* ── Edit-specific state ─────────────────────────────────── */
selectedPrereqs = [
  { code: 'CS101',  name: 'Introduction to Computer Science' },
  { code: 'MTH101', name: 'Calculus I' },
];

let isDirty = false;

/* ── Mark unsaved changes ────────────────────────────────── */
function markDirty() {
  if (!isDirty) {
    isDirty = true;
    document.getElementById('unsaved-banner').classList.remove('d-none');
  }
}

/* ── Active Status ───────────────────────────── */
const toggleInput = document.getElementById('course-active');
const toggleText  = document.getElementById('toggle-text');
if (toggleInput && toggleText) {
  toggleText.style.color = toggleInput.checked ? '#059669' : 'var(--c-muted)';
  toggleInput.addEventListener('change', () => {
    toggleText.textContent = toggleInput.checked ? 'Active' : 'Inactive';
    toggleText.style.color = toggleInput.checked ? '#059669' : 'var(--c-muted)';
    markDirty();
  });
}

/* ── Availability ──────────────────────────── */
document.querySelectorAll('input[name="avail"]').forEach(radio => {
  radio.addEventListener('change', () => {
    document.querySelectorAll('.avail-label').forEach(l => l.classList.remove('avail-label-checked'));
    if (radio.checked) radio.nextElementSibling.classList.add('avail-label-checked');
  });
});


const _origAddPrereq = addPrereq;
addPrereq = function(code, name) {
  _origAddPrereq(code, name);
  markDirty();
  document.getElementById('prereq-change-warn').classList.remove('d-none');
  document.getElementById('prereq-change-warn').style.display = 'flex';
};

const _origRemovePrereq = removePrereq;
removePrereq = function(code) {
  _origRemovePrereq(code);
  markDirty();
  document.getElementById('prereq-change-warn').classList.remove('d-none');
  document.getElementById('prereq-change-warn').style.display = 'flex';
};

/* ── Save changes ────────────────────────────────────────── */
function saveChanges() {
  let ok = true;
  if (!document.getElementById('course-code').value.trim()) {
    showError('fg-code', 'err-code'); ok = false;
  } else { clearError('fg-code'); }
  if (!document.getElementById('course-name').value.trim()) {
    showError('fg-name', 'err-name'); ok = false;
  } else { clearError('fg-name'); }
  if (!ok) { window.scrollTo({ top: 0, behavior: 'smooth' }); return; }

  const btn = document.getElementById('save-btn');
  btn.disabled = true;
  btn.innerHTML = '<span class="material-symbols-outlined" style="font-size:16px;animation:spin .7s linear infinite">progress_activity</span> Saving…';

  setTimeout(() => {
    btn.innerHTML = '<span class="material-symbols-outlined" style="font-size:17px;font-variation-settings:\'FILL\' 1,\'wght\' 400">check_circle</span> Saved!';
    btn.style.background = 'linear-gradient(135deg,#059669,#047857)';

    isDirty = false;
    document.getElementById('unsaved-banner').classList.add('d-none');

    const hint = document.getElementById('save-hint');
    hint.textContent = 'Last saved just now';
    hint.style.color = '#059669';

    setTimeout(() => {
      btn.disabled = false;
      btn.innerHTML = '<span class="material-symbols-outlined" style="font-size:17px;font-variation-settings:\'FILL\' 1,\'wght\' 400">save</span> Save Changes';
      btn.style.background = '';
      hint.textContent = 'All changes will be applied immediately';
      hint.style.color = '';
    }, 2500);
  }, 800);
}

/* ── Discard changes ─────────────────────────────────────── */
function discardChanges() {
  isDirty = false;
  document.getElementById('unsaved-banner').classList.add('d-none');
  document.getElementById('course-code').value = 'CS201';
  document.getElementById('course-name').value = 'Data Structures & Algorithms';
}

/* ── Archive modal ───────────────────────────────────────── */
function confirmArchive() {
  const code = document.getElementById('course-code').value.trim() || 'this course';
  document.getElementById('modal-course-code').textContent = code;
  document.getElementById('archive-modal').classList.remove('d-none');
}

function closeArchiveModal() {
  document.getElementById('archive-modal').classList.add('d-none');
}

function archiveCourse() {
  closeArchiveModal();
  window.location.href = 'courses.html';
}

window.addEventListener('beforeunload', e => {
  if (isDirty) {
    e.preventDefault();
    e.returnValue = '';
  }
});
