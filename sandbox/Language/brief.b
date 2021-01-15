assertEqual "lex brackets" ['test '\] '\} 'a '\{ 'is '\[ 'this] [lex "this [is {a}] test"]
assertEqual "lex strings" ['test "is a" 'this] [lex "this \"is a\" test"]
assertEqual "lex escaped chars" ["\b \f \n \r \t \\ x"] [lex "\"\b \f \n \r \t \\ \x\""]

let 'whitespace? [any? swap [" " '\r '\n '\t] fry [= _] dup]

let 'lex [tokenize rot [] [] split
    let 'tokenize [cond [[2drop token]                                [empty?]
                         [tokenize token drop]                        [whitespace? snoc]
                         [tokenize token dip [cons] swap dip [token]] [brackets?]
                         [str drop]                                        [and dip [= '" dup] rot 2dip [empty?]]
                         [tokenize addChar]]]
    let 'str [cond [[tokenize token drop] [= '" dup snoc]
                    [str addChar unescape]]]
    let 'unescape [cond [['\b drop] [= 'b dup]
                         ['\f drop] [= 'f dup]
                         ['\n drop] [= 'n dup]
                         ['\r drop] [= 'r dup]
                         ['\t drop] [= 't dup]]]
    let 'addChar [dip [cons] swap]
    let 'emptyToken? [swap dip [empty?]]
    let 'brackets? [any? swap ["[" "]" "{" "}" ] fry [= _] dup]
    let 'token [swap [] if [drop] [dip [cons] swap join reverse] empty? swap]]


let 'cond [if [drop] [if [apply head] [pair] = 1 count] empty?
    let 'pair [if [apply nip] [cond drop] rot dip [dip snoc] snoc]]

drop [
]
