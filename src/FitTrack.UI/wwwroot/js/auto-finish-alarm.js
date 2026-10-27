window.startAutoFinishAlarm = function () {
    let alarmState = window._alarmState || {};
    
    // Tentar vibração nativa do dispositivo
    if (!alarmState.vibrationAttempted) {
        alarmState.vibrationAttempted = true;
        try {
            if (navigator.vibrate) {
                navigator.vibrate([500, 200, 500, 200, 500]);
                alarmState.vibrationActive = true;
            }
        } catch (e) {
            /* gracefull degradation */
        }
    }
    
    // Tentar áudio nativo
    if (!alarmState.audioAttempted) {
        alarmState.audioAttempted = true;
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
            alarmState.audioActive = true;
            
            // Parar automaticamente após 15 segundos
            setTimeout(function() {
                if (window._alarmOsc) {
                    window._alarmOsc.stop();
                    window._alarmOsc = null;
                }
            }, 15000);
        } catch (e) {
            /* gracefull degradation */
        }
    }
    
    // Tentar Notification API se permissão concedida
    if (!alarmState.notificationAttempted) {
        alarmState.notificationAttempted = true;
        try {
            if (Notification.permission === 'granted') {
                var notification = new Notification('FitTrack', {
                    body: 'Treino expirado — finalize a série',
                    icon: '/icons/icon-512.png',
                    badge: '/icons/icon-192.png'
                });
                notification.onclick = function() {
                    window.focus();
                };
                alarmState.notificationActive = true;
            } else if (Notification.permission === 'default') {
                Notification.requestPermission().then(function(permission) {
                    if (permission === 'granted') {
                        new Notification('FitTrack', {
                            body: 'Treino expirado — finalize a série',
                            icon: '/icons/icon-512.png',
                            badge: '/icons/icon-192.png'
                        });
                    }
                }).catch(function(err) {
                    // graceful degradation
                });
            }
        } catch (e) {
            /* gracefull degradation */
        }
    }
    
    window._alarmState = alarmState;
};

window.stopAutoFinishAlarm = function () {
    var alarmState = window._alarmState || {};
    
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
    
    // Limpar estado
    window._alarmState = {};
};

window.fitTrack = window.fitTrack || {};

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
