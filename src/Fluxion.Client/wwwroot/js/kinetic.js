// Fluxion Kinetic Engine
// Handles scroll reveals, 3D card tilts, and ambient cursor tracking

document.addEventListener("DOMContentLoaded", () => {
    initKineticEffects();
});

// Re-initialize when Blazor navigation occurs
window.initializeKinetic = () => {
    setTimeout(initKineticEffects, 100);
};

function initKineticEffects() {
    initScrollReveal();
    initCardTilt();
    initCursorGlow();
}

// 1. Scroll Reveal using IntersectionObserver
function initScrollReveal() {
    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0.1
    };

    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('revealed');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    // Grab elements we want to reveal
    const revealElements = document.querySelectorAll('.bento-card, .glass-card, .heading-xl, .hero-section > *, .graph-node');
    revealElements.forEach(el => {
        if (!el.classList.contains('reveal-aware')) {
            el.classList.add('reveal-aware');
            observer.observe(el);
        }
    });
}

// 2. 3D Hover Tilt for Cards
function initCardTilt() {
    const cards = document.querySelectorAll('.bento-card, .glass-card');

    cards.forEach(card => {
        card.addEventListener('mousemove', handleHover);
        card.addEventListener('mouseleave', resetHover);
    });

    function handleHover(e) {
        const card = e.currentTarget;
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        const centerX = rect.width / 2;
        const centerY = rect.height / 2;

        // Calculate tilt amounts (max 5 degrees)
        const tiltX = ((y - centerY) / centerY) * -5;
        const tiltY = ((x - centerX) / centerX) * 5;

        card.style.transform = `perspective(1000px) rotateX(${tiltX}deg) rotateY(${tiltY}deg) scale3d(1.02, 1.02, 1.02)`;
        card.style.transition = 'transform 0.1s ease-out';

        // Dynamic glare effect based on cursor
        let glare = card.querySelector('.card-glare');
        if (!glare) {
            glare = document.createElement('div');
            glare.classList.add('card-glare');
            card.appendChild(glare);
        }

        glare.style.background = `radial-gradient(circle at ${x}px ${y}px, rgba(255,255,255,0.4) 0%, rgba(255,255,255,0) 60%)`;
        glare.style.opacity = '1';
    }

    function resetHover(e) {
        const card = e.currentTarget;
        card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)';
        card.style.transition = 'transform 0.5s ease-out';

        const glare = card.querySelector('.card-glare');
        if (glare) {
            glare.style.opacity = '0';
        }
    }
}

// 3. Ambient Cursor Tracking (subtle gradient follows mouse)
function initCursorGlow() {
    let cursorGlow = document.getElementById('cursor-glow');
    if (!cursorGlow) {
        cursorGlow = document.createElement('div');
        cursorGlow.id = 'cursor-glow';
        document.body.appendChild(cursorGlow);
    }

    document.addEventListener('mousemove', (e) => {
        // Request animation frame for smooth tracking
        requestAnimationFrame(() => {
            cursorGlow.style.left = `${e.clientX}px`;
            cursorGlow.style.top = `${e.clientY}px`;
        });
    });
}
