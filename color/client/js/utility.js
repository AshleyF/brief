"use strict";

function hiDpiCanvas(canvas, context, width, height) {
    var devicePixelRatio = window.devicePixelRatio || 1;
    var backingStoreRatio = context.webkitBackingStorePixelRatio ||
                            context.mozBackingStorePixelRatio    ||
                            context.msBackingStorePixelRatio     ||
                            context.oBackingStorePixelRatio      ||
                            context.backingStorePixelRatio       || 1;
    var ratio = devicePixelRatio / backingStoreRatio;
    canvas.width = width * ratio;
    canvas.height = height * ratio;
    context.scale(ratio, ratio);
}
