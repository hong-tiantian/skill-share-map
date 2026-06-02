// Global keyboard shortcut wiring for the AI command bar (⌘K / Ctrl+K, Esc).
window.ssmCommandBar = {
    ref: null,
    _bound: false,

    register: function (dotNetRef) {
        this.ref = dotNetRef;
        if (this._bound) return;
        this._bound = true;
        document.addEventListener('keydown', function (e) {
            const isK = e.key === 'k' || e.key === 'K';
            if ((e.metaKey || e.ctrlKey) && isK) {
                e.preventDefault();
                if (window.ssmCommandBar.ref)
                    window.ssmCommandBar.ref.invokeMethodAsync('ToggleFromJs');
            } else if (e.key === 'Escape') {
                if (window.ssmCommandBar.ref)
                    window.ssmCommandBar.ref.invokeMethodAsync('EscapeFromJs');
            }
        });
    },

    unregister: function () {
        this.ref = null;
    },

    focusInput: function (id) {
        setTimeout(function () {
            const el = document.getElementById(id);
            if (el) { el.focus(); el.select && el.select(); }
        }, 60);
    }
};
