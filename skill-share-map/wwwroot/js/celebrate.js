// Celebration helpers for SkillShareMap gamification (no external deps)
window.ssmCelebrate = {

    // Animate a number element from `from` to `to` over durationMs with easing.
    countUp: function (elementId, from, to, durationMs) {
        const el = document.getElementById(elementId);
        if (!el) return;
        durationMs = durationMs || 1200;
        const start = performance.now();
        const diff = to - from;
        function frame(now) {
            const t = Math.min((now - start) / durationMs, 1);
            const eased = 1 - Math.pow(1 - t, 3); // easeOutCubic
            el.textContent = Math.round(from + diff * eased);
            if (t < 1) requestAnimationFrame(frame);
            else el.textContent = to;
        }
        requestAnimationFrame(frame);
    },

    // Full-screen confetti burst from the center.
    confetti: function (durationMs) {
        durationMs = durationMs || 2600;
        let canvas = document.getElementById('ssm-confetti-canvas');
        if (!canvas) {
            canvas = document.createElement('canvas');
            canvas.id = 'ssm-confetti-canvas';
            canvas.style.cssText =
                'position:fixed;inset:0;width:100%;height:100%;pointer-events:none;z-index:4000;';
            document.body.appendChild(canvas);
        }
        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        const colors = ['#4a75b0', '#a0e548', '#f6c445', '#e45f2b', '#7b61c1', '#ffffff'];
        const cx = canvas.width / 2;
        const cy = canvas.height * 0.42;
        const N = 170;
        const parts = [];
        for (let i = 0; i < N; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = 6 + Math.random() * 12;
            parts.push({
                x: cx, y: cy,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed - 5,
                size: 5 + Math.random() * 8,
                color: colors[(Math.random() * colors.length) | 0],
                rot: Math.random() * Math.PI,
                vrot: (Math.random() - 0.5) * 0.35
            });
        }

        const start = performance.now();
        function frame(now) {
            const elapsed = now - start;
            const life = Math.max(0, 1 - elapsed / durationMs);
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            for (const p of parts) {
                p.vy += 0.28;   // gravity
                p.vx *= 0.99;
                p.x += p.vx;
                p.y += p.vy;
                p.rot += p.vrot;
                ctx.save();
                ctx.globalAlpha = life;
                ctx.translate(p.x, p.y);
                ctx.rotate(p.rot);
                ctx.fillStyle = p.color;
                ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 0.6);
                ctx.restore();
            }
            if (elapsed < durationMs) requestAnimationFrame(frame);
            else ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
        requestAnimationFrame(frame);
    },

    // Scroll a container to its bottom (used by the AI companion chat).
    scrollToBottom: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) el.scrollTop = el.scrollHeight;
    },

    // localStorage helpers for level-up detection (per user).
    getLastSeenXp: function (userId) {
        const v = localStorage.getItem('ssm_lastSeenXp_' + userId);
        return v === null ? -1 : parseInt(v, 10);
    },
    setLastSeenXp: function (userId, xp) {
        localStorage.setItem('ssm_lastSeenXp_' + userId, xp);
    }
};
