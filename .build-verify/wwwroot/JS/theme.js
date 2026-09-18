(function () {
    const root = document.documentElement;
    const toggleBtn = document.getElementById('cb-theme-toggle');
    const icon = document.getElementById('cb-theme-icon');

    function applyTheme(theme) {
        root.setAttribute('data-theme', theme);
        if (icon) {
            icon.className = theme === 'dark' ? 'bi bi-brightness-high' : 'bi bi-moon';
        }
    }

    const savedTheme = localStorage.getItem('cinebook-theme') || 'dark';
    applyTheme(savedTheme);

    toggleBtn?.addEventListener('click', function () {
        const current = root.getAttribute('data-theme') || 'dark';
        const next = current === 'dark' ? 'light' : 'dark';
        applyTheme(next);
        localStorage.setItem('cinebook-theme', next);
    });
})();
