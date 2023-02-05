'use strict';
const moonIcon = document.getElementById('moon');
const sunIcon = document.getElementById('sun');
const themeButton = document.getElementById('change-mode');

function changeModeIcon(isLight) {
    if (isLight) {
        moonIcon.removeAttribute('hidden');
        sunIcon.setAttribute('hidden', true);
    } else {
        sunIcon.removeAttribute('hidden');
        moonIcon.setAttribute('hidden', true);
    }
}

function switchTheme(isLight) {
    const LSisLight = LS.getItem('KitBlogIsLight') === 'true';
    isLight = isLight ?? !LSisLight;

    changeModeIcon(isLight);

    lightHL1.rel = isLight ? 'stylesheet' : 'stylesheet alternate';
    lightHL2.rel = isLight ? 'stylesheet' : 'stylesheet alternate';
    darkHL1.rel = isLight ? 'stylesheet alternate' : 'stylesheet';
    darkHL2.rel = isLight ? 'stylesheet alternate' : 'stylesheet';

    LS.setItem('KitBlogIsLight', isLight);
}

themeButton.addEventListener('click', () => {
    switchTheme();
});

if (LS.getItem('KitBlogIsLight') === null) {
    switchTheme(
        !(
            window.matchMedia &&
            window.matchMedia('(prefers-color-scheme: dark)').matches
        )
    );
} else {
    switchTheme(LSisLight);
}
