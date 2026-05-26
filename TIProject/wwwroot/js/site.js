window.toggleDarkMode = function () {
    var body = document.body;
    var isDark = !body.classList.contains('dark-mode');

    if (isDark) {
        body.classList.add('dark-mode');
    } else {
        body.classList.remove('dark-mode');
    }

    // update all icons with id 'darkModeIcon' (some pages duplicate the id)
    try {
        var icons = document.querySelectorAll('#darkModeIcon');
        icons.forEach(function (icon) {
            icon.classList.remove('fa-moon');
            icon.classList.remove('fa-sun');
            icon.classList.add(isDark ? 'fa-sun' : 'fa-moon');
        });
    } catch (e) {
        // ignore
    }

    try {
        localStorage.setItem('darkMode', isDark ? 'true' : 'false');
    } catch (e) {
        // ignore storage errors
    }

    return isDark;
};

window.initDarkMode = function () {
    var saved = null;
    try {
        saved = localStorage.getItem('darkMode');
    } catch (e) {
        saved = null;
    }
    var isDark = saved === 'true';

    if (isDark) {
        document.body.classList.add('dark-mode');
    } else {
        document.body.classList.remove('dark-mode');
    }

    try {
        var icons = document.querySelectorAll('#darkModeIcon');
        icons.forEach(function (icon) {
            icon.classList.remove('fa-moon');
            icon.classList.remove('fa-sun');
            icon.classList.add(isDark ? 'fa-sun' : 'fa-moon');
        });
    } catch (e) {
        // ignore
    }

    return isDark;
};