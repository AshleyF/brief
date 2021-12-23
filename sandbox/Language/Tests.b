"+" [3 4 +] 7 assertEqual
"-" [3 4 -] -1 assertEqual
"*" [3 4 *] 12 assertEqual
"/" [2 4 /] 0.5 assertEqual

"reverse" [[1 2 3] reverse] [3 2 1] assertEqual
"fold" [[1 2 3] 1 [*] fold] 6 assertEqual
"map" [[1 2 3] [10 *] map] [10 20 30] assertEqual
"flatmap" [[1 2 3] [dup 10 * quote swons] flatmap] [1 10 2 20 3 30] assertEqual
"fry" [[[baz]] 2 1 [[_ bar _] foo _] fry] [[1 bar 2] foo [baz]] assertEqual

"depth" [1 2 3 depth] 3 assertEqual
"clear" [1 2 3 clear depth] 0 assertEqual

"drop" ['foo drop '_stack @map] [] assertEqual
"swap" [1 2 swap '_stack @map] [1 2] assertEqual

"dip" [2 3 4 [*] dip get-stack] [4 6] assertEqual
"if true" [2 3 true [+] [*] if] 5 assertEqual
"if false" [2 3 false [+] [*] if] 6 assertEqual
"if empty" [[] count ['FALSE] ['TRUE] if] 'TRUE assertEqual
"if not empty" [[1 2 3] count ['FALSE] ['TRUE] if] 'FALSE assertEqual
"when" [2 3 true [+] when] 5 assertEqual
"unless" [2 3 false [*] unless] 6 assertEqual
"unless empty" [[] count ['TRUE] unless] 'TRUE assertEqual
"cond" [4 [[dup 3 =] [123] [dup 4 =] [456]] cond] 456 assertEqual

"and true" [true true and] assertTrue
"and false" [false true and] assertFalse
"or true" [false true or] assertTrue
"or false" [false false or] assertFalse
"not true" [false not] assertTrue
"not false" [true not] assertFalse

"any? true" [[3 5 2 7] [even?] any?] assertTrue
"any? false" [[3 5 7 9] [even?] any?] assertFalse
"all? true" [[2 4 6 8] [even?] all?] assertTrue
"all? false" [[2 4 7 8] [even?] all?] assertFalse

"utf8" ["foo" >utf8 utf8>] 'foo assertEqual
"ieee754" [2.71828 >ieee754 ieee754>] 2.71828 assertEqual

"repeat" [7 3 [2 +] repeat] 13 assertEqual
"take" [[0 1 2 3 4 5] 3 take nip] [0 1 2] assertEqual
"skip" [[0 1 2 3 4 5] 3 skip] [3 4 5] assertEqual

"lex regular strings" ["this \"foo is a\" test" lex] ['this "'foo is a" 'test] assertEqual
"lex tick strings" ["foo 'bar baz" lex] ['foo ''bar 'baz] assertEqual
"lex escaped chars" ["\"\b \f \n \r \t \\ \x\"" lex] ["'\b \f \n \r \t  x"] assertEqual
"parse list" ["this ['is 123 a] test" lex parse] [this ['is 123 a] test] assertEqual
"parse packed list" ["this['is 123 a]test" lex parse] [this ['is 123 a] test] assertEqual
"parse nested lists" ["this [ 'is [ 123 456 ] a ] test" lex parse] [this ['is [123 456] a] test] assertEqual
"parse packed nested lists" ["this['is [123 456]a]test" lex parse] [this ['is [123 456] a] test] assertEqual
"parse map" ["{ 'foo 123 'bar 'hi }" lex parse] [{ 'foo 123 'bar 'hi }] assertEqual
"parse packed map" ["{'foo 123 'bar \"hi\"}" lex parse] [{ 'foo 123 'bar 'hi }] assertEqual

"serdes number" [2.71 dup serialize deserialize =] assertTrue
"serdes string" ["this is a test" dup serialize deserialize =] assertTrue
"serdes symbol" [[just-testing] snoc nip dup serialize deserialize =] assertTrue
"serdes list" [[1 2 3] dup serialize deserialize =] assertTrue
"serdes nested list" [[1 2 [3 4] 5 6] dup serialize deserialize =] assertTrue
"serdes map" [{ 'foo 123  'bar "just testing" } dup serialize deserialize =] assertTrue
"serdes nested map" [{ 'foo 123  'bar { 'baz 2.71 }} dup serialize deserialize =] assertTrue
"serdes machine state" [get-state dup serialize deserialize =] assertTrue