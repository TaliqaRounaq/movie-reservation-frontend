function renderShowtimes(availability, movieTitle) {
    const container = document.getElementById('showtimesContainer');
    if (!container) return;

    if (!availability || availability.length === 0) {
        container.innerHTML = '<p class="text-secondary">No showtimes available in this city yet.</p>';
        return;
    }

    container.innerHTML = '';

    availability.forEach(theatre => {
        const card = document.createElement('div');
        card.className = 'cb-showtime-card';

        const timesHtml = theatre.times.map(slot => {
            const url = `/Seats/SeatCount?showtimeId=${encodeURIComponent(slot.showId)}`;
            return `<a class="cb-time-pill" href="${url}">${slot.time}</a>`;
        }).join('');

        card.innerHTML = `
            <div class="cb-showtime-head">
                <span class="cb-showtime-name">${theatre.theatreName}</span>
                <span class="cb-showtime-date">${theatre.date}</span>
            </div>
            <div class="cb-showtime-times">${timesHtml}</div>
        `;

        container.appendChild(card);
    });
}

function loadShowtimesForCity(movieId, movieTitle, cityId) {
    fetch(`/Movies/GetShowtimesByCity?movieId=${movieId}&cityId=${cityId}`)
        .then(response => response.json())
        .then(availability => renderShowtimes(availability, movieTitle))
        .catch(err => console.error('Failed to load showtimes:', err));
}

document.addEventListener('cinego:cityChanged', function (e) {
    const container = document.getElementById('showtimesContainer');
    if (!container) return;

    loadShowtimesForCity(container.dataset.movieId, container.dataset.movieTitle, e.detail.cityId);
});