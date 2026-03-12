window.fluxionAnimations = window.fluxionAnimations || {};

window.fluxionAnimations.countUp = function (elementId, target, duration) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const step = target / (duration / 16);
    let cur = 0;
    const t = setInterval(() => {
        cur = Math.min(cur + step, target);
        el.textContent = Math.floor(cur);
        if (cur >= target) clearInterval(t);
    }, 16);
};

window.fluxionAnimations.typeText = function (elementId, text, speed) {
    const el = document.getElementById(elementId);
    if (!el || !text) return;
    el.textContent = '';
    let i = 0;
    const t = setInterval(() => {
        el.textContent += text[i++];
        if (i >= text.length) clearInterval(t);
    }, speed || 18);
};

window.fluxionAnimations.registerFocusKey = function (dotnet) {
    document.addEventListener('keydown', (e) => {
        if ((e.key === 'f' || e.key === 'F') &&
            !['INPUT', 'TEXTAREA'].includes(document.activeElement.tagName)) {
            dotnet.invokeMethodAsync('ToggleFocusMode');
        }
    });
};

window.fluxionAnimations.neuralTransition = function (containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;
    const canvas = document.createElement('canvas');
    canvas.style.cssText = `position:absolute;inset:0;width:100%;height:100%;z-index:50;pointer-events:none;`;
    canvas.width = container.offsetWidth || 600;
    canvas.height = container.offsetHeight || 400;
    container.style.position = 'relative';
    container.appendChild(canvas);
    const ctx = canvas.getContext('2d');
    const nodes = Array.from({ length: 14 }, () => ({
        x: Math.random() * canvas.width,
        y: Math.random() * canvas.height,
        r: 0, maxR: 4 + Math.random() * 4, alpha: 0
    }));
    let frame = 0;
    const total = 60;
    function draw() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        const progress = frame / total;
        nodes.forEach((n, i) => {
            if (i / nodes.length > progress) return;
            n.r = Math.min(n.r + 0.5, n.maxR);
            n.alpha = Math.min(n.alpha + 0.05, 0.9);
            ctx.beginPath();
            ctx.arc(n.x, n.y, n.r, 0, Math.PI * 2);
            ctx.fillStyle = `rgba(0,212,255,${n.alpha})`;
            ctx.shadowBlur = 12; ctx.shadowColor = '#00D4FF';
            ctx.fill();
            if (i > 0) {
                const prev = nodes[i - 1];
                ctx.beginPath();
                ctx.moveTo(prev.x, prev.y);
                ctx.lineTo(n.x, n.y);
                ctx.strokeStyle = `rgba(124,58,237,${n.alpha * 0.5})`;
                ctx.lineWidth = 1;
                ctx.shadowBlur = 6; ctx.shadowColor = '#7C3AED';
                ctx.stroke();
            }
        });
        frame++;
        if (frame < total + 20) {
            requestAnimationFrame(draw);
        } else {
            canvas.style.transition = 'opacity 0.4s';
            canvas.style.opacity = '0';
            setTimeout(() => canvas.remove(), 400);
        }
    }
    draw();
};

window.fluxionAnimations.highlightCode = function () {
    if (window.hljs) {
        document.querySelectorAll('pre code').forEach(block => {
            if (!block.dataset.highlighted) hljs.highlightElement(block);
        });
        document.querySelectorAll('pre').forEach(pre => {
            if (pre.querySelector('.code-copy-btn')) return;
            const code = pre.querySelector('code');
            const btn = document.createElement('button');
            btn.className = 'code-copy-btn';
            btn.textContent = 'Copy';
            btn.onclick = () => {
                navigator.clipboard.writeText(code ? code.textContent : '').then(() => {
                    btn.textContent = 'Copied!';
                    setTimeout(() => btn.textContent = 'Copy', 1500);
                });
            };
            pre.style.position = 'relative';
            pre.appendChild(btn);
        });
    }
};
