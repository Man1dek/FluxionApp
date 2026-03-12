(function() {
    // Apply theme immediately on script load to prevent flash
    var saved = localStorage.getItem('fluxion_theme') || 'dark';
    document.documentElement.setAttribute('data-theme', saved);
    
    window.fluxionTheme = {
        apply: function(isDark) {
            var theme = isDark ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', theme);
            localStorage.setItem('fluxion_theme', theme);
        },
        get: function() {
            return document.documentElement.getAttribute('data-theme') || 'dark';
        }
    };
})();
