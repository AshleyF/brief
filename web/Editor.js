// Editor

var Editor = new function () {

    // Structure is represented as nodes with 'next', 'prev' and 'parent'.
    // Lists have 'first' and 'last'. First in lists is Nil.
    // Cursor follows node.
    // Root nodes have no parent. First/last nodes have no prev/next.

    var root = { type: "Nil" };
    var cursor = root;
    var selection = { from: null, to: null };

    this.Root = function () {
        return root;
    };

    this.Cursor = function () {
        return cursor;
    };

    this.Selection = function () {
        var from = selection.from;
        var to = selection.to;

        function _switch() {
            var n = from;
            do {
                if (n == to) return false;
                n = n.next;
            } while (n);
            return true;
        }

        if (_switch()) {
            return { from: to, to: selection.from };
        }

        return selection;
    };

    this.HasSelection = function () {
        return selection.from || selection.to;
    }

    this.SelectAll = function () {
        if (root.next) {
            selection.from = root.next;
            var to = selection.from;
            while (to.next) to = to.next;
            selection.to = to;
            cursor = to;
        }
    };

    this.SelectNone = function () {
        selection.from = selection.to = null;
    };

    this.InsertWord = function (name) {
        var k = kind(name);
        this.SelectNone();
        var next = cursor.next;
        var word = { type: "Word", kind: k, name: name, prev: cursor, next: next, parent: cursor.parent };
        if (!next && word.parent)
            word.parent.last = word;
        cursor.next = word;
        if (next) next.prev = word;
        cursor = word;
    };

    this.InsertList = function () {
        this.SelectNone();
        var nil = { type: "Nil" };
        var next = cursor.next;
        var list = { type: "List", first: nil, last: nil, prev: cursor, next: next, parent: cursor.parent };
        nil.parent = list;
        if (!next && list.parent)
            list.parent.last = list;
        cursor.next = list;
        if (next) next.prev = list;
        cursor = nil;
    };

    // TODO: Simplify below once stepIn/stepOut usage is known

    function extendSelection(to, dir) {
        if (selection.from == to) {
            selection.from = selection.to = null;
            return;
        }

        if (!selection.from)
            selection.from = to;

        if (selection.to == to)
            selection.to = dir;
        else
            selection.to = to;
    }

    this.MovePrev = function (stepIn, stepOut, select) {
        if (select) stepIn = stepOut = false; // Not allowed while selecting
        else this.SelectNone();
        if (stepIn && cursor.type == "List") {
            cursor = cursor.last;
            return true;
        }
        else if (cursor.prev) {
            if (select) extendSelection(cursor, cursor.prev);
            cursor = cursor.prev;
            return true;
        }
        else if (stepOut && cursor.parent) {
            cursor = cursor.parent.prev;
            return true;
        }
        return false;
    };

    this.MoveNext = function (stepIn, stepOut, select) {
        if (select) stepIn = stepOut = false; // Not allowed while selecting
        else this.SelectNone();
        if (cursor.next) {
            var f = cursor;
            cursor = cursor.next;
            if (stepIn && cursor.type == "List")
                cursor = cursor.first;
            if (select) extendSelection(cursor, cursor.next);
            return true;
        }
        else if (stepOut && cursor.parent) {
            cursor = cursor.parent;
            return true;
        }
        return false;
    };

    this.DeletePrev = function (stepIn, stepOut) {
        if (this.HasSelection()) {
            // TODO: Identical to other Delete*
            var s = this.Selection(); // Normalized
            cursor = s.from.prev;
            cursor.next = s.to.next;
            if (cursor.next)
                cursor.next.prev = cursor;
            this.SelectNone();
        }
        else {
            if (this.MovePrev(stepIn, stepOut)) {
                if (cursor.next) cursor.next = cursor.next.next;
                if (cursor.next) cursor.next.prev = cursor;
                if (!cursor.next && cursor.parent)
                    cursor.parent.last = cursor;
                return true;
            }
            else {
                return false;
            }
        }
    };

    this.DeleteNext = function (stepIn, stepOut) {
        if (this.HasSelection()) {
            // TODO: Identical to other Delete*
            var s = this.Selection(); // Normalized
            cursor = s.from.prev;
            cursor.next = s.to.next;
            if (cursor.next)
                cursor.next.prev = cursor;
            this.SelectNone();
        }
        else {
            if (this.MoveNext(stepIn, stepOut)) {
                return this.DeletePrev(stepIn, stepOut);
            }
            else {
                return false;
            }
        }
    };
}

