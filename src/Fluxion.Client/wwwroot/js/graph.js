window.fluxionGraph = window.fluxionGraph || {};
window.fluxionGraph._raf = null;

window.fluxionGraph.draw = function (nodes, mastery, canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !nodes || !nodes.length) return;
    if (this._raf) cancelAnimationFrame(this._raf);

    function getMastery(masteryObj, nodeId) {
        if (!masteryObj || !nodeId) return 0;
        if (masteryObj[nodeId] !== undefined) return masteryObj[nodeId];
        var lower = String(nodeId).toLowerCase();
        if (masteryObj[lower] !== undefined) return masteryObj[lower];
        var upper = String(nodeId).toUpperCase();
        if (masteryObj[upper] !== undefined) return masteryObj[upper];
        return 0;
    }

    const ctx = canvas.getContext('2d');
    const W = canvas.width = canvas.offsetWidth || 800;
    const H = canvas.height = canvas.offsetHeight || 500;

    const maxDiff = Math.max(...nodes.map(n => n.difficultyLevel || 1));
    const colCount = Math.min(maxDiff, 8);
    const colW = W / (colCount + 1);

    const colCounts = {};
    const positioned = nodes.map(n => {
        const col = Math.min(Math.ceil((n.difficultyLevel || 1) / (maxDiff / colCount)), colCount);
        colCounts[col] = (colCounts[col] || 0) + 1;
        return { ...n, col, rowIdx: colCounts[col] };
    });
    const colTotals = {};
    positioned.forEach(n => { colTotals[n.col] = colCounts[n.col]; });
    const placed = positioned.map(n => ({
        ...n,
        x: n.col * colW,
        y: (n.rowIdx / (colTotals[n.col] + 1)) * H,
        phase: Math.random() * Math.PI * 2,
        masteryScore: getMastery(mastery, n.id)
    }));

    let hoveredNode = null;
    let t = 0;

    canvas.onmousemove = (e) => {
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;
        hoveredNode = placed.find(n => {
            const dx = n.x - mx, dy = n.y - my;
            return Math.sqrt(dx * dx + dy * dy) < 22;
        }) || null;
    };
    canvas.onmouseleave = () => { hoveredNode = null; };

    function wrapText(ctx, text, x, y, maxWidth, lineHeight) {
        const words = text.split(' ');
        let line = '';
        let lines = [];
        for (let n = 0; n < words.length; n++) {
            let testLine = line + words[n] + ' ';
            let metrics = ctx.measureText(testLine);
            if (metrics.width > maxWidth && n > 0) {
                lines.push(line);
                line = words[n] + ' ';
            } else {
                line = testLine;
            }
        }
        lines.push(line);
        // Max 2 lines
        if (lines.length > 2) {
            lines = lines.slice(0, 2);
            lines[1] = lines[1].trim() + '…';
        }
        lines.forEach((l, i) => ctx.fillText(l.trim(), x, y + (i * lineHeight)));
    }

    function getNodeColor(n) {
        const isDark = document.documentElement.getAttribute('data-theme') !== 'light';
        if (n.masteryScore >= 0.7) return '#10B981'; // Mastered: Emerald
        if (n.masteryScore > 0) return '#7C3AED';   // In Progress: Violet
        if (n.difficultyLevel <= 2) return '#94A3B8'; // Available: Slate
        return isDark ? '#334155' : '#CBD5E1'; // Locked: Dark/Light Slate
    }

    function loop() {
        ctx.clearRect(0, 0, W, H);
        t += 0.012;

        // Draw connections
        for (let i = 1; i < placed.length; i++) {
            const a = placed[i - 1], b = placed[i];
            if (b.col !== a.col && b.col !== a.col + 1) continue;
            const cpx = (a.x + b.x) / 2;
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.bezierCurveTo(cpx, a.y, cpx, b.y, b.x, b.y);
            ctx.strokeStyle = 'rgba(255,255,255,0.06)';
            ctx.lineWidth = 1.5;
            ctx.stroke();
        }

        placed.forEach(n => {
            const breathe = Math.sin(t + n.phase) * 2;
            const r = 16 + breathe + (hoveredNode === n ? 6 : 0);
            const color = getNodeColor(n);
            const isHovered = hoveredNode === n;

            // Glow
            ctx.beginPath();
            ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
            ctx.shadowBlur = isHovered ? 30 : (n.masteryScore >= 0.7 ? 16 : 8);
            ctx.shadowColor = color;
            ctx.fillStyle = color;
            ctx.fill();

            // Internal Circle
            ctx.shadowBlur = 0;
            ctx.beginPath();
            ctx.arc(n.x, n.y, r * 0.7, 0, Math.PI * 2);
            ctx.fillStyle = 'rgba(0,0,0,0.2)';
            ctx.fill();

            // Labels
            const isDark = document.documentElement.getAttribute('data-theme') !== 'light';
            ctx.fillStyle = isHovered ? (isDark ? '#FFFFFF' : '#1E293B') : (isDark ? 'rgba(241,245,249,0.85)' : '#334155');
            ctx.font = `${isHovered ? 600 : 500} 11px "Plus Jakarta Sans", sans-serif`;
            ctx.textAlign = 'center';
            
            // Text shadow for readability against graph connection lines
            ctx.shadowColor = isDark ? 'rgba(0,0,0,0.8)' : 'rgba(255,255,255,0.9)';
            ctx.shadowBlur = 3;
            wrapText(ctx, n.title, n.x, n.y + r + 18, 100, 14);
            ctx.shadowBlur = 0;

            if (isHovered) {
                const pct = Math.round(n.masteryScore * 100);
                const tipW = 160, tipH = 50;
                const tx = Math.min(Math.max(8, n.x - tipW / 2), W - tipW - 8);
                const ty = n.y - r - tipH - 12;
                
                ctx.fillStyle = 'rgba(15, 23, 42, 0.95)';
                ctx.strokeStyle = 'rgba(0, 212, 255, 0.2)';
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.roundRect(tx, ty, tipW, tipH, 10);
                ctx.fill(); ctx.stroke();
                
                ctx.fillStyle = '#FFFFFF';
                ctx.font = '600 12px "Plus Jakarta Sans", sans-serif';
                ctx.textAlign = 'left';
                ctx.fillText(n.title.length > 20 ? n.title.substring(0, 19) + '…' : n.title, tx + 12, ty + 20);
                
                ctx.fillStyle = '#94A3B8';
                ctx.font = '500 11px sans-serif';
                ctx.fillText(`Mastery: ${pct}%`, tx + 12, ty + 38);
                ctx.fillText(`Level: ${n.difficultyLevel}`, tx + 100, ty + 38);
            }
        });

        window.fluxionGraph._raf = requestAnimationFrame(loop);
    }
    canvas.style.opacity = '1';
    loop();
};
