window.CursusFormHelpers = (function formHelpersModule() {
  function setButtonLoading(button, loadingText) {
    if (!button) return;

    if (!button.dataset.defaultHtml) {
      button.dataset.defaultHtml = button.innerHTML;
    }

    button.disabled = true;
    button.innerHTML = `<span class="material-symbols-outlined" style="font-size:16px;animation:spin .7s linear infinite">progress_activity</span> ${loadingText}`;
  }

  function resetButton(button, fallbackHtml) {
    if (!button) return;

    button.disabled = false;
    button.innerHTML = button.dataset.defaultHtml || fallbackHtml || button.innerHTML;
  }

  function initJqueryValidation(formSelector, rules) {
    if (!(window.jQuery && window.jQuery.fn && window.jQuery.fn.validate)) {
      return false;
    }

    const normalizedRules = {};
    Object.keys(rules || {}).forEach((id) => {
      normalizedRules[id] = rules[id];
    });

    window.jQuery(formSelector).validate({
      ignore: [],
      rules: normalizedRules,
      errorPlacement: () => {},
      highlight: () => {},
      unhighlight: () => {}
    });

    return true;
  }

  function validateFormWithJquery(formSelector) {
    if (!(window.jQuery && window.jQuery.fn && window.jQuery.fn.validate)) {
      return true;
    }

    const form = window.jQuery(formSelector);
    if (!form.length) return true;

    const validator = form.data('validator');
    if (!validator) return true;

    return form.valid();
  }

  if (!document.getElementById('cursus-spin-style')) {
    const spinStyle = document.createElement('style');
    spinStyle.id = 'cursus-spin-style';
    spinStyle.textContent = '@keyframes spin { to { transform: rotate(360deg); } }';
    document.head.appendChild(spinStyle);
  }

  return {
    initJqueryValidation,
    resetButton,
    setButtonLoading,
    validateFormWithJquery
  };
}());
