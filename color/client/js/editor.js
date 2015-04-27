"use strict";

var editor = (function() {
    var curX = 0;
    var code = [];
    var mode = 'normal';
    var count = 0;

    var load = function(source) {
        code = source;
    }

    var fileCache = {};

    function addCachedCode(name, code, cursor) {
        fileCache[name] = { code: code, cursor: cursor };
    }

    var getRemote = function(name, callback) {
        var cached = fileCache[name];
        if (cached) {
            callback(cached);
        } else {
            $.getJSON('load?name=' + name, function(data) {
                addCachedCode(name, data, curX);
                callback({ code: data, cursor: 0 });
            });
        }
    }

    var fileStack = [];

    var loadRemoteByName = function(name) {
        getRemote(name, function (cached) {
            fileStack.unshift(name);
            curX = cached.cursor;
            code = cached.code;
            rend(code); // TODO: dependency on index code
        });
    }

    var loadRemote = function() {
        if (fileStack.length > 0) fileCache[fileStack[0]].cursor = curX; // remember last cursor position
        loadRemoteByName(code[curX].value);
    }

    var loadBack = function() {
        if (fileStack.length > 1) {
            var current = fileStack.shift();
            fileCache[current].cursor = curX; // remember last cursor position
            loadRemoteByName(fileStack.shift());
        }
    }

    var saveRemote = function() {
        var name = fileStack[0];
        var data = _.map(code, function (w) { return { kind: w.kind, value: w.value }; });
        addCachedCode(name, data, curX);
        $.ajax({
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            url: 'store?name=' + name,
            success: function (data, status) {
                alert('Stored: ' + data + ' (' + status + ')');
            }});
    }

    var back = function() {
        curX = Math.max(0, curX - 1);
        count = 0;
        rend(code); // TODO: dependency on index code
    }

    var forward = function() {
        curX = Math.min(code.length - 1, curX + 1);
        count = 0;
        rend(code); // TODO: dependency on index code
    }

    var down = function() {
        // TODO
        // curY = Math.min(code.length - 1, curY + 1);
        // curX = Math.min(code[curY].length - 1, curX);
        // count = 0;
        // rend(code); // TODO: dependency on index code
    }

    var up = function() {
        // TODO
        // curY = Math.max(0, curY - 1);
        // curX = Math.min(code[curY].length - 1, curX);
        // count = 0;
        // rend(code); // TODO: dependency on index code
    }

    var delForward = function() {
        if (code.length > 1) {
            code.splice(curX, 1);
            curX = Math.min(code.length - 1, curX);
        } else {
            code = [{ kind: code[curX].kind, value: '' }];
            count = 0;
            mode = 'insert';
        }
        rend(code); // TODO: dependency on index code
    }

    var count = function(d) {
        count = count * 10 + d;
        rend(code); // TODO: dependency on index code
    }

    var insertEscape = function() {
        if (code[curX].value.length == 0) {
            code.splice(curX, 1);
            if (mode == 'insert-after') curX--;
        }
        mode = 'normal';
        rend(code); // TODO: dependency on index code
    }

    var insertBackspace = function() {
        var name = code[curX].value;
        if (name.length > 0) name = name.substr(0, name.length - 1);
        code[curX].value = name;
        rend(code); // TODO: dependency on index code
    }

    var insertAfter = function() {
        code.splice(curX + 1, 0,  { kind: code[curX].kind, value: '' });
        curX++;
        mode = 'insert-after';
        count = 0;
        rend(code); // TODO: dependency on index code
    }

    var insertBefore = function() {
        code.splice(curX, 0, { kind: code[curX].kind, value: '' });
        mode = 'insert';
        count = 0;
        rend(code); // TODO: dependency on index code
    }

    var insertSpace = function() {
        if (code[curX].kind == 'symbol') {
            //code[curX].value += '␣';
            //code[curX].value += '⍽';
            //code[curX].value += '␠';
            code[curX].value += '·';
        } else {
            code.splice(curX + 1, 0,  { kind: code[curX].kind, value: '' });
            curX++;
        }
        rend(code); // TODO: dependency on index code
    }

    var insertNewLine = function() {
        code.splice(curX, 0, { kind: 'editor', value: '↵' });
        rend(code); // TODO: dependency on index code
    }

    var cycleColor = function() {
        switch (code[curX].kind) {
            case 'define': code[curX].kind = 'compile'; break;
            case 'compile': code[curX].kind = 'immediate'; break;
            case 'immediate': code[curX].kind = 'comment'; break;
            case 'comment': code[curX].kind = 'literal'; break;
            case 'literal': code[curX].kind = 'number'; break;
            case 'number': code[curX].kind = 'symbol'; break;
            case 'symbol': code[curX].kind = 'define'; break;
        }
    }

    var insertCharacter = function(c) {
        var word = code[curX].value;
        var kind = code[curX].kind;
        if (word.length > 0) {
            word += c;
        } else {
            if (kind == 'number' || kind == 'literal') {
                word += c;
                if (c < '0' || c > '9')
                    kind = 'symbol';
            } else {
                switch (c) {
                    case ':': kind = 'define'; break;
                    case ',': kind = 'compile'; break;
                    case '_': kind = 'immediate'; break;
                    case '/': kind = 'comment'; break;
                    case "'": kind = 'symbol'; break;
                    case '#': kind = 'literal'; break;
                    case '0': kind = 'number'; break;
                    default: word += c; break;
                }
            }
        }

        word = word
            .replace('\\*', '×')
            .replace('\\pi', '𝛑')
            .replace('\\/', '÷')
            .replace('!=', '≠')
            .replace('>=', '≥')
            .replace('<=', '≤')
            ;

        code[curX].value = word;
        code[curX].kind = kind;

        rend(code); // TODO: dependency on index code
    }

    var getCode = function() { return code; }
    var getCount = function() { return count; }
    var getMode = function() { return mode; }
    var getCurX = function() { return curX; }

    return {
        getCode: getCode,
        getCount: getCount,
        getMode: getMode,
        getCurX: getCurX,
        load: load,
        getRemote: getRemote,
        loadRemote: loadRemote,
        loadBack: loadBack,
        saveRemote: saveRemote,
        back: back,
        forward: forward,
        down: down,
        up: up,
        delForward: delForward,
        count: count,
        insertEscape: insertEscape,
        insertBackspace: insertBackspace,
        insertAfter: insertAfter,
        insertBefore: insertBefore,
        insertSpace: insertSpace,
        insertNewLine: insertNewLine,
        cycleColor: cycleColor,
        insertCharacter: insertCharacter,
    };
})();
