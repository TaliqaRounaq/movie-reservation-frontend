(() => {
    if (!window.CineBookAuth?.isLoggedIn) {
        const returnUrl = `${window.location.pathname}${window.location.search}`;
        window.location.replace(`/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`);
    }
})();