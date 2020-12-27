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
let 'bi@ [apply 2dip dup]
let '2bi@ [apply 3dip dup]

let 'tri [apply dip [keep] 2dip [keep]]
let '2tri [apply dip [2keep] 2dip [2keep]]
let '3tri [apply dip [3keep] 2dip [3keep]]
let 'tri* [apply dip [dip] 2dip [2dip]]
let '2tri* [apply dip [2dip] 2dip [4dip]]
let 'tri@ [apply 2dip dup 3dip dup]
let '2tri@ [apply 4dip dup]

let '3keep [3dip dip [3dup]]
let '2keep [2dip dip [2dup]]
let 'keep [dip dip [dup]]

let '3drop [2drop drop]
let '2drop [drop drop]

let '3dup [dup 2dup]
let '2dup [over over]

let '4dip [dip [3dip] swap]
let '3dip [dip [2dip] swap]
let '2dip [dip [dip] swap]

let 'over [swap dip [dup]]
let 'nip [drop swap]
let 'tuck [over swap]

let 'apply [when swap true]
let 'when [if swap []]
let 'unless [if []]

let 'neg [- 0]
let 'abs [when [neg] < 0 dup]

let '< [not or 2bi [>] [=]]