// Render

function _escape(str) {
    return str; // TODO: Breaks rendering of >, <, words and </b> turns into a comment?!
    //return str.replace("<", "&lt;").replace(">", "&gt;").replace("&", "&amp;");
}

function render(cursorFn) {
    var cursor = Editor.Cursor();
    var selection = Editor.Selection();
    var from = selection.from;
    var to = selection.to;

    function _render(node, html) {
        function _cursor() {
            if (node == cursor) return cursorFn();
            else return "";
        }

        function _selectionFrom() {
            if (node == from)
                return "<span class='selected'>";
            else
                return "";
        }

        function _selectionTo() {
            if (node == to)
                return "</span>";
            else
                return "";
        }

        if (node) {
            switch (node.type) {
                case "List":
                    return _render(node.next, html + _selectionFrom() + "<span class='list'>" + _render(node.first, "") + "</span>" + _selectionTo() + _cursor());
                case "Word":
                    return _render(node.next, html + _selectionFrom() + "<span class='" + node.kind + "'>" + _escape(node.name) + "</span>" + _selectionTo() + _cursor());
                case "Nil":
                    return _render(node.next, html + _selectionFrom() + (node.next ? "" : "&nbsp;") + _selectionTo() + _cursor());
            }
        }
        else {
            return html;
        }
    }

    $("#input").empty().append(_render(Editor.Root(), ""));
}

function code(from, to) {
    var abort = false;

    function _code(node, out) {
        if (node) {
            switch (node.type) {
                case "List":
                    out += "[ " + _code(node.first, "") + "] ";
                    if (abort) return out;
                    break;
                case "Word":
                    out += _escape(node.name) + " ";
                    break;
                case "Nil":
                    break;
            }
            if (node == to) {
                abort = true;
                return out;
            }
            else return _code(node.next, out);
        }
        else {
            return out;
        }
    }

    return _code(from, "");
}

// Input

var token = "";
var inQuote = false;
var last = 0;

function complete(token) {
    if (kind(token) == "unknown") {
        var words = Brief.Words();
        for (var w in words) {
            var d = words[w];
            if (d.substr(0, token.length) == token)
                return d;
        }
    }
    if ("true".substr(0, token.length) == token) return "true"; // Note: Not all completions are dictionary words
    if ("false".substr(0, token.length) == token) return "false";
    if (token.substr(0, 1) == '"' && (token.length == 1 || token.substr(token.length - 1, 1) != '"')) return token + '"';
    return token;
}

function lookup(token) {
    var words = Brief.Words();
    for (var w in words) {
        if (words[w] == token) return true;
    }
    return false;
}

function kind(token) {
    try {
        var t = typeof (eval(token));
        switch (t) {
            case "string":
            case "number":
            case "boolean":
                return t;
            default:
                throw "Unknown kind: '" + token + "'";
        }
    }
    catch (ex) {
        if (lookup(token))
	    return Brief.Word(token).kind;
        else
            return "unknown";
    }
}

function update() {
    render(function () {
        if (token.length > 0) {
            return "<span class='" + kind(complete(token)) + "'>" + _escape(token) + "<span class='cursor'>|</span><span class='complete'>" + complete(token).substr(token.length) + "</span></span>";
        }
        else {
            return token + "<span class='cursor'>|</span>";
        }
    });

    var c = code(Editor.Root(), Editor.Cursor());
    $("#output").empty().append(c);
    Brief.Reset();
    Brief.Execute(c);

    var ctx = Brief.Context();
    $("#context").empty();
    for (var i = 0; i < ctx.Stack.length; i++) {
        var s = ctx.Stack[i];
        $("#context").append("<div class='stack'/>").append(Brief.Print([s]));
    }
}

