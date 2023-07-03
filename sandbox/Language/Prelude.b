'pi 3.14159 let
'e 2.71828 let

'sq [dup *] let
'cube [dup dup * *] let

'sign [-1 max 1 min] let
'min [2dup > [swap] when drop] let
'max [2dup < [swap] when drop] let

'both? [bi@ and] let
'either? [bi@ or] let
'neither? [either? not] let

'bi [[keep] dip apply] let
'2bi [[2keep] dip apply] let
'3bi [[3keep] dip apply] let
'bi* [[dip] dip apply] let
'2bi* [[2dip] dip apply] let
'bi@ [dup bi*] let
'2bi@ [dup 2bi*] let

'tri [[[keep] dip keep] dip apply] let
'2tri [[[2keep] dip 2keep] dip apply] let
'3tri [[[3keep] dip 3keep] dip apply] let
'tri* [[[2dip] dip dip] dip apply] let
'2tri* [[4dip] 2dip 2bi*] let
'tri@ [dup dup tri*] let
'2tri@ [dup dup 2tri*] let

'2drop [drop drop] let
'3drop [drop 2drop] let

'2dup [over over] let
'3dup [dup 2dup] let

'2dip [swap [dip] dip] let
'3dip [swap [2dip] dip] let
'4dip [swap [3dip] dip] let

'keep [[dup] dip dip] let
'2keep [[2dup] dip 2dip] let
'3keep [[3dup] dip 3dip] let

'clear [[] '_stack !map] let
'depth ['_stack @map count nip] let
'over [[dup] dip swap] let
'2over [pick pick] let
'nip [swap drop] let
'tuck [swap over] let
'rot [[swap] dip swap] let
'-rot [swap [swap] dip] let

'sym? [type 'sym =] let
'str? [type 'str =] let
'num? [type 'num =] let
'list? [type 'list =] let
'map? [type 'map =] let
'word? [type 'word =] let

'apply [true swap when] let
'when [[] if] let
'unless [[] swap if] let

	'cond.pair [snoc dip snoc rot [nip apply] [drop cond] if] let
'cond [empty? [drop] [count 1 = [head apply] [cond.pair] if] if] let

'concat [[>list] bi@ compose join] let
'open [read lex parse apply] let

'neg [-1 *] let
'abs [dup 0 < [neg] when] let
'recip [1 swap /] let

'< [[=] [>] 2bi or not] let
'<= [[=] [<] 2bi or] let
'>= [[=] [>] 2bi or] let
'<> [= not] let

'empty? [count 0 =] let
'head [snoc nip] let
'tail [snoc drop] let

'do [dup 2dip] let
'while [[dup dip] dip rot [do while] [2drop] if] let
'until [[[not] compose] dip while] let

'swons [swap cons] let
'quote [[] swons] let
'compose [swap prepose] let

'curry [[quote] dip compose] let
'2curry [curry curry] let
'3curry [curry curry curry] let

'fold [[rot empty?] [-rot [snoc] 2dip dup dip] until 2drop] let
'map [[cons] compose [swap] prepose [] swap fold reverse] let
'flatmap [map [] [prepose] fold] let
'filter [[dup _ [quote] [drop []] if] fry flatmap] let
'any? [swap empty? [2drop false] [snoc pick apply [2drop true] [swap any?] if] if] let
'all? [[not] compose any? not] let

'reverse [[] [swons] fold] let

'even? [2 mod 0 =] let
'odd? [even? not] let

'sum [0 [+] fold] let
'product [1 [* ] fold] let

'inc [1 +] let
'dec [1 -] let

'range [[] -rot [dup _ >=] fry [dup dec [cons] dip] while drop] let

'factorial [1 range product] let

    'fry.fill [drop rot dup list? [quote] unless] let
    'fry.deepfry [dup list? [-rot [fry] 2dip rot quote] [quote] if] let
    'fry.hole? [dup '_ >sym =] let
'fry [[fry.hole? [fry.fill] [fry.deepfry] if] flatmap] let

'assertTrue [apply ['PASS] ["!!! FAIL"] if [_ " " _ "\n"] fry print clear] let
'assertFalse [[not] compose assertTrue] let
'assertEqual [rot [[apply] dip = quote] dip swap assertTrue] let

'test ['tests.b open] let

'true -1 let
'false 0 let

'break [_break] let

'time [stopwatch-reset apply stopwatch-elapsed] let
'steps [steps-reset apply steps-count] let
'perf [steps-reset stopwatch-reset apply stopwatch-elapsed steps-count] let

'get-stack [get-state '_stack @ nip] let
'get-dictionary [get-state '_dictionary @ nip] let

'set-stack ['_stack !map] let
'set-dictionary ['_dictionary !map] let
'set-continuation ['_continuation !map] let

'save-value [[serialize] dip save] let
'load-value [load deserialize] let

'save-state [[get-state [] '_continuation !] dip save-value] let
'load-state [load-value set-state] let

'clear-dictionary [get-dictionary >list [snoc drop snoc nip word?] filter >map set-dictionary] let
'find [get-dictionary swap @ nip] let

'repeat [over 0 > [swap dec over [apply] 2dip repeat] [2drop] if] let

'take [[] -rot [snoc swap [cons] dip] repeat swap reverse] let
'skip [[tail] repeat] let
