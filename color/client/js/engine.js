"use strict";

var engine = (function() {
    var turtle;
    var primitives, dictionary, memory, stack, retStack, p;

    function getTurtle() {
        return turtle;
    }

    function bearingFetch() {
        stack.unshift({ kind: 'number', value: turtle.bearing });
    }

    function bearingStore() {
        turtle.bearing = stack.shift().value;
    }

    function nextRainbow() {
        var r = turtle.r;
        var g = turtle.g;
        var b = turtle.b;
        if (r >= 255 && b <= 0 && g < 255) g += 5;
        else if (g >= 255 && b <= 0 && r > 0) r -= 5;
        else if (r <= 0 && g >= 255 && b < 255) b += 5;
        else if (r <= 0 && b >= 255 && g > 0) g -= 5;
        else if (g <= 0 && b >= 255 && r < 255) r += 5;
        else if (r >= 255 && g <= 0 && b > 0) b -= 5;
        turtle.r = r;
        turtle.g = g;
        turtle.b = b;
    }

    function turtleForward() {
        var step = stack.shift().value;
        var x = turtle.x;
        var y = turtle.y;
        turtle.x += Math.sin(turtle.bearing) * step;
        turtle.y += Math.cos(turtle.bearing) * step;
        turtle.path.unshift({
            r: turtle.r,
            g: turtle.g,
            b: turtle.b,
            xa: x,
            ya: y,
            xb: turtle.x,
            yb: turtle.y,
        });
    }

    function binaryOp(kind, f) {
        return function () {
            if (stack.length >= 2) {
                var x = stack.shift();
                var y = stack.shift();
                stack.unshift({ kind: kind, value: f(y.value, x.value) });
            } else throw "Stack underflow";
        };
    }

    function use() {
        var file = stack.shift();
        if (file.kind != 'symbol') throw 'Expected symbol library name';
        editor.getRemote(file.value, function (data) {
            // TODO: handle async in non-interactive execution!
            exec(data.code);
        });
    }

    function define(name) {
        dictionary[name] = memory.length;
    }

    function append(name, fn) {
        memory.push({ name: name, op: fn });
    }

    function ret() {
        if (retStack.length > 0) p = retStack.shift();
    }

    function addPrimitive(name, fn) {
        primitives[name] = fn;
    }

    function jump(address) {
        p = address;
        while (p != -1) {
             var op = memory[p++].op;
             op(); // important to call *after* p increment
        }
    }

    function branch(address) {
        var p = stack.shift().value;
        if (!p) jump(address);
    }

    function call(address) {
        retStack.unshift(p);
        jump(address);
    }

    function makeCallClosure(address) {
        var c = function () { call(address); };
        c.isCall = true;
        c.address = address;
        return c;
    }

    function makeJumpClosure(address) {
        return function () { jump(address); };
    }

    function makeBranchClosure(address) {
        return function () { branch(address); };
    }

    function here() {
        stack.unshift({ kind: 'number', value: memory.length });
    }

    function compileBranch() {
        var a = stack.shift().value;
        append('branch ' + a, makeBranchClosure(a));
    }

    function patchBranch() {
        var p = stack.shift().value;
        var a = stack.shift().value;
        memory[a] = { name: 'branch ' + p, op: makeBranchClosure(p) };
    }

    function makeLitClosure(val) {
        return function () { stack.unshift({ kind: 'number', value: parseFloat(val) }); };
    }

    var exec = function(code) {
        p = -1;
        for (var i in code) {
            delete code[i].error;
            delete code[i].color;
            var w = code[i];
            // try {
                if (w.value != '') { // allow for initial editing without error
                    switch (w.kind) {
                        case 'define':
                            define(w.value);
                            break;
                        case 'number':
                            stack.unshift({ kind: 'number', value: parseFloat(w.value) });
                            break;
                        case 'symbol':
                            stack.unshift({ kind: 'symbol', value: w.value });
                            break;
                        case 'literal':
                            append('lit ' + w.value, makeLitClosure(w.value));
                            // TODO: compile number
                            break;
                        case 'immediate':
                            var a = dictionary[w.value];
                            if (a != undefined) call(a);
                            else if (primitives[w.value]) primitives[w.value]();
                            break;
                        case 'compile':
                            var a = dictionary[w.value];
                            if (a != undefined)
                                append('call ' + a + ' (' + w.value + ')', makeCallClosure(a));
                            else if (primitives[w.value]) {
                                var c = memory[memory.length - 1];
                                if (w.value == ';' && c.op.isCall) { // tail call elimination
                                    c.name = 'jump' + c.name.substr(4);
                                    c.op = makeJumpClosure(c.op.address);
                                } else {
                                    append('prim ' + w.value, primitives[w.value]);
                                }
                            }
                            else throw "Unknown word '" + w.value + "'";
                            break;
                        case 'editor':
                            break;
                    }
                }
            // } catch(ex) {
            //     w.error = ex;
            //     return [{ kind: 'error', error: ex, value: ex }];
            // }
        }
        return stack;
    }

    var reset = function() {
        primitives = {};
        dictionary = {};
        memory = [];
        stack = [];
        retStack = [];
        p = 0;

        addPrimitive('pause', function () { alert('Pause'); });
        addPrimitive('here', here);
        addPrimitive('branch', compileBranch);
        addPrimitive('patch', patchBranch);
        addPrimitive('use', use);
        addPrimitive(';', ret);
        addPrimitive('dup', function () {
            if (stack.length >= 1) { stack.unshift(stack[0]); }
            else throw "Stack underflow"; });
        addPrimitive('drop', function () {
            if (stack.length >= 1) { stack.shift(); }
            else throw "Stack underflow"; });
        addPrimitive('+', binaryOp('number', function (y, x) { return y + x }));
        addPrimitive('-', binaryOp('number', function (y, x) { return y - x }));
        addPrimitive('×', binaryOp('number', function (y, x) { return y * x }));
        addPrimitive('÷', binaryOp('number', function (y, x) { return y / x }));
        addPrimitive('=', binaryOp('boolean', function (y, x) { return y = x }));
        addPrimitive('≠', binaryOp('boolean', function (y, x) { return y != x }));
        addPrimitive('>', binaryOp('boolean', function (y, x) { return y > x }));
        addPrimitive('<', binaryOp('boolean', function (y, x) { return y < x }));
        addPrimitive('≥', binaryOp('boolean', function (y, x) { return y >= x }));
        addPrimitive('≤', binaryOp('boolean', function (y, x) { return y <= x }));

        addPrimitive('bearing@', bearingFetch);
        addPrimitive('bearing!', bearingStore);
        addPrimitive('next-rainbow', nextRainbow);
        addPrimitive('forward', turtleForward);

        turtle = { // TODO: move to proper module
            x: 0, y: 1,
            bearing: Math.PI,
            r: 255, g: 0, b: 0,
            path: []
            };
    }

    reset();

    var getMemory = function() { return memory; };
    var getDictionary = function() { return dictionary; };

    return {
        getMemory: getMemory,
        getDictionary: getDictionary,
        reset: reset,
        exec: exec,
        getTurtle: getTurtle,
    };
})();
