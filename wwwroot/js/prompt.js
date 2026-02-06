// === Unified Prompt & Sound ===
// Requires SweetAlert2 (included in _Layout.cshtml)
// Provides: showSuccess, showError, showInfo, playDing, confirmAsync

(function () {
    // Minimal chime using Web Audio API (no external files)
    function playDing(volume = 0.25) {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const o = ctx.createOscillator();
            const g = ctx.createGain();
            o.connect(g);
            g.connect(ctx.destination);
            const now = ctx.currentTime;
            // Simple two-note chime
            o.type = "sine";
            o.frequency.setValueAtTime(880, now);
            g.gain.setValueAtTime(volume, now);
            o.start(now);
            o.frequency.exponentialRampToValueAtTime(1760, now + 0.15);
            g.gain.exponentialRampToValueAtTime(0.0001, now + 0.35);
            o.stop(now + 0.4);
        } catch (e) {
            // ignore if autoplay blocked
        }
    }

    function baseFire(opts, withDing = true) {
        if (withDing) playDing();
        return Swal.fire(Object.assign({
            toast: false,
            position: 'center',
            showConfirmButton: false,
            timer: 1500,
            timerProgressBar: true,
            allowEscapeKey: true,
            allowOutsideClick: true
        }, opts));
    }

    window.showSuccess = function (message, withDing = true) {
        return baseFire({ icon: 'success', title: message }, withDing);
    };

    window.showError = function (message, withDing = true) {
        // Keep longer so users can read
        return baseFire({ icon: 'error', title: 'Error', text: message, timer: 2500 }, withDing);
    };

    window.showInfo = function (message, withDing = false) {
        return baseFire({ icon: 'info', title: message }, withDing);
    };

    // Promise-based confirm dialog (non-blocking)
    window.confirmAsync = function (message, confirmText = 'OK', cancelText = 'Cancel') {
        playDing(0.15);
        return Swal.fire({
            icon: 'question',
            title: message,
            showCancelButton: true,
            confirmButtonText: confirmText,
            cancelButtonText: cancelText,
        }).then(r => !!r.isConfirmed);
    };

    // Optional: override window.alert to use SweetAlert2 (keeps legacy code working)
    // Note: native alert is synchronous; this is async visually but fine for most cases.
    const nativeAlert = window.alert;
    window.alert = function (message) {
        showInfo(String(message), true);
    };

    // Avoid double-popups: guard against duplicate calls within 500ms
    let lastFireAt = 0;
    const oldShowSuccess = window.showSuccess;
    window.showSuccess = function() {
        const now = Date.now();
        if (now - lastFireAt < 500) return;
        lastFireAt = now;
        return oldShowSuccess.apply(this, arguments);
    };
})();