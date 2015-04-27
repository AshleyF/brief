// Be Brief!

var Brief = new function () {

    function lex(source) {
        function isWhitespace(c) {
            return c == ' ' || c == '\n' || c == '\r' || c == '\t' || c == '\f';
        }
        source += " ";
        var tokens = [];
        var tok = "";
        var str = false;
        var last = '';
        for (var i = 0; i < source.length; i++) {
            var c = source[i];
            if (str) {
                tok += c;
                if (c == '"' && last != '\\') {
                    tokens.push(tok);
                    tok = "";
                    str = false;
                }
                last = c;
            }
            else {
                var emptyTok = (tok.length == 0);
                if (isWhitespace(c)) {
                    if (!emptyTok) {
                        tokens.push(tok);
                        tok = "";
                    }
                } else {
                    if (emptyTok && c == '"') {
                        str = true;
                    }
                    tok += c;
                }
            }
        }
        if (tok.length > 0)
            alert("Incomplete string token: '" + tok + "'");

        return tokens;
    };

    function parse(tokens) {
        var ast = [];
        ast.kind = "list";
        while (tokens.length > 0) {
            var t = tokens.shift();
            switch (t) {
                case "[":
                    ast.push(parse(tokens));
                    break;
                case "]":
                    return ast;
                default:
                    ast.push(word(t));
                    break;
            }
        }
        return ast;
    }

    function compile(quote) {
        return function () {
            for (var i = 0; i < quote.length; i++) {
                var w = quote[i];
                if (typeof (w) == "function")
                    w();
                else {
                    if (w.kind == "list")
                        context.Stack.unshift(w);
                    else if (w.kind == "literal")
                        context.Stack.unshift(w.val);
                    else
                        alert("Unexpected kind: " + w.kind);
                }
            }
        }
    };

    // TODO: Functional print/render

    function print(ast) {
        var output = "";
        for (var i = 0; i < ast.length; i++) {
            var a = ast[i];
            if (a.kind == "list") {
                output += "[ " + print(a) + "] ";
            }
            else {
                if (a.disp)
                    output += a.disp + " ";
                else
                    output += a + " ";
            }
        }
        return output;
    }

    function escape(str) {
        return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/'/g, "&apos;").replace(/"/g, "&quot;");
    }

    function render(ast) {
        var html = "";
        for (var i = 0; i < ast.length; i++) {
            var a = ast[i];
            if (a.kind == "list")
                html += "<span class='list'>" + render(a) + "</span>";
            else
                html += "<span class='" + a.kind + "'>" + escape(a.disp) + "</span>";
        }
        return html;
    }

    function error(token) {
        var e = function () { /* alert("Undefined word: '" + token + "'"); */ };
        e.kind = "error";
        e.disp = token;
        return e;
    }

    function word(token) {
        var w = dictionary[token];
        if (w) {
            return w;
        }
        else {
            try {
                return literal(token);
            }
            catch (ex) {
                return error(token);
            }
        }
    }

    var dictionary = {};

    function define(quote, name) {
        var c = compile(quote);
        c.kind = "secondary";
        c.disp = name;
        dictionary[name] = c;
    }

    this.Words = function () {
        var w = [];
        for (var d in dictionary) {
            w.push(d);
        }
        return w;
    };

    this.Primitive = function (name, func) {
        var f = func;
        var word = function () {
            var len = f.length;
            assertStack(len);
            var args = context.Stack.slice(0, len).reverse(); // TODO: more efficient that slice/reverse
            context.Stack = context.Stack.slice(len);
            var result = f.apply(null, args);
            if (result) {
                if (result.kind == "tuple") {
                    for (var i = 0; i < result.length; i++) {
                        context.Stack.unshift(result[i]);
                    }
                }
                else {
                    context.Stack.unshift(result);
                }
            }
        }
        word.kind = "primitive";
        word.disp = name;
        dictionary[name] = word;
        return word;
    };

    var context = { Stack: [] };

    this.Reset = function () {
        context = { Stack: [] };
    };

    function assertStack(length) {
        //if (context.Stack.length < length)
        //    alert("Stack underflow!");
    }

    this.Push = function (val) {
        if (val !== null && val !== undefined)
            context.Stack.unshift(val);
    };

    this.Peek = function () {
        assertStack(1);
        return context.Stack[0];
    };

    this.Pop = function () {
        assertStack(1);
        return context.Stack.shift();
    };

    function literal(val) {
        var lit = eval(val);
        return { kind: "literal", disp: val, val: lit };
    }

    this.Word = function (token) {
        return word(token);
    };

    this.Parse = function (source) {
        return parse(lex(source));
    };

    this.Render = function (ast) {
        return render(ast);
    };

    this.Context = function () {
        return context;
    };

    this.Print = function (ast) {
        return print(ast);
    };

    this.Compile = function (source) {
        return compile(parse(lex(source)));
    };

    this.Execute = function (source) {
        this.Compile(source)();
    };

    this.Run = function (ast) {
        compile(ast)();
    };

    this.Init = function () {
        var scripts = document.getElementsByTagName("script");
        for (var i = 0; i < scripts.length; i++) {
            if (scripts[i].type === "text/brief") {
                var lines = scripts[i].innerHTML.split('\n');
                for (var j = 0; j < lines.length; j++) {
                    this.Execute(lines[j]);
                }
            }
        }
    };

    this.Primitive("define", function (quote, name) { define(quote, name); });

    return this;
} ();

// stack
Brief.Primitive("drop", function (x) { });
Brief.Primitive("dup", function (x) { var ret = [x, x]; ret.kind = "tuple"; return ret; });
Brief.Primitive("swap", function (y, x) { var ret = [x, y]; ret.kind = "tuple"; return ret; });

// combinators
Brief.Primitive("dip", function (x, q) { Brief.Run(q); Brief.Push(x); });
//Brief.Primitive("keep", function (q) { var x = Brief.Peek(); Brief.Run(q); Brief.Push(x); });
//Brief.Primitive("bi", function (x, p, q) { Brief.Push(x); Brief.Run(p); Brief.Push(x); Brief.Run(q); });
//Brief.Primitive("tri", function (x, p, q, r) { Brief.Push(x); Brief.Run(p); Brief.Push(x); Brief.Run(q); Brief.Push(x); Brief.Run(r); });
//Brief.Primitive("2bi", function (y, x, p, q) { Brief.Push(y); Brief.Push(x); Brief.Run(p); Brief.Push(y); Brief.Push(x); Brief.Run(q); });
//Brief.Primitive("bi*", function (y, x, p, q) { Brief.Push(y); Brief.Run(p); Brief.Push(x); Brief.Run(q); });

// arithmetic
Brief.Primitive("+", function (y, x) { return y + x; });
Brief.Primitive("-", function (y, x) { return y - x; });
Brief.Primitive("*", function (y, x) { return y * x; });
Brief.Primitive("/", function (y, x) { return y / x; });
Brief.Primitive("mod", function (y, x) { Brief.Push(y % x); });
//Brief.Primitive("neg", function (x) { return -x; });
//Brief.Primitive("abs", function (x) { return Math.abs(x); });

// comparison
Brief.Primitive("=", function (y, x) { Brief.Push(y == x); });
Brief.Primitive("<", function (y, x) { Brief.Push(y < x); });
Brief.Primitive(">", function (y, x) { Brief.Push(y > x); });
Brief.Primitive("<=", function (y, x) { Brief.Push(y <= x); });
Brief.Primitive(">=", function (y, x) { Brief.Push(y >= x); });

// boolean/conditional
Brief.Primitive("not", function (x) { Brief.Push(!x); });
Brief.Primitive("and", function (y, x) { Brief.Push(y && x); });
Brief.Primitive("or", function (y, x) { Brief.Push(y || x); });
Brief.Primitive("xor", function (y, x) { Brief.Push((y || x) && !(y && x)); });
Brief.Primitive("if", function (x, p, q) { Brief.Run(x ? p : q); });

// lists
Brief.Primitive("length", function (x) { return x.length; });
Brief.Primitive("cons", function (x, xs) { xs.unshift({ val: x, disp: x.toString() }); return xs; });
Brief.Primitive("snoc", function (xs) { var x = xs.shift(); Brief.Push(x.val); return xs; });

// jquery
Brief.Primitive("$", function (x) { Brief.Push($(x)); });
Brief.Primitive("append", function (y, x) { return y.append(x); });
Brief.Primitive("empty", function (x) { return x.empty(); });

// miscellaneous
Brief.Primitive("alert", function (x) { alert(x); });
Brief.Primitive("script", function (s) { eval(s); });
Brief.Primitive("eval", function (c) { Brief.Execute(c); })

Brief.Primitive("range", function (y, x) {
    var r = [];
    r.kind = "list";
    for (var i = x; i <= y; i++) {
	r.push({ kind: "literal", disp: i.toString(), val: i });
    }
    Brief.Push(r);
});

Brief.Primitive("map", function (xs, q) {
    for (var i = 0; i < xs.length; i++) {
        Brief.Push(xs[i].val);
        Brief.Run(q);
        var v = Brief.Pop();
        xs[i].val = v;
        xs[i].disp = v.toString();
    }
    Brief.Push(xs);
});

Brief.Primitive("filter", function (xs, q) {
    var f = [];
    f.kind = "list";
    for (var i = 0; i < xs.length; i++) {
        var x = xs[i];
        Brief.Push(x.val);
        Brief.Run(q);
        if (Brief.Pop())
            f.push(x);
    }
    Brief.Push(f);
});

Brief.Primitive("fold", function (xs, a, q) {
    for (var i = 0; i < xs.length; i++) {
        Brief.Push(xs[i].val);
        Brief.Push(a);
        Brief.Run(q);
        a = Brief.Pop();
    }
    Brief.Push(a);
});

Brief.Primitive("words", function () {
    var words = [];
    words.kind = "list";
    var dict = Brief.Words();
    for (var w in dict) {
        words.push(Brief.Word(dict[w]));
    }
    return words;
});

Brief.Execute('[ dup * ]                              "square"    define');
Brief.Execute('[ 1 [ * ] fold ]                       "prod"      define');
Brief.Execute('[ 1 range prod ]                       "factorial" define');
Brief.Execute('[ [ ] if ]                             "when"      define');
Brief.Execute('[ [ ] swap if ]                        "unless"    define');
Brief.Execute('[ true swap when ]                     "apply"     define');
Brief.Execute('[ 0 [ + ] fold ]                       "sum"       define');
Brief.Execute('[ drop drop ]                          "2drop"     define');
Brief.Execute('[ drop drop drop ]                     "3drop"     define');
Brief.Execute('[ 0 swap - ]                           "neg"       define');
Brief.Execute('[ dup 0 < [ neg ] when ]               "abs"       define');
Brief.Execute('[ swap drop ]                          "nip"       define');
Brief.Execute('[ [ 2drop ] dip ]                      "2nip"      define');
Brief.Execute('[ [ dup ] dip swap ]                   "over"      define');
Brief.Execute('[ over over ]                          "2dup"      define');
Brief.Execute('[ [ over ] dip swap ]                  "pick"      define');
Brief.Execute('[ pick pick pick ]                     "3dup"      define');
Brief.Execute('[ [ dup ] dip ]                        "dupd"      define');
Brief.Execute('[ [ swap ] dip ]                       "swapd"     define');
Brief.Execute('[ swapd swap ]                         "rot"       define');
Brief.Execute('[ rot rot ]                            "-rot"      define');
Brief.Execute('[ swap [ dip ] dip ]                   "2dip"      define');
Brief.Execute('[ swap [ 2dip ] dip ]                  "3dip"      define');
Brief.Execute('[ swap [ 3dip ] dip ]                  "4dip"      define');
Brief.Execute('[ dupd dip ]                           "keep"      define');
Brief.Execute('[ [ 2dup ] dip 2dip ]                  "2keep"     define');
Brief.Execute('[ [ 3dup ] dip 3dip ]                  "3keep"     define');
Brief.Execute('[ [ keep ] dip apply ]                 "bi"        define');
Brief.Execute('[ [ sum ] [ length ] bi / ]            "average"   define');
Brief.Execute('[ [ 2keep ] dip apply ]                "2bi"       define');
Brief.Execute('[ [ 3keep ] dip apply ]                "3bi"       define');
Brief.Execute('[ [ keep ] 2dip [ keep ] dip apply ]   "tri"       define');
Brief.Execute('[ [ 2keep ] 2dip [ 2keep ] dip apply ] "2tri"      define');
Brief.Execute('[ [ 3keep ] 2dip [ 3keep ] dip apply ] "3tri"      define');
Brief.Execute('[ [ dip ] dip apply ]                  "bi*"       define');
Brief.Execute('[ [ 2dip ] dip apply ]                 "2bi*"      define');
Brief.Execute('[ [ 2dip ] 2dip [ dip ] dip apply ]    "tri*"      define');
Brief.Execute('[ [ 4dip ] 2dip [ 2dip ] dip apply ]   "2tri*"     define');
Brief.Execute('[ dup 2dip apply ]                     "bi@"       define');
Brief.Execute('[ dup 3dip apply ]                     "2bi@"      define');
Brief.Execute('[ dup 3dip dup 2dip apply ]            "tri@"      define');
Brief.Execute('[ dup 4dip apply ]                     "2tri@"     define');
Brief.Execute('[ bi@ and ]                            "both?"     define');
Brief.Execute('[ bi@ or ]                             "either?"   define');

$(document).ready(function () {
    Brief.Init();
});

/*
TODO:
- Support recursive definitions
*/