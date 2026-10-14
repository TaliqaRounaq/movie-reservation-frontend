(() => {
    const form = document.querySelector('.movie-search-form');
    const search = form?.querySelector('input[name="q"]');
    let searchDelay;

    search?.addEventListener('input', () => {
        const query = search.value.trim();
        clearTimeout(searchDelay);

        searchDelay = setTimeout(() => {
            if (!query) {
                window.location.assign('/Home');
                return;
            }
            const parameters = new URLSearchParams(new FormData(form));
            parameters.set('q', query);
            window.location.assign(`${form.action}?${parameters.toString()}`);
        }, 250);
    });
})();

(() => {
    function populateIndex() {
        const recommendedSection = document.getElementById('recommended-section');
        recommendedSection?.classList.toggle('d-none', !window.CineBookAuth?.isLoggedIn);
       
    }
    populateIndex();
})();