assertEqual "+" 7 [ + 4 3 ]
assertEqual "-" -1 [ - 4 3 ]
assertEqual "*" 12 [ * 4 3 ]
assertEqual "/" 0.5 [ / 4 2 ]

assertEqual "reverse" [ 3 2 1 ] [ reverse [ 1 2 3 ] ]
assertEqual "fry" [ 1 foo [ 2 bar [ baz ] ] ] [ fry [ _ foo [ _ bar _ ] ] 1 2 [ [ baz ] ] ]

assertEqual "depth" 3 [ depth 1 2 3 ]
assertEqual "clear" 0 [ depth clear 1 2 3 ]

assertEqual "drop" [ ] [ @map '_stack drop 'foo ]
assertEqual "swap" [ 2 1 ] [ @map '_stack swap 1 2 ]

assertEqual "dip" [ 1 6 ] [ @map '_stack dip [ * ] 1 2 3 ]
assertEqual "if true" 5 [ if [ + ] [ * ] true 2 3 ]
assertEqual "if false" 6 [ if [ + ] [ * ] false 2 3 ]
assertEqual "if empty" 'FALSE [ if [ 'TRUE ] [ 'FALSE ] count [ ] ]
assertEqual "if not empty" 'TRUE [ if [ 'TRUE ] [ 'FALSE ] count [ 1 2 3 ] ]
assertEqual "when" 5 [ when [ + ] true 2 3 ]
assertEqual "unless" 6 [ unless [ * ] false 2 3 ]
assertEqual "unless empty" 'TRUE [ unless [ 'TRUE ] count [ ] ]

assertTrue "and true" [ and true true ]
assertFalse "and false" [ and false true ]
assertTrue "or true" [ or false true ]
assertFalse "or false" [ or false false ]
assertTrue "not true" [ not false ]
assertFalse "not false" [ not true ]

assertTrue "any? true" [ any? [ even? ] [ 3 5 2 7 ] ]
assertFalse "any? false" [ any? [ even? ] [ 3 5 7 9 ] ]
assertTrue "all? true" [ all? [ even? ] [ 2 4 6 8 ] ]
assertFalse "all? false" [ all? [ even? ] [ 2 4 7 8 ] ]

