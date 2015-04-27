"use strict";

var render = (function() {
    var darkGray = '#111111';

    function renderTurtle(turtle, left, top, width, height) {
        var canvas = document.getElementById("canvas")
        var ctx = canvas.getContext("2d");

        ctx.fillStyle = '#000000;';
        ctx.fillRect(left, top, width, height);

        var midx = width / 2 + left;
        var midy = height / 2 + top;

        // draw turtle
        ctx.fillStyle = "rgb(" + turtle.r + "," + turtle.g + "," + turtle.b + ")";
        ctx.beginPath();
        var p0 = turtle.bearing - 0.5 * Math.PI;
        var p1 = p0 + Math.PI - 0.6;
        var p2 = p0 + Math.PI + 0.6;
        ctx.moveTo(midx + turtle.x + Math.cos(p0) * 16, midy + turtle.y - Math.sin(p0) * 16);
        ctx.lineTo(midx + turtle.x + Math.cos(p1) * 16, midy + turtle.y - Math.sin(p1) * 16);
        ctx.lineTo(midx + turtle.x, midy + turtle.y);
        ctx.lineTo(midx + turtle.x + Math.cos(p2) * 16, midy + turtle.y - Math.sin(p2) * 16);
        ctx.fill();

        // draw path
        for (var f in turtle.path) {
            var p = turtle.path[f];
            ctx.lineWidth = 4;
            ctx.strokeStyle = 'rgb(' + p.r + ',' + p.g + ',' + p.b + ')';
            ctx.beginPath();
            ctx.moveTo(midx + p.xa, midy + p.ya);
            ctx.lineTo(midx + p.xb, midy + p.yb);
            ctx.stroke();
        }

        // draw border
        ctx.lineWidth = 4;
        ctx.strokeStyle = '#333333';
        ctx.beginPath();
        ctx.moveTo(left, top + height);
        ctx.lineTo(left + width, top + height);
        ctx.stroke();

        ctx.lineCap = "round";
        ctx.lineJoin = "round";
    }

    var error  = '#ff0000';
    var red    = '#f92672';
    var orange = '#fd971f';
    var yellow = '#e7db75';
    var green  = '#a6e22e';
    var blue   = '#66d9ef';
    var purple = '#9358fe';
    var black  = '#272822';
    var gray   = '#abaa98';
    var white  = '#f2f0f2';

    function kindColor(kind) {
        switch (kind) {
            case 'define'   : return red;
            case 'compile'  : return green;
            case 'immediate': return yellow;
            case 'comment'  : return white;
            case 'symbol'   : return orange;
            case 'number'   : return blue;
            case 'literal'  : return purple;
            case 'boolean'  : return gray;
            case 'editor'   : return black;
            default         : throw 'Unknown kind: ' + kind;
        }
    }

    var margin = 32;
    var displayHeight = 64;

    function renderCode(code, left, top, width, height, atX, atY, atSize) {
        var canvas = document.getElementById("canvas")
        var ctx = canvas.getContext("2d");

        var defaultSize = 18;
        var fontSize = atSize || defaultSize;
        var lineHeight = (atSize || defaultSize) * 1.8;
        var spaceWidth = (atSize || defaultSize) * 0.5;
        ctx.font = fontSize + 'pt sans-serif';

        var indent = (atSize || defaultSize) * 3.5;
        var round = (atSize || defaultSize) * 0.4;
        var x = (atX || margin) + left,
            y = (atY || (displayHeight + margin * 4 + (atSize || defaultSize))) + top;

        var lastLine = top + height - lineHeight - margin;
        var lastColumn = left + width - margin;

        for (var i in code) {
            var word = code[i];
            word.color = word.color || kindColor(word.kind);
            if (word.error) word.color = error;
            ctx.fillStyle = word.color;
            var w = ctx.measureText(word.value).width;
            if (x + w + spaceWidth > lastColumn && word.kind != 'editor') { // note: editor words allowed to 'hang off'
                x = margin + indent;
                y += lineHeight;
                if (y > lastLine)
                    return;
            }

            if (i == editor.getCurX()) {
                var xx = spaceWidth / 2 + 0.5; // TODO: why + 0.5?
                var yy = (lineHeight - fontSize) / 2;
                var xa = x - xx;
                var xb = xa + round;
                var xd = xa + w + 2 * xx;
                var xc = xd - round;
                var ya = y - yy;
                var yb = ya + round;
                var yd = ya + fontSize + 2 * yy;
                var yc = yd - round;

                var roundNW = true;
                var roundNE = true;
                var roundSE = true;
                var roundSW = true;

                ctx.beginPath();
                if (roundNW) ctx.moveTo(xb, ya); else ctx.moveTo(x, ya);
                if (roundNE) ctx.lineTo(xc, ya); else ctx.lineTo(xd, ya);
                if (roundNE) ctx.quadraticCurveTo(xd, ya, xd, yb);
                if (roundSE) ctx.lineTo(xd, yc); else ctx.lineTo(xd, yd);
                if (roundSE) ctx.quadraticCurveTo(xd, yd, xc, yd);
                if (roundSW) ctx.lineTo(xb, yd); else ctx.lineTo(xa, yd);
                if (roundSW) ctx.quadraticCurveTo(xa, yd, xa, yc);
                if (roundNW) ctx.lineTo(xa, yb); else ctx.lineTo(xa, ya);
                if (roundNW) ctx.quadraticCurveTo(xa, ya, xb, ya);
                ctx.fill();
                ctx.fillStyle = '#000000';
            }

            ctx.fillText(word.value, x, y + fontSize);
            x += w + spaceWidth;
            
            if (word.kind == 'editor' && word.value == '↵') {
                x = margin;
                y += lineHeight;
                if (y > lastLine)
                    return;
            }
        }
    }

    function renderMainPane(code, left, top, width, height) {
        var canvas = document.getElementById("canvas")
        var ctx = canvas.getContext("2d");
        ctx.save();
        ctx.rect(left, top, width, height);
        ctx.clip();
        ctx.fillStyle = '#000000';
        ctx.fillRect(left, top, width, height);
        renderCode(code, left, top, width, height);
        engine.reset();
        var stack = engine.exec(code.slice(0, editor.getCurX() + 1));
        var stackDisplayHeight = 12;
        if (stack) renderCode(stack.slice(1).reverse(), left, top, width, height, margin, margin + stackDisplayHeight, stackDisplayHeight);
        ctx.fillStyle = '#ff0000';
        ctx.strokeStyle = '#ff0000';
        ctx.font = displayHeight + 'pt sans-serif';
        if (stack && stack.length > 0) ctx.fillText(stack[0].value, left + margin, top + displayHeight + margin * 2 + stackDisplayHeight);
        ctx.fillStyle = '#000000';
        ctx.fillRect(left + width - margin, top, margin, displayHeight + margin * 3 + stackDisplayHeight);
        ctx.beginPath();
        ctx.moveTo(left, top + stackDisplayHeight + displayHeight + margin * 3);
        ctx.lineTo(left + width, top + stackDisplayHeight + displayHeight + margin * 3);
        ctx.lineWidth = 2;
        ctx.stroke();
        ctx.restore();
    }

    var col0 = margin;
    var col1 = col0 + 110;
    var col2 = col1 + 40;

    function renderMem(mem, left, top, width, height) {
        var smallSize = 12;
        var regSize = 18;
        var margin = regSize;
        var canvas = document.getElementById("canvas")
        var ctx = canvas.getContext("2d");
        ctx.save();
        ctx.fillStyle = darkGray;
        ctx.fillRect(left, top, width, height);
        ctx.fillStyle = red;
        ctx.font = regSize + 'pt sans-serif';
        var maxDef = mem.length;
        var dictionary = engine.getDictionary();
        for (var w in dictionary) {
            var i = dictionary[w];
            var t = top + margin + regSize + regSize * i * 1.8;
            ctx.fillStyle = darkGray;
            ctx.fillRect(left + col0, t - regSize, col1 - col0, regSize * 1.5);
            ctx.fillStyle = red;
            ctx.fillText(w, left + col0, t);
            if (i >= maxDef) maxDef = i + 1;
        }
        for (var i = 0; i < maxDef; i++) {
            ctx.fillStyle = gray;
            ctx.font = smallSize + 'pt sans-serif';
            ctx.fillText(i, left + col1, top + margin + regSize + regSize * i * 1.8);
            if (i < mem.length) {
                ctx.fillStyle = yellow;
                ctx.font = regSize + 'pt sans-serif';
                ctx.fillText(mem[i].name, left + margin + 150, top + margin + regSize + regSize * i * 1.8);
            }
        }
        ctx.restore();
    }

    var renderWindow = function(turtle, code, mem, left, top, width, height) {
        var canvas = document.getElementById("canvas")
        var ctx = canvas.getContext("2d");
        hiDpiCanvas(canvas, ctx, window.innerWidth, window.innerHeight);
        var memWidth = width / 3;
        var turtleHeight = height / 2;
        ctx.fillStyle = '#000000';
        ctx.fillRect(left, top, width, height);
        renderTurtle(turtle, left, top, width - memWidth, turtleHeight);
        renderMainPane(code, left, top + turtleHeight, width - memWidth, height - turtleHeight);
        renderMem(mem, left + width - memWidth, top, memWidth, height);
    }

    return {
        renderWindow: renderWindow,
    };
})();
