let 'pi 3.14159
let 'e 2.71828

let 'sq [* dup]
let 'cube [* * dup dup]

let 'sign [min 1 max -1]
let 'min [drop when [swap] < 2dup]
let 'max [drop when [swap] > 2dup]

let 'both? [and bi@]
let 'either? [or bi@]
let 'neither? [not or bi@]

let 'bi [apply dip [keep]]
let '2bi [apply dip [2keep]]
let '3bi [apply dip [3keep]]
let 'bi* [apply dip [dip]]
let '2bi* [apply dip [2dip]]
let 'bi@ [bi* dup]
let '2bi@ [2bi* dup]

let 'tri [apply dip [keep dip [keep]]]
let '2tri [apply dip [2keep dip [2keep]]]
let '3tri [apply dip [3keep dip [3keep]]]
let 'tri* [apply dip [dip dip [2dip]]]
let '2tri* [2bi* 2dip [4dip]]
let 'tri@ [tri* dup dup]
let '2tri@ [2tri* dup dup]

let '2drop [drop drop]
let '3drop [2drop drop]

let '2dup [over over]
let '3dup [dup 2dup]

let '2dip [dip [dip] swap]
let '3dip [dip [2dip] swap]
let '4dip [dip [3dip] swap]

let 'keep [dip dip [dup]]
let '2keep [2dip dip [2dup]]
let '3keep [3dip dip [3dup]]

let 'over [swap dip [dup]]
let '2over [pick pick]
let 'nip [drop swap]
let 'tuck [over swap]
let 'rot [swap dip [swap]]
let '-rot [dip [swap] swap]

let 'apply [when swap true]
let 'when [if swap []]
let 'unless [if []]

let 'neg [- 0]
let 'abs [when [neg] < 0 dup]

let '< [not or 2bi [>] [=]]
let '<= [or 2bi [<] [=]]
let '>= [or 2bi [>] [=]]
let '<> [not =]

let 'empty? [= 0 count]
let 'head [nip snoc]
let 'tail [drop snoc]

let 'do [2dip dup]
let 'while [if [while do] [2drop] rot dip [dip dup]]
let 'until [while dip [compose [not]]]

let 'swons [cons swap]
let 'quote [swons []]

let 'fold [2drop until [dip dup 2dip [snoc] -rot] [empty? rot]]
let 'map [reverse fold swap [] compose swap [swap] compose [cons]]
let 'filter [fold [compose] [] map compose [if [quote] [[] drop]] compose swap [dup]]

let 'reverse [fold [swons] []]

let 'even? [= 0 mod swap 2]
let 'odd? [not even?]

let 'sum [fold [+] 0]
let 'product [fold [*] 1]

let '++ [+ 1]
let '-- [+ -1]

let 'range [drop while [dip [cons] -- dup] compose [<=] swons [dup] -rot []]

let 'factorial [product range 1]
