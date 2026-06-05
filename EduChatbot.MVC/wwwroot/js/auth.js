(function () {
    'use strict';

    document.body.classList.add('auth-page-loaded');

    document.querySelectorAll('[data-auth-switch]').forEach(function (link) {
        link.addEventListener('click', function (event) {
            const href = link.getAttribute('href');
            const mode = link.getAttribute('data-auth-mode');
            if (!href || !mode) return;

            event.preventDefault();

            const tabs = document.querySelector('.auth-tabs');
            if (tabs) {
                tabs.classList.remove('mode-login', 'mode-register', 'switching-login', 'switching-register');
                tabs.classList.add('switching-' + mode);
            }

            document.body.classList.add('auth-page-leaving');
            window.setTimeout(function () {
                window.location.href = href;
            }, 190);
        });
    });

    // Password visibility toggle
    var toggleBtn = document.getElementById('passwordToggleBtn');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            var input = toggleBtn.closest('.auth-field-input-wrapper').querySelector('input');
            var eyeOpen = toggleBtn.querySelector('.auth-eye-open');
            var eyeClosed = toggleBtn.querySelector('.auth-eye-closed');

            if (input.type === 'password') {
                input.type = 'text';
                eyeOpen.style.display = 'none';
                eyeClosed.style.display = '';
            } else {
                input.type = 'password';
                eyeOpen.style.display = '';
                eyeClosed.style.display = 'none';
            }
        });
    }
})();
