// Subtle cursor-following ambient glow.
// A soft, low-opacity radial blob trails the pointer with easing — barely
// noticeable, just enough to make the diffuse background feel alive.
(function () {
    if (window.__ssmCursorGlow) return;
    window.__ssmCursorGlow = true;

    // Respect reduced-motion and skip on touch (no hover pointer).
    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var coarse = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
    if (reduce || coarse) return;

    var el = document.createElement('div');
    el.className = 'ssm-cursor-glow';
    document.body.appendChild(el);

    var tx = window.innerWidth / 2, ty = window.innerHeight / 2;  // target
    var cx = tx, cy = ty;                                          // current (eased)
    var visible = false;
    var raf = null;

    function onMove(e) {
        tx = e.clientX; ty = e.clientY;
        if (!visible) { visible = true; el.style.opacity = '1'; }
        if (!raf) raf = requestAnimationFrame(tick);
    }

    function tick() {
        // ease toward target for a smooth, lagging trail
        cx += (tx - cx) * 0.12;
        cy += (ty - cy) * 0.12;
        el.style.transform = 'translate3d(' + (cx - 150) + 'px,' + (cy - 150) + 'px,0)';
        if (Math.abs(tx - cx) > 0.5 || Math.abs(ty - cy) > 0.5) {
            raf = requestAnimationFrame(tick);
        } else {
            raf = null;
        }
    }

    window.addEventListener('mousemove', onMove, { passive: true });
    window.addEventListener('mouseout', function (e) {
        if (!e.relatedTarget) { visible = false; el.style.opacity = '0'; }
    });
})();
