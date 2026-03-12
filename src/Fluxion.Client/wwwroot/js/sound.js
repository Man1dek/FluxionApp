window.fluxionSound = window.fluxionSound || {};

fluxionSound._enabled = 
    localStorage.getItem('fluxion_sound') !== 'off';

fluxionSound.toggle = function() {
    fluxionSound._enabled = !fluxionSound._enabled;
    localStorage.setItem('fluxion_sound', 
        fluxionSound._enabled ? 'on' : 'off');
    return fluxionSound._enabled;
};

fluxionSound.setEnabled = function(enabled) {
    fluxionSound._enabled = enabled;
    localStorage.setItem('fluxion_sound', enabled ? 'on' : 'off');
};

fluxionSound.init = function(enabled) {
    fluxionSound.setEnabled(enabled);
};

fluxionSound.isEnabled = function() {
    return fluxionSound._enabled;
};

fluxionSound.play = function(type) {
    if (!fluxionSound._enabled) return;
    try {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (!AudioCtx) return;
        const ctx = new AudioCtx();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        
        const sounds = {
            correct:  { freq: 523, duration: 0.2, type: 'sine' },
            wrong:    { freq: 150, duration: 0.15, type: 'square' },
            advance:  { freq: 300, duration: 0.1, type: 'sine' },
            mastered: { freq: 440, duration: 0.4, type: 'sine' },
            xp:       { freq: 880, duration: 0.08, type: 'sine' }
        };
        
        const s = sounds[type] || sounds.advance;
        osc.type = s.type;
        osc.frequency.setValueAtTime(s.freq, ctx.currentTime);
        gain.gain.setValueAtTime(0.1, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(
            0.001, ctx.currentTime + s.duration);
        osc.start(ctx.currentTime);
        osc.stop(ctx.currentTime + s.duration);
    } catch(e) { /* silently fail */ }
};

// Global click listener for buttons
document.addEventListener('click', function(e) {
    if (!fluxionSound._enabled) return;
    
    // Check if clicked element or its parent is a button or has button-like classes
    let target = e.target;
    while (target && target !== document) {
        if (target.tagName === 'BUTTON' || 
            (target.tagName === 'A' && target.classList.contains('btn')) ||
            target.classList.contains('nav-item')) {
            // Play a very subtle high-frequency "tick" for general clicks
            try {
                const AudioCtx = window.AudioContext || window.webkitAudioContext;
                if (!AudioCtx) return;
                const ctx = new AudioCtx();
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.connect(gain);
                gain.connect(ctx.destination);
                
                osc.type = 'sine';
                osc.frequency.setValueAtTime(800, ctx.currentTime);
                gain.gain.setValueAtTime(0.02, ctx.currentTime); // Very quiet
                gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.05); // Very short
                osc.start(ctx.currentTime);
                osc.stop(ctx.currentTime + 0.05);
            } catch(e) { /* silently fail */ }
            break;
        }
        target = target.parentNode;
    }
});
