let 'pi 3.14159
let 'e 2.71828

let 'sq [ * dup ]
let 'cube [ * * dup dup ]

let 'sign [ min 1 max -1 ]
let 'min [ drop when [ swap ] > 2dup ]
let 'max [ drop when [ swap ] < 2dup ]

let 'both? [ and bi@ ]
let 'either? [ or bi@ ]
let 'neither? [ not or bi@ ]

let 'bi [ apply dip [ keep ] ]
let '2bi [ apply dip [ 2keep ] ]
let '3bi [ apply dip [ 3keep ] ]
let 'bi* [ apply dip [ dip ] ]
let '2bi* [ apply dip [ 2dip ] ]
let 'bi@ [ bi* dup ]
let '2bi@ [ 2bi* dup ]

let 'tri [ apply dip [ keep dip [ keep ] ] ]
let '2tri [ apply dip [ 2keep dip [ 2keep ] ] ]
let '3tri [ apply dip [ 3keep dip [ 3keep ] ] ]
let 'tri* [ apply dip [ dip dip [ 2dip ] ] ]
let '2tri* [ 2bi* 2dip [ 4dip ] ]
let 'tri@ [ tri* dup dup ]
let '2tri@ [ 2tri* dup dup ]

let '2drop [ drop drop ]
let '3drop [ 2drop drop ]

let '2dup [ over over ]
let '3dup [ dup 2dup ]

let '2dip [ dip [ dip ] swap ]
let '3dip [ dip [ 2dip ] swap ]
let '4dip [ dip [ 3dip ] swap ]

let 'keep [ dip dip [ dup ] ]
let '2keep [ 2dip dip [ 2dup ] ]
let '3keep [ 3dip dip [ 3dup ] ]

let 'clear [ !map '_stack [ ] ]
let 'depth [ nip count @map '_stack ]
let 'over [ swap dip [ dup ] ]
let '2over [ pick pick ]
let 'nip [ drop swap ]
let 'tuck [ over swap ]
let 'rot [ swap dip [ swap ] ]
let '-rot [ dip [ swap ] swap ]

let 'list? [ = 'list type ]
let 'sym? [ = 'sym type ]
let 'num? [ = 'num type ]
let 'str? [ = 'str type ]
let 'bool? [ = 'bool type ]
let 'list? [ = 'list type ]
let 'map? [ = 'map type ]

let 'apply [ when swap true ]
let 'when [ if swap [ ] ]
let 'unless [ if [ ] ]
let 'cond [ if [ drop ] [ if [ apply head ] [ cond.pair ] = 1 count ] empty? ]
    let 'cond.pair [ if [ apply nip ] [ cond drop ] rot dip [ dip snoc ] snoc ]

let 'concat [ join prepose bi@ [ split ] ]
let 'source [ apply parse lex read ]

let 'neg [ * -1 ]
let 'abs [ when [ neg ] < 0 dup ]
let 'recip [ / 1 swap ]

let '< [ not or 2bi [ > ] [ = ] ]
let '<= [ or 2bi [ < ] [ = ] ]
let '>= [ or 2bi [ > ] [ = ] ]
let '<> [ not = ]

let 'empty? [ = 0 count ]
let 'head [ nip snoc ]
let 'tail [ drop snoc ]

let 'do [ 2dip dup ]
let 'while [ if [ while do ] [ 2drop ] rot dip [ dip dup ] ]
let 'until [ while dip [ prepose [ not ] ] ]

let 'swons [ cons swap ]
let 'quote [ swons [ ] ]
let 'compose [ prepose swap ]

let 'curry [ prepose dip [ quote ] ]
let '2curry [ curry curry ]
let '3curry [ curry curry curry ]

let 'fold [ 2drop until [ dip dup 2dip [ snoc ] -rot ] [ empty? rot ] ]
let 'map [ reverse fold swap [ ] compose [ swap ] prepose [ cons ] ]
let 'flatmap [ fold [ prepose ] [ ] map ]
let 'filter [ flatmap fry [ if [ quote ] [ [ ] drop ] _ dup ] ]
let 'any? [ if [ false 2drop ] [ if [ true 2drop ] [ any? swap ] apply pick snoc ] empty? swap ]
let 'all? [ not any? prepose [ not ] ]

let 'reverse [ fold [ swons ] [ ] ]

let 'even? [ = 0 mod 2 ]
let 'odd? [ not even? ]

let 'sum [ fold [ + ] 0 ]
let 'product [ fold [ * ] 1 ]

let '++ [ + 1 ]
let '-- [ - 1 ]

let 'range [ drop while [ dip [ cons ] -- dup ] fry [ >= _ dup ] -rot [ ] ]

let 'factorial [ product range 1 ]

let 'fry [ flatmap [ if [ fry.fill ] [ fry.deepfry ] fry.hole? ] ]
    let 'fry.fill [ unless [ quote ] list? dup rot drop ]
    let 'fry.deepfry [ if [ quote rot 2dip [ fry ] -rot ] [ quote ] list? dup ]
    let 'fry.hole? [ = >sym '_ dup ]

let 'assertTrue [ clear print fry [ _ " " _ "\n" ] if [ 'PASS ] [ "!!! FAIL" ] apply swap ]
let 'assertFalse [ assertTrue dip [ prepose [ not ] ] ]
let 'assertEqual [ assertTrue dip [ quote = ] 2dip [ apply ] ]

let 'test [ source 'tests.b ]

let 'true -1
let 'false 0

let 'break [ _break ]

let 'time [ stopwatch-elapsed apply stopwatch-reset ]
let 'steps [ steps-count apply steps-reset ]
let 'perf [ steps-count stopwatch-elapsed apply stopwatch-reset steps-reset ]
