function populateDashboardUser() {
    if (!window.CineBookAuth?.isLoggedIn) return;

    const nameEl = document.getElementById('dashboardTitle');
    const emailEl = document.getElementById('dashboardEmail');

    if (nameEl) nameEl.textContent = window.CineBookAuth.name;
    if (emailEl) emailEl.textContent = window.CineBookAuth.email;
    // dashboardAvatar aur indexAvatar ab Razor khud render kar deta hai
    // _Navbar.cshtml mein, isliye yahan set karne ki zaroorat nahi
}

(() => {
    const panel = document.getElementById('dashboardPanel');
    const overlay = document.getElementById('dashboardOverlay');
    const trigger = document.getElementById('avatarBtn');
    const closeButton = document.getElementById('closeDashboardBtn');
    if (!panel || !overlay || !trigger || !closeButton) return;
    const closePanel = () => { panel.classList.remove('open'); overlay.classList.remove('active'); panel.setAttribute('aria-hidden', 'true'); trigger.setAttribute('aria-expanded', 'false'); document.body.classList.remove('dashboard-open'); trigger.focus(); };
    const openPanel = () => { panel.classList.add('open'); overlay.classList.add('active'); panel.setAttribute('aria-hidden', 'false'); trigger.setAttribute('aria-expanded', 'true'); document.body.classList.add('dashboard-open'); closeButton.focus(); };
    const guestPrompt = document.getElementById('guestPrompt');

    function isLoggedIn() {
        return !!window.CineBookAuth?.isLoggedIn;
    }

    function handleTriggerClick() {
        if (isLoggedIn()) {
            openPanel();
        } else if (guestPrompt) {
            guestPrompt.classList.toggle('d-none');
        }
    }

    trigger.addEventListener('click', handleTriggerClick);

    document.addEventListener('click', (event) => {
        if (guestPrompt && !guestPrompt.classList.contains('d-none') &&
            !guestPrompt.contains(event.target) && event.target !== trigger && !trigger.contains(event.target)) {
            guestPrompt.classList.add('d-none');
        }
    });
    closeButton.addEventListener('click', closePanel);
    overlay.addEventListener('click', closePanel);
    document.addEventListener('keydown', (event) => { if (event.key === 'Escape' && panel.classList.contains('open')) closePanel(); });

    populateDashboardUser();
})();

(() => {
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (event) => {
            event.preventDefault();
            window.location.href = '/Account/Logout';
        });
    }
})();