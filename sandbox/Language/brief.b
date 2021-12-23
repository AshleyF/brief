    'lex.match [[dup [_ =] fry] dip swap any?] let
    'lex.whitespace? [[" " '\r '\n '\t] lex.match] let
    'lex.delimiter? [['[ '] '{ '}] lex.match] let
    'lex.tokenize [[[empty?]               [lex.done]
                    [snoc lex.whitespace?] [drop lex.token lex.tokenize]
                    [lex.delimiter?]       [[lex.token] dip lex.addChar lex.token lex.tokenize]
                    ['' lex.firstCharIs?]  [lex.addChar lex.tick]
                    ['" lex.firstCharIs?]  [drop '' lex.addChar lex.str]
                                           [lex.addChar lex.tokenize]] cond] let
    'lex.firstCharIs? [[[empty?] 2dip rot [dup _ =] dip and] fry apply] let
    'lex.str [[[snoc dup '" =] [drop lex.token lex.tokenize]      
               [dup '\\ =]     [lex.unescape lex.addChar lex.str] 
                               [lex.addChar lex.str]] cond] let
    'lex.tick [[[empty?]               [lex.done]                      
                  [snoc lex.whitespace?] [drop lex.token lex.tokenize]       
                  [lex.delimiter?]       [rot count [-rot] dip 1 >
                                                               [[lex.token] dip cons lex.tokenize]
                                                               [swap [cons] dip lex.token lex.tokenize] if]
                  [dup '\\ =]            [lex.unescape lex.addChar lex.tick] 
                                         [lex.addChar lex.tick]] cond] let
    'lex.unescape [drop snoc [[dup 'b =] [drop '\b]
                              [dup 'f =] [drop '\f]
                              [dup 'n =] [drop '\n]
                              [dup 'r =] [drop '\r]
                              [dup 't =] [drop '\t]] cond] let
    'lex.done [lex.token 2drop] let
    'lex.addChar [swap [cons] dip] let
    'lex.token [swap empty? [drop] [reverse join swap [cons] dip] if [] swap] let
'lex [>list [] [] rot lex.tokenize reverse] let

    'parse.next [[[empty?]                  [drop]
                  [snoc dup '[ =]           [drop parse [reverse cons] dip parse.next]
                  [dup ['} =] ['] =] bi or] [drop reverse]
                  [dup '{ =]                [drop parse parse.buildMap parse.next]
                                            [swap [parse.convert cons] dip parse.next]] cond] let
    'parse.convert [[[dup >list head '' =] [>list tail join]
                     [dup >num?]           [nip]
                                           [>sym]] cond] let
    'parse.buildMap [{} rot parse.buildMap.build] let
    'parse.buildMap.build [empty? [drop swap [cons] dip]
                                   [snoc swap snoc swap [!] dip parse.buildMap.build] if] let
'parse [[] swap parse.next reverse] let
