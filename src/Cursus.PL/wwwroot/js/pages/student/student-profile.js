const GPA_DATA = [
  { sem:'Fall\n\'22',  sgpa:2.90 },
  { sem:'Spr\n\'23',  sgpa:3.10 },
  { sem:'Fall\n\'23',  sgpa:3.20 },
  { sem:'Spr\n\'24',  sgpa:3.00 },
  { sem:'Fall\n\'24',  sgpa:3.45 },
  { sem:'Spr\n\'25',  sgpa:3.58 },
  { sem:'Fall\n\'25',  sgpa:3.12 },
];
const MAX_GPA = 4.0;
const barsEl   = document.getElementById('gpa-bars');
const labelsEl = document.getElementById('gpa-x-labels');

GPA_DATA.forEach((d, i) => {
  const pct  = (d.sgpa / MAX_GPA) * 100;
  const bar  = document.createElement('div');
  bar.className = 'sp-gpa-bar-wrap';
  bar.innerHTML = `
    <div class="sp-gpa-bar-tooltip">${d.sgpa.toFixed(2)}</div>
    <div class="sp-gpa-bar" style="--h:${pct}%;--delay:${i*60}ms;"
         data-sgpa="${d.sgpa}"></div>`;
  barsEl.appendChild(bar);

  const lbl = document.createElement('div');
  lbl.className = 'sp-gpa-x-label';
  lbl.textContent = d.sem.replace('\n', ' ');
  labelsEl.appendChild(lbl);
});

// Animate bars on load
requestAnimationFrame(() => {
  document.querySelectorAll('.sp-gpa-bar').forEach(b => b.classList.add('animated'));
});

function openEditModal() {
  document.getElementById('edit-modal').classList.add('open');
  document.body.style.overflow = 'hidden';
}
function closeEditModal(e) {
  if (e && e.target !== document.getElementById('edit-modal')) return;
  document.getElementById('edit-modal').classList.remove('open');
  document.body.style.overflow = '';
}
function saveProfile() {
  const form = document.getElementById('edit-profile-form');
  if (!form) {
    closeEditModal();
    return;
  }

  const fullName = document.getElementById('profile-fullname')?.value?.trim() || '';
  const phone = document.getElementById('profile-phone')?.value?.trim() || '';

  if (!fullName) {
    alert('Full Name is required.');
    return;
  }

  const formData = new FormData(form);
  formData.set('FullName', fullName);
  formData.set('PhoneNumber', phone);
    const email = document.getElementById('profile-email')?.value?.trim() || '';
    formData.set('Email', email);

  fetch('/Student/UpdateProfile', {
    method: 'POST',
    headers: { 'X-Requested-With': 'XMLHttpRequest' },
    body: formData
  })
    .then(async res => {
      const responseText = await res.text();
      let data = {};

      if (responseText) {
        try {
          data = JSON.parse(responseText);
        } catch {
          data = { message: responseText };
        }
      }

      if (!res.ok) {
        return Promise.reject(
          data && typeof data === 'object'
            ? { status: res.status, ...data }
            : { status: res.status, message: 'Request failed.' }
        );
      }

      return data;
    })
    .then(data => {
      if (data && data.success) {
        closeEditModal();
        // Reload to show updated name/email in page
        setTimeout(() => location.reload(), 300);
      }
    })
    .catch(err => {
      console.error('Save error:', err);
      alert('Error saving profile. Please try again.');
    });
}