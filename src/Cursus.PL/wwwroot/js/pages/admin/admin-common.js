function toggleDropdown(id) {
  const dropdown = document.getElementById(`${id}-dropdown`);
  const button = document.getElementById(`${id}-btn`);
  if (!dropdown) return;

  const isOpen = dropdown.classList.contains('open');
  document.querySelectorAll('.custom-dropdown.open').forEach((node) => node.classList.remove('open'));
  document.querySelectorAll('.custom-select-btn.open').forEach((node) => node.classList.remove('open'));

  if (!isOpen) {
    dropdown.classList.add('open');
    if (button) button.classList.add('open');
  }
}

document.addEventListener('click', (event) => {
  if (!event.target.closest('.custom-select-wrap')) {
    document.querySelectorAll('.custom-dropdown.open').forEach((node) => node.classList.remove('open'));
    document.querySelectorAll('.custom-select-btn.open').forEach((node) => node.classList.remove('open'));
  }
});
