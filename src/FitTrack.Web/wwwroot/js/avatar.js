window.fitTrack = window.fitTrack || {};

// Redimensiona uma imagem no cliente antes do upload, para manter o payload pequeno.
// Recebe uma data URL (data:image/...;base64,...) e devolve a data URL redimensionada.
window.fitTrack.resizeImage = function (dataUrl, maxSize) {
    return new Promise(function (resolve, reject) {
        var img = new Image();
        img.onload = function () {
            var width = img.width;
            var height = img.height;
            if (width > maxSize || height > maxSize) {
                if (width > height) {
                    height = Math.round(height * maxSize / width);
                    width = maxSize;
                } else {
                    width = Math.round(width * maxSize / height);
                    height = maxSize;
                }
            }
            var canvas = document.createElement('canvas');
            canvas.width = width;
            canvas.height = height;
            var ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, width, height);
            resolve(canvas.toDataURL('image/jpeg', 0.85));
        };
        img.onerror = reject;
        img.src = dataUrl;
    });
};
