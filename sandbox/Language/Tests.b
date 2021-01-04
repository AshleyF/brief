assertEqual "Addition" 7 [+ 4 3]
assertEqual "Subtraction" -1 [- 4 3]
assertEqual "Multiplication" 12 [* 4 3]
assertEqual "Division" 0.5 [/ 4 2]

assertEqual "Reverse" [3 2 1] [reverse [1 2 3]]
assertEqual "Fry" [1 foo [2 bar [baz]]] [fry [_ foo [_ bar _]] 1 2 [[baz]]]

assertEqual "Depth" 3 [depth 1 2 3]
assertEqual "Clear" 0 [depth clear 1 2 3]

assertEqual "Drop" [] [@map '_stack drop 'foo]
assertEqual "Swap" [2 1] [@map '_stack swap 1 2]

assertEqual "Dip" [1 6] [@map '_stack dip [*] 1 2 3]
assertEqual "IfTrue" 5 [if [+] [*] true 2 3]
assertEqual "IfFalse" 6 [if [+] [*] false 2 3]
