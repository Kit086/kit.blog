'use strict'
function changeModeIcon(isLight) {
    const moonIcon = document.getElementById('moon');
    const sunIcon = document.getElementById('sun');
    if (isLight) {
        moonIcon.removeAttribute('hidden');
        sunIcon.setAttribute('hidden', true);
    } else {
        sunIcon.removeAttribute('hidden');
        moonIcon.setAttribute('hidden', true);
    }
}

function switchTheme(isLight) {
    const lightHL1 = document.getElementById('light-css-1');
    const lightHL2 = document.getElementById('light-css-2');
    const darkHL1 = document.getElementById('dark-css-1');
    const darkHL2 = document.getElementById('dark-css-2');

    const LS = window.localStorage;
    const LSisLight = LS.getItem('KitBlogIsLight') === 'true';

    isLight = isLight ?? !LSisLight;

    changeModeIcon(isLight);

    lightHL1.rel = isLight ? 'stylesheet' : 'stylesheet alternate';
    lightHL2.rel = isLight ? 'stylesheet' : 'stylesheet alternate';
    darkHL1.rel = isLight ? 'stylesheet alternate' : 'stylesheet';
    darkHL2.rel = isLight ? 'stylesheet alternate' : 'stylesheet';

    LS.setItem('KitBlogIsLight', isLight);
}

const themeButton = document.getElementById('change-mode');
const LS = window.localStorage;
const LSisLight = LS.getItem('KitBlogIsLight') === 'true';

themeButton.addEventListener('click', () => {
    switchTheme();
});

LS.getItem('KitBlogIsLight') === null
    ? switchTheme(
        !(
            window.matchMedia &&
            window.matchMedia('(prefers-color-scheme: dark)').matches
        )
    )
    : switchTheme(LSisLight);
