// Fluxion Kinetic Engine v5 — scroll reveals, 3D tilt, cursor glow, hljs
document.addEventListener("DOMContentLoaded", () => { initKineticEffects(); });
window.initializeKinetic = () => { setTimeout(initKineticEffects, 120); };

function initKineticEffects() {
    initScrollReveal();
    initCardTilt();
    initCursorGlow();
    if (window.fluxionCursor) window.fluxionCursor.rebind();
    if (window.fluxionAnimations) window.fluxionAnimations.highlightCode();
}

function initScrollReveal() {
    const obs = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) { e.target.classList.add('revealed'); obs.unobserve(e.target); }
        });
    }, { threshold: 0.07 });
    document.querySelectorAll('.bento-card, .glass-card, .step-card, .stat-card, .right-sidebar-card, .heading-xl, .hero-section > *, .graph-node')
        .forEach(el => {
            if (!el.classList.contains('reveal-aware')) {
                el.classList.add('reveal-aware');
                obs.observe(el);
            }
        });
}

function initCardTilt() {
    document.querySelectorAll('.bento-card, .glass-card, .step-card, .stat-card').forEach(card => {
        if (card.dataset.tiltBound) return;
        card.dataset.tiltBound = '1';
        card.addEventListener('mousemove', (e) => {
            const r = card.getBoundingClientRect();
            const x = e.clientX - r.left, y = e.clientY - r.top;
            const cx = r.width / 2, cy = r.height / 2;
            const tX = ((y - cy) / cy) * -4, tY = ((x - cx) / cx) * 4;
            card.style.transform = `perspective(900px) rotateX(${tX}deg) rotateY(${tY}deg) scale3d(1.015,1.015,1.015)`;
            card.style.transition = 'transform 0.1s ease-out';
            let g = card.querySelector('.card-glare');
            if (!g) { g = document.createElement('div'); g.className = 'card-glare'; card.appendChild(g); }
            g.style.background = `radial-gradient(circle at ${x}px ${y}px, rgba(255,255,255,0.08) 0%, transparent 65%)`;
            g.style.opacity = '1';
        });
        card.addEventListener('mouseleave', () => {
            card.style.transform = '';
            card.style.transition = 'transform 0.5s ease-out';
            const g = card.querySelector('.card-glare');
            if (g) g.style.opacity = '0';
        });
    });
}

function initCursorGlow() {
    let glow = document.getElementById('cursor-glow');
    if (!glow) {
        glow = document.createElement('div');
        glow.id = 'cursor-glow';
        document.body.appendChild(glow);
    }
    document.addEventListener('mousemove', (e) => {
        requestAnimationFrame(() => {
            glow.style.left = e.clientX + 'px';
            glow.style.top = e.clientY + 'px';
        });
    });
}
