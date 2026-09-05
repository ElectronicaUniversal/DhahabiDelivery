const STORAGE_KEY = 'dhahabi-theme';

export function getIsDark() {
    return localStorage.getItem(STORAGE_KEY) === 'dark';
}

export function setIsDark(isDark) {
    localStorage.setItem(STORAGE_KEY, isDark ? 'dark' : 'light');
    document.documentElement.classList.toggle('dark', isDark);
}

export function applyStoredTheme() {
    document.documentElement.classList.toggle('dark', getIsDark());
}
