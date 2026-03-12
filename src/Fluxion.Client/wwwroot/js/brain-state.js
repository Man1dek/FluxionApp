// ═══ BUG 5 FIX: Complete brain-state.js rewrite ═══
(function() {
    window.fluxionBrainState = window.fluxionBrainState || {};
    
    let animationId = null;
    let currentLoad = 0.3;
    let targetLoad = 0.3;
    
    function getColor(load) {
        if (load < 0.3) return '#00D4FF';
        if (load < 0.6) return '#7C3AED';
        if (load < 0.8) return '#F59E0B';
        return '#EF4444';
    }
    
    function drawBlob(canvas, load, time) {
        const ctx = canvas.getContext('2d');
        const w = canvas.width;
        const h = canvas.height;
        const cx = w / 2;
        const cy = h / 2;
        const baseR = Math.min(w, h) * 0.30;
        
        const maxAmp = Math.min(cx, cy) - baseR - 5;
        const amp = Math.min(load * baseR * 0.5, maxAmp);
        const speed = 0.5 + load * 2;
        const points = 8;
        const color = getColor(load);
        
        ctx.clearRect(0, 0, w, h);
        
        // Draw blob
        ctx.beginPath();
        for (let i = 0; i <= points; i++) {
            const angle = (i / points) * Math.PI * 2;
            const noise = Math.sin(angle * 3 + time * speed) 
                        * amp * 0.5
                        + Math.sin(angle * 5 - time * speed * 0.7) 
                        * amp * 0.3;
            const r = baseR + noise;
            const x = cx + Math.cos(angle) * r;
            const y = cy + Math.sin(angle) * r;
            
            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        }
        ctx.closePath();
        
        // Gradient fill
        const gradient = ctx.createRadialGradient(
            cx, cy, 0, cx, cy, baseR + amp);
        gradient.addColorStop(0, color + 'CC');
        gradient.addColorStop(1, color + '33');
        ctx.fillStyle = gradient;
        
        // Glow
        ctx.shadowBlur = 20;
        ctx.shadowColor = color;
        ctx.fill();
        ctx.shadowBlur = 0;
    }
    
    window.fluxionBrainState.draw = function(canvasId, load) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) {
            // Retry after a short delay (Blazor may not have rendered the element yet)
            setTimeout(function() {
                var retryCanvas = document.getElementById(canvasId);
                if (retryCanvas) {
                    startAnimation(retryCanvas, canvasId, load);
                }
            }, 300);
            return;
        }
        startAnimation(canvas, canvasId, load);
    };

    function startAnimation(canvas, canvasId, load) {
        currentLoad = load || 0.3;
        targetLoad = currentLoad;
        canvas.style.opacity = '1';
        
        if (animationId) cancelAnimationFrame(animationId);
        
        let time = 0;
        function animate() {
            // Lerp toward target
            currentLoad += (targetLoad - currentLoad) * 0.05;
            time += 0.016;
            
            // Re-fetch canvas in case it was re-rendered by Blazor
            const currentCanvas = document.getElementById(canvasId);
            if (!currentCanvas) return; // Stop if canvas is gone
            
            drawBlob(currentCanvas, currentLoad, time);
            animationId = requestAnimationFrame(animate);
        }
        animate();
    }
    
    window.fluxionBrainState.update = function(canvasId, newLoad) {
        targetLoad = newLoad || 0.3;
        const canvas = document.getElementById(canvasId);
        if (canvas && canvas.style.opacity === '0') {
            window.fluxionBrainState.draw(canvasId, newLoad);
        }
    };
})();
