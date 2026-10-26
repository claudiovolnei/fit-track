window.startAutoFinishAlarm = function () {
    try {
        if (navigator.vibrate) {
            navigator.vibrate([500, 200, 500, 200, 500]);
        }
    } catch (e) { /* graceful degradation */ }

    try {
        var ctx = new (window.AudioContext || window.webkitAudioContext)();
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(880, ctx.currentTime);
        gain.gain.setValueAtTime(0.1, ctx.currentTime);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        window._alarmOsc = osc;
        window._alarmGain = gain;
        window._alarmCtx = ctx;
    } catch (e) { /* graceful degradation */ }
};

window.stopAutoFinishAlarm = function () {
    try {
        if (navigator.vibrate) {
            navigator.vibrate(0);
        }
    } catch (e) { /* graceful degradation */ }

    try {
        if (window._alarmOsc) {
            window._alarmOsc.stop();
            window._alarmOsc = null;
        }
        if (window._alarmCtx) {
            window._alarmCtx.close();
            window._alarmCtx = null;
        }
    } catch (e) { /* graceful degradation */ }
};

window.fitTrack.playRestAlarm = function () {
    try {
        if (navigator.vibrate) {
            navigator.vibrate([500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150, 500, 150]);
        }
    } catch (e) { /* graceful degradation */ }

    try {
        var ctx = new (window.AudioContext || window.webkitAudioContext)();
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(880, ctx.currentTime);
        gain.gain.setValueAtTime(0.08, ctx.currentTime);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        osc.frequency.linearRampToValueAtTime(660, ctx.currentTime + 0.5);
        osc.frequency.linearRampToValueAtTime(880, ctx.currentTime + 1.0);
        osc.stop(ctx.currentTime + 20.0);
        gain.gain.linearRampToValueAtTime(0.08, ctx.currentTime + 19.5);
        gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 20.0);
    } catch (e) { /* graceful degradation */ }
};

window.fitTrack.unlockRestAlarm = function () {
    try {
        new (window.AudioContext || window.webkitAudioContext)();
    } catch (e) { /* graceful degradation */ }
};
