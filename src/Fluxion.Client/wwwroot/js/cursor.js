// ═══ nuclear fix for cursor flash — Round 6 ═══
(function() {
    // Only enable on non-touch devices
    if (!window.matchMedia('(hover: hover)').matches) return;

    var dot = document.getElementById('cursor-dot');
    var shown = false;

    if (dot) {
        // Ensure it's hidden initially (redundant with CSS/HTML but safe)
        dot.style.display = 'none';
        dot.style.opacity = '0.9'; // prepare opacity for when shown
    }

    // Only reveal on actual mouse movement
    document.addEventListener('mousemove', function handler(e) {
        if (dot && !shown) {
            shown = true;
            dot.style.display = 'block';
        }
        
        // Follow cursor immediately
        if (dot) {
            dot.style.transform = `translate(${e.clientX - 4}px, ${e.clientY - 4}px) scale(${dot.dataset.scale || 1})`;
        }
    }, { passive: true });

    // Hover effects
    function addHoverListeners() {
        if (!dot) return;
        document.querySelectorAll(
            'button, a, .glass-card, .card, .step-card, .sidebar-card, input, .theme-toggle-wrapper, .nav-item'
        ).forEach(el => {
            if (el.dataset.cursorbound) return;
            el.dataset.cursorbound = 'true';
            
            el.addEventListener('mouseenter', () => {
                dot.style.setProperty('background', '#B066FF', 'important');
                dot.dataset.scale = '2';
            });
            el.addEventListener('mouseleave', () => {
                dot.style.setProperty('background', '#00D4FF', 'important');
                dot.dataset.scale = '1';
            });
        });
    }
    
    // Run after Blazor renders
    setTimeout(addHoverListeners, 1000);
    // Re-run periodically for dynamic content
    setInterval(addHoverListeners, 3000);
})();
