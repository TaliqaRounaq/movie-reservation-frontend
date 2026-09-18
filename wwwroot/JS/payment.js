function getCurrentUserId() {
  try {
    const user = JSON.parse(localStorage.getItem('cinego-demo-user') || 'null');
    return encodeURIComponent(user?.email?.trim().toLowerCase() || 'guest');
  } catch {
    return 'guest';
  }
}

function getShowKey() {
  const values = ['HallId', 'MovieName', 'TheatreName', 'ShowDate', 'ShowTime']
    .map(id => document.getElementById(id)?.value || '');
  return values.map(value => encodeURIComponent(value.trim().toLowerCase())).join('|');
}

function getBookedSeatsStorageKey() {
  return `cinebook_booked_seats_${getShowKey()}`;
}

function reserveSeatsForCurrentUser() {
  const userId = getCurrentUserId();
  const selectedSeatIds = (document.getElementById('Seats')?.value || '')
    .split(',')
    .map(seat => seat.trim())
    .filter(Boolean);

  let bookedSeats;
  try {
    bookedSeats = JSON.parse(localStorage.getItem(getBookedSeatsStorageKey()) || '[]');
    if (!Array.isArray(bookedSeats)) bookedSeats = [];
  } catch {
    bookedSeats = [];
  }

  const reservationsBySeat = new Map(bookedSeats.map(booking => [booking.seatId, booking]));
  if (selectedSeatIds.some(seatId => {
    const reservation = reservationsBySeat.get(seatId);
    return reservation && reservation.userId !== userId;
  })) {
    return 'One or more of these seats were just booked. Please choose different seats.';
  }

  const reservations = selectedSeatIds
    .filter(seatId => !reservationsBySeat.has(seatId))
    .map(seatId => ({ seatId, userId }));
  localStorage.setItem(getBookedSeatsStorageKey(), JSON.stringify([...bookedSeats, ...reservations]));
  localStorage.removeItem(`cinebook_selected_seats_${userId}_${getShowKey()}`);
  return '';
}

document.getElementById('payment-form')?.addEventListener('submit', function (event) {
  const name = document.getElementById('card-name');
  const card = document.getElementById('card-number');
  const error = document.getElementById('payment-error');
  const digits = card.value.replace(/\s/g, '');
  if (!name.value.trim() || !/^\d{12,19}$/.test(digits)) {
    event.preventDefault();
    error.textContent = 'Enter a name and a card number with 12 to 19 digits.';
    return;
  }

  const reservationError = reserveSeatsForCurrentUser();
  if (reservationError) {
    event.preventDefault();
    error.textContent = reservationError;
  }
});
