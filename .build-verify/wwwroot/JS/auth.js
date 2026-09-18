function showError(message) { document.getElementById('auth-error').textContent = message; }

const accountsStorageKey = 'cinego-demo-accounts';
const activeUserStorageKey = 'cinego-demo-user';

function getAccounts() {
  try {
    const accounts = JSON.parse(localStorage.getItem(accountsStorageKey) || '[]');
    if (Array.isArray(accounts)) return accounts;
  } catch {
    localStorage.removeItem(accountsStorageKey);
  }

  // Preserve the account created before multi-account local storage was added.
  try {
    const legacyUser = JSON.parse(localStorage.getItem(activeUserStorageKey) || 'null');
    return legacyUser?.email ? [{ name: legacyUser.name, email: legacyUser.email, password: null }] : [];
  } catch {
    return [];
  }
}

function saveAccounts(accounts) {
  localStorage.setItem(accountsStorageKey, JSON.stringify(accounts));
}

function getReturnUrl() {
  const returnUrl = new URLSearchParams(window.location.search).get('returnUrl');
  return returnUrl && returnUrl.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/';
}


document.getElementById('register-form')?.addEventListener('submit', (event) => {
  event.preventDefault();
  const name = document.getElementById('register-name').value.trim();
  const email = document.getElementById('register-email').value.trim();
  const password = document.getElementById('register-password').value;
  const confirm = document.getElementById('register-confirm').value;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailRegex.test(email)) {
        return showError('Enter a valid email address');
    }

  if (!name || password.length < 6) return showError('Enter your name,  and a password with at least 6 characters.');
  if (password !== confirm) return showError('Passwords do not match.');
  const accounts = getAccounts();
  if (accounts.some(account => account.email.toLowerCase() === email.toLowerCase())) {
    return showError('An account with this email already exists. Please log in.');
  }

  accounts.push({ name, email, password });
  saveAccounts(accounts);
  localStorage.setItem(activeUserStorageKey, JSON.stringify({ name, email }));
  window.location.href = getReturnUrl();
});

document.getElementById('login-form')?.addEventListener('submit', (event) => {
  event.preventDefault();
  const email = document.getElementById('login-email').value.trim();
  const password = document.getElementById('login-password').value;
  if (!email || !password) return showError('Enter your email and password.');
  const account = getAccounts().find(candidate => candidate.email.toLowerCase() === email.toLowerCase());
  if (!account || (account.password !== null && account.password !== password)) {
    return showError('No matching demo account found. Register first.');
  }

  localStorage.setItem(activeUserStorageKey, JSON.stringify({ name: account.name, email: account.email }));
  window.location.href = getReturnUrl();
});
