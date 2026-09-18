const seats = document.querySelectorAll('.seat');
const bookingContext = document.getElementById('booking-context');
const seatLimit = parseInt(bookingContext?.dataset.seatLimit || '1', 10);

// Browsers can restore the seat page from their back/forward cache. Reloading
// that restored page ensures seats booked in another request are fetched again.
window.addEventListener('pageshow', function (event) {
    if (event.persisted) {
        window.location.reload();
    }
});

function updateSummary() {
    const selectedSeats = document.querySelectorAll('.seat.selected');

    let total = 0;
    let seatNames = [];

    selectedSeats.forEach(function (seat) {
        const price = parseFloat(seat.getAttribute('data-price')) || 0;
        const seatId = seat.getAttribute('data-seat-id');

        total += price;
        seatNames.push(seatId);
    });

    document.getElementById('total-price').textContent = total;
    document.getElementById('selected-seats-list').textContent =
        seatNames.length > 0 ? seatNames.join(', ') : 'None';
}

async function callSeatAction(action, showSeatId) {
    const showtimeId = document.getElementById('booking-context')?.dataset.showtimeId;
    const response = await fetch(`/Seats/${action}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ showSeatId: parseInt(showSeatId), showtimeId: parseInt(showtimeId) })
    });
    return response.json();
}

seats.forEach(function (seat) {
    seat.addEventListener('click', async function () {
        if (seat.classList.contains('booked') || seat.disabled) {
            return;
        }

        const showSeatId = seat.getAttribute('data-show-seat-id');
        const isCurrentlySelected = seat.classList.contains('selected');

        if (!isCurrentlySelected && document.querySelectorAll('.seat.selected').length >= seatLimit) {
            alert(`You can select up to ${seatLimit} seat${seatLimit === 1 ? '' : 's'}.`);
            return;
        }

        const action = isCurrentlySelected ? 'Release' : 'Reserve';

        seat.disabled = true; // double-click rokne ke liye, response aane tak

        try {
            const result = await callSeatAction(action, showSeatId);

            if (result.success) {
                seat.classList.toggle('selected');
                seat.setAttribute('aria-pressed', seat.classList.contains('selected').toString());
            } else {
                alert(result.message || 'This seat is no longer available.');
                if (action === 'Reserve') {
                    seat.classList.add('booked');
                }
            }
        } catch (err) {
            alert('Something went wrong. Please try again.');
        } finally {
            seat.disabled = seat.classList.contains('booked');
            updateSummary();
        }
    });
});
function goToSummary() {
    const selectedSeats = document.querySelectorAll('.seat.selected');

    if (selectedSeats.length !== seatLimit) {
        alert(`Please select exactly ${seatLimit} seat${seatLimit === 1 ? '' : 's'}.`);
        return;
    }

    const showSeatIds = [];
    selectedSeats.forEach(function (seat) {
        showSeatIds.push(seat.getAttribute('data-show-seat-id'));
    });

    const context = document.getElementById('booking-context');
    const showtimeId = context?.dataset.showtimeId || '';

    const params = new URLSearchParams({ showtimeId, showSeatIds: showSeatIds.join(',') });
    window.location.href = `/Booking/Summary?${params.toString()}`;
}

document.getElementById('continue-btn')?.addEventListener('click', goToSummary);
updateSummary();