$(document).keydown(function (e) {
    switch (e.which) {
        case 8: // Backspace
            e.preventDefault();
            if (token.length > 0)
                token = token.substr(0, token.length - 1);
            else
                Editor.DeletePrev(false, false);
            break;
        case 46: // Delete
            e.preventDefault();
            Editor.DeleteNext(false, false);
            break;
        case 37: // <-
            e.preventDefault();
            if (e.altKey) {
                if (!Editor.MovePrev(false, false, e.shiftKey)) // At first already?
                    Editor.MovePrev(false, true, e.shiftKey); // Step out
                while (Editor.MovePrev(false, false, e.shiftKey)); // Move to first
            }
            else {
                Editor.MovePrev(e.ctrlKey, true, e.shiftKey);
            }
            break;
        case 39: // ->
            e.preventDefault();
            if (e.altKey) {
                if (!Editor.MoveNext(false, false, e.shiftKey)) // At first already?
                    Editor.MoveNext(false, true, e.shiftKey); // Step out
                while (Editor.MoveNext(false, false, e.shiftKey)); // Move to first
            }
            else {
                Editor.MoveNext(e.ctrlKey, true, e.shiftKey);
            }
            break;
        case 65: // CTRL-A - Select All
            if (e.ctrlKey) {
                e.preventDefault();
                Editor.SelectAll();
            }
            break;
        case 68: // CTRL-D - Define
            if (e.ctrlKey) {
                e.preventDefault
                if (Editor.HasSelection()) {
                    var n = prompt("Your new word's name?");
                    if (!n || n.length == 0)
                        break;
                    if (n.substr(0, 1) == '"') {
                        alert("Names cannot begin with double quotes.");
                        break;
                    }
                    var k = kind(n);
                    if (k != "unknown") {
                        if (!confirm("Redefine existing word?"))
                            break;
                        k = "unknown";
                    }

                    var s = Editor.Selection();
                    var d = '[ ' + code(s.from, s.to) + '] "' + n.replace(/"/g, '\"') + '" define';
                    Brief.Execute(d);

                    Editor.DeleteNext(false, false); // TODO: Need a DeleteSelected
                    Editor.InsertWord(n);

                    //prompt("Source:", d); // TODO: Add source to definition
                    $("#dictionary").append($("<div/>").append(d));
                }
            }
            break;
        case 81: // CTRL-Q
            if (e.ctrlKey && Editor.HasSelection()) {
                e.preventDefault();
                var s = Editor.Selection();
                var f = s.from;
                var t = s.to;
                Editor.DeleteNext(false, false); // TODO: Need DeleteSelection()
                Editor.InsertList();
                var l = Editor.Cursor().parent;
                Editor.MoveNext(false, true, false); // Past list
                f.prev = l.first;
                l.first.next = f;
                l.last = t;
                t.next = null;
                do {
                    f.parent = l;
                    f = f.next;
                } while (f);
            }
            break;
    }
    $(document).focus(); // Prompts and alerts steal focus
    update();
});

$(document).keypress(function (e) {
    e.preventDefault();
    if (inQuote) {
        switch (e.which) {
           case 34: // "
                if (last != 92 /* \ */) {
                    inQuote = false;
                    token += '"';
                    token = complete(token);
                    Editor.InsertWord(token);
                    token = "";
                }
                else {
                    token += '"';
                }
                break;
            default:
                if (e.which >= 32) // not control char
                    token += String.fromCharCode(e.which);
                break;
        }
        last = e.which;
    }
    else { // !inQuote
        switch (e.which) {
            case 32: // space
                if (token.length > 0) {
                    token = complete(token);
                    Editor.InsertWord(token);
                    token = "";
                }
                break;
            case 34: // "
                if (token.length == 0)
                    inQuote = true;
                token += '"';
                break;
            case 91: // [
                if (token.length == 0) {
                    e.preventDefault();
                    Editor.InsertList();
                }
                else {
                    token += '[';
                }
                break;
            case 93: // [
                if (token.length == 0) {
                    e.preventDefault();
                    Editor.MoveNext(true, true);
                }
                else {
                    token += ']';
                }
                break;
            default:
                if (e.which >= 32) { // not control char
                    token += String.fromCharCode(e.which);
                    Editor.SelectNone();
                }
                break;
        }
    }
    update();
});

$(document).ready(function () {
    update();
});

/*
- Not allowing definitions containing literal values
- Completion bug (fixed) with "2bi"

TODO:
- Quote/Unquote
- Copy/Paste
*/