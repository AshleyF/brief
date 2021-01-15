assertEqual "lex brackets" ['test '\] '\} 'a '\{ 'is '\[ 'this] [lex "this [is {a}] test"]
assertEqual "lex regular strings" ['test "'foo is a" 'this] [lex "this \"foo is a\" test"]
assertEqual "lex tick strings" ['baz ''bar 'foo] [lex "foo 'bar baz"]
assertEqual "lex escaped chars" ["'\b \f \n \r \t \\ x"] [lex "\"\b \f \n \r \t \\ \x\""]

let 'whitespace? [any? swap [" " '\r '\n '\t] fry [= _] dup]

let 'lex [tokenize rot [] [] split
    let 'tokenize [cond [[done]                     [empty?]
                         [tokenize token drop]      [whitespace? snoc]
                         [tokenize singleCharToken] [brackets?]
                         [tick addChar]             [firstCharIs? '']
                         [str addChar '' drop]      [firstCharIs? '"]
                         [tokenize addChar]]
        let 'firstCharIs? [apply fry [and dip [= _ dup] rot 2dip [empty?]]]]
    let 'str [cond [[tokenize token drop]  [= '" dup snoc]
                    [str addChar unescape] [= '\\ dup]
                    [str addChar]]]
    let 'tick [cond [[done]                     [empty?]
                     [tokenize token drop]      [whitespace? snoc]
                     [tokenize singleCharToken] [brackets?]
                     [tick addChar unescape]    [= '\\ dup]
                     [tick addChar]]]
    let 'unescape [cond [['\b drop] [= 'b dup]
                         ['\f drop] [= 'f dup]
                         ['\n drop] [= 'n dup]
                         ['\r drop] [= 'r dup]
                         ['\t drop] [= 't dup]]]
    let 'done [2drop token]
    let 'addChar [dip [cons] swap]
    let 'emptyToken? [swap dip [empty?]]
    let 'brackets? [any? swap ["[" "]" "{" "}" ] fry [= _] dup]
    let 'singleCharToken [token dip [cons] swap dip [token]]
    let 'token [swap [] if [drop] [dip [cons] swap join reverse] empty? swap]]


let 'cond [if [drop] [if [apply head] [pair] = 1 count] empty?
    let 'pair [if [apply nip] [cond drop] rot dip [dip snoc] snoc]]

drop [
]
