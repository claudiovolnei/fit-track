window.fitTrack = window.fitTrack || {};

// Tiny alarm used when the rest timer reaches zero. It is generated with the Web Audio API so
// there is no asset to ship, and it degrades gracefully when the browser blocks autoplay.
(function () {
    var context = null;

    function getContext() {
        if (context) {
            return context;
        }
        var AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) {
            return null;
        }
        try {
            context = new AudioContext();
        } catch (error) {
            return null;
        }
        return context;
    }

    window.fitTrack.unlockRestAlarm = function () {
        var ctx = getContext();
        if (ctx && ctx.state === 'suspended') {
            ctx.resume().catch(function () { /* autoplay policy: ignore */ });
        }
    };

    window.fitTrack.playRestAlarm = function () {
        var ctx = getContext();
        if (!ctx) {
            return;
        }
        if (ctx.state === 'suspended') {
            ctx.resume().catch(function () { /* ignore */ });
        }

        try {
            var oscillator = ctx.createOscillator();
            var gain = ctx.createGain();
            oscillator.type = 'sine';
            oscillator.frequency.value = 880;
            gain.gain.setValueAtTime(0.0001, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.25, ctx.currentTime + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.45);
            oscillator.connect(gain);
            gain.connect(ctx.destination);
            oscillator.start(ctx.currentTime);
            oscillator.stop(ctx.currentTime + 0.5);
        } catch (error) {
            // Never let the alarm take the modal down.
        }

        if (navigator.vibrate) {
            try {
                navigator.vibrate([200, 100, 200]);
            } catch (error) {
                // ignore
            }
        }
    };
})();