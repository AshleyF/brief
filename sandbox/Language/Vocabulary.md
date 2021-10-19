# Brief Vocabulary

These are the words defined in a Brief system with Prelude.b loaded. The `words` word also lists them

## Stack Words

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `depth` | -n | Get stack depth | Secondary |
| `clear` | - | Clear stack | Secondary |
| `dup` | x-xx | Duplicate top stack value | Primitive |
| `2dup` | xy-xyxy | Duplicate top two stack values | Secondary |
| `3dup` | xyz-xyzxyz | Duplicate top three stack values | Secondary |
| `drop` | x- | Drop top stack value | Primitive |
| `2drop` | xy- | Drop top two stack values | Secondary |
| `3drop` | xyz- | Drop top three stack values | Secondary |
| `swap` | xy-yx | Swap top two stack values | Primitive |
| `pick` | xyz-zxyz | Duplicate third stack value | Primitive |
| `over` | xy-yxy | Duplicate second stack value | Secondary |
| `2over` | xyz-yzxyz | Duplicate second and third stack values | Secondary |
| `nip` | xy-x | Drop second stack value | Secondary |
| `tuck` | xy-xyx | Duplicate top stack value below second value | Secondary |
| `rot` | xyz-zxy | Rotate third stack value to the top | Secondary |
| `-rot` | xyz-yzx | Rotate top stack value to third spot | Secondary |

## Quotation Application

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `apply` | q- | Apply quotation | Secondary |
| `dip` | qx-x | Apply quotation "under" second stack value | Primitive |
| `2dip` | qxy-xy | Apply quotation "under" first two stack value | Secondary |
| `3dip` | qxyz-xyz | Apply quotation "under" first three stack value | Secondary |
| `3dip` | qxyzw-xyzw | Apply quotation "under" first four stack value | Secondary |
| `keep` | qx-x | Apply quotation to top stack value and restore value | Secondary |
| `2keep` | qxy-xy | Apply quotation to top two stack values and restore values | Secondary |
| `3keep` | qxyz-xyz | Apply quotation to top three stack values and restore values | Secondary |
| `bi` | qrx- | Apply each quotation to following value | Secondary |
| `2bi` | qrxy- | Apply each quotation to following two values | Secondary |
| `3bi` | qrxyz- | Apply each quotation to following three values | Secondary |
| `bi*` | qrx- | Apply first quotation (q) to value (x), then second quotation (r) to value (x) | Secondary |
| `2bi*` | qrxy- | Apply first quotation (q) to values (xy), then second quotation (r) to values (xy) | Secondary |
| `bi@` | qxy- | Apply quotation to each value (x, y) | Secondary |
| `2bi@` | qxyzw- | Apply quotation to each pair of values (xy, zw) | Secondary |
| `tri` | pqrx- | Apply each quotation to following value | Secondary |
| `2tri` | pqrxy- | Apply each quotation to following two values | Secondary |
| `3tri` | pqrxyz- | Apply each quotation to following three values | Secondary |
| `tri*` | pqrx- | Apply first quotation (p) to value (x), then second quotation (q) to value (x), then third quotation (r) to value (x) | Secondary |
| `2tri*` | pqrxy- | Apply first quotation (p) to values (xy), then second quotation (q) to values (xy), then third quotation (r) to values (xy) | Secondary |
| `tri@` | qxyz- | Apply quotation to each value (x, y, z) | Secondary |
| `2tri@` | qxyzwvu- | Apply quotation to each pair of values (xy, zw, vu) | Secondary |
| `both?` | qq-b | Apply two quotations and determine whether both are `true` | Secondary |
| `either?` | qq-b | Apply two quotations and determine whether either are `true` | Secondary |
| `neither?` | qq-b | Apply two quotations and determine whether neither are `true` | Secondary |

## List and Map

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `map` | ql-l | Map a quotation (q) over a list | Secondary |
| `map-many` | ql-l | Map a list-producing quotation (q) over a list; composing the result | Secondary |
| `filter` | ql-l | Filter a list by a boolean expression (q) | Secondary |
| `fold` | qvl-x | Fold a quotation (q) along with a seed (v) over a list | Secondary |
| `sum` | l-n | Compute the sum of a list of Numbers | Secondary |
| `product` | l-n | Compute the product of a list of Numbers | Secondary |
| `empty?` | x- | Determine whether List or Map is empty, while keeping the collection | Secondary |
| `count` | x-n | Count of values within List or Map, while keeping the collection | Primitive |
| `cons` | vl-l | Cons value onto head of List (tail) | Primitive |
| `snoc` | l-vl | Reverse cons List into head and tail | Primitive |
| `swons` | lv-l | Cons value (v) onto head of List (equivalent to `cons swap`) | Secondary |
| `quote` | v-l | Quote a value into a single-element list | Secondary |
| `compose` | qr-q | Compose two quotations (`Lists`) into one | Primitive |
| `prepose` | qr-q | Compose two quotations in reverse order (`Lists`) into one | Secondary |
| `curry` | qx-q | Compose value (x) onto end of quotations (q) | Secondary |
| `2curry` | qxy-q | Compose two values (x, y) onto end of quotations (q) | Secondary |
| `3curry` | qxyz-q | Compose three values (x, y, z) onto end of quotations (q) | Secondary |
| `fry` | q...-q | Compose quotation containing _ "holes"; taking quotations from stack | Secondary |
| `head` | - | Retrieve first element of List | Secondary |
| `tail` | - | Retrieve all but first element of List | Secondary |
| `key?` | km-bm | Determine whether key is in Map, while keeping Map | Primitive |
| `@` | km-vm | Fetch value for key in Map, while keeping Map | Primitive |
| `!` | kvm-m | Store key/value in Map, while keeping Map | Primitive |
| `!map` | nv- | Store named value in `State.Map` | Primitive |
| `@map` | n- | Fetch named value from `State.Map` | Primitive |
| `split` | s-l | Split Symbol or String into List of single-character Strings | Primitive |
| `join` | l-s | Join List of Strings into single String | Primitive |
| `concat` | ss-s | Concatenate two strings | Secondary |
| `reverse` | l-l | Reverse List | Secondary |

## Comparison and Conditionals

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `true` | -b | True constant (`-1`) | Secondary |
| `false` | -b | False constant (`0`) | Secondary |
| `=` | xy-b | Compare top two stack values, returning `true` if equal | Primitive |
| `>` | xy-b | Compare top two stack values, returning `true` if top value is greater than second value | Primitive |
| `<` | xy-b | Compare top two stack values, returning `true` if top value is less than second value | Secondary |
| `<>` | xy-b | Compare top two stack values, returning `true` if not equal | Secondary |
| `>=` | xy-b | Compare top two stack values, returning `true` if top value is greater than or equal to second value | Secondary |
| `<=` | xy-b | Compare top two stack values, returning `true` if top value is less than or equal to second value | Secondary |
| `and` | xy-b | Boolean and of top two stack values | Primitive |
| `or` | xy-b | Boolean or of top two stack values | Primitive |
| `not` | x-b | Boolean not of top stack value | Primitive |
| `if` | qrb- | Apply one or the other quotation depending on boolean value | Primitive |
| `cond` | q-* | Walks a list of quotations representing cases/default. | Secondary |
| `when` | qb - | Apply quotation when boolean value is `true` | Secondary |
| `unless` | qb - | Apply quotation unless boolean value is `true` | Secondary |

## Math

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `+` | xy-n | Add top two stack values | Primitive |
| `-` | xy-n | Subtract top stack value from second stack value | Primitive |
| `*` | xy-n | Multiply top two stack values | Primitive |
| `/` | xy-n | Divide second stack value by top stack value | Primitive |
| `mod` | xy-n | Modulus second stack value by top stack value | Primitive |
| `++` | n-n | Increment number | Secondary |
| `--` | n-n | Decrement number | Secondary |
| `recip` | x-n | Compute reciprocal (1/x) of top stack value | Secondary |
| `neg` | - | Negate top stack value | Secondary |
| `abs` | - | Compute absolute value of top stack value | Secondary |
| `sign` | n-n | Determine sign of top stack value (-1, 1) | Secondary |
| `min` | nn-n | Compute minimum of top two stack values | Secondary |
| `max` | nn-n | Compute maximum of top two stack values | Secondary |
| `sq` | n-n | Square top stack value | Secondary |
| `cube` | n-n | Cube top stack value | Secondary |
| `pi` | -n | Math constant | Secondary |
| `e` | -n | Math constant | Secondary |
| `sqrt` | n-n | Compute square root of top stack value | Primitive |
| `cbrt` | n-n | Compute cube root of top stack value | Primitive |
| `sin` | n-n | Compute sine of top stack value | Primitive |
| `cos` | n-n | Compute cosine of top stack value | Primitive |
| `tan` | n-n | Compute tangent of top stack value | Primitive |
| `sinh` | n-n | Compute hyperbolic sine of top stack value | Primitive |
| `cosh` | n-n | Compute hyperbolic cosine of top stack value | Primitive |
| `tanh` | n-n | Compute hyperbolic tangent of top stack value | Primitive |
| `asin` | n-n | Compute angle whose sine is top stack value | Primitive |
| `acos` | n-n | Compute angle whose cosine is top stack value | Primitive |
| `atan` | n-n | Compute angle whose tangent is top stack value | Primitive |
| `asinh` | n-n | Compute angle whose hyperbolic sine is top stack value | Primitive |
| `acosh` | n-n |  Compute angle whose hyperbolic cosine is top stack value| Primitive |
| `atanh` | n-n |  Compute angle whose hyperbolic tangen is top stack value| Primitive |
| `atan2` | xy-n |  Compute angle whose tangent is the quotient of top two stack values| Primitive |
| `ceil` | n-n |  Compute ceiling of top stack value| Primitive |
| `floor` | n-n | Compute floor of top stack value | Primitive |
| `trunc` | n-n | Truncate top stack value | Primitive |
| `round` | n-n | Round top stack value | Primitive |
| `pow` | yx-n | Compute x raised to the power of y | Primitive |
| `ln` | n-n | Compute natural logarithm of top stack value | Primitive |
| `log` | n-n | Compute base10 logarithm of top stack value | Primitive |
| `log2` | n-n | Compute base2 logarithm of top stack value | Primitive |
| `even?` | n-b | Determine whether a number is even | Secondary |
| `odd?` | n-b | Determine whether a number is odd | Secondary |

## Casting

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `type` | x-s | Determine the type of top stack value ('sym, 'num, 'str, 'bool, 'list, 'map) | Primitive |
| `list?` | x-b | Determine whether top of stack value is a List | Secondary |
| `sym?` | x-b | Determine whether top of stack value is a Symbol | Secondary |
| `num?` | x-b | Determine whether top of stack value is a Number | Secondary |
| `str?` | x-b | Determine whether top of stack value is a String | Secondary |
| `bool?` | x-b | Determine whether top of stack value is a Boolean | Secondary |
| `list?` | x-b | Determine whether top of stack value is a List | Secondary |
| `map?` | x-b | Determine whether top of stack value is a Map | Secondary |
| `>sym` | x-s | Cast String (not including white space), Boolean, or Number to Symbol | Primitive |
| `>str` | x-s | Cast value to string in Brief literal source form | Primitive |
| `>num` | x-n | Cast Symbol, String, Boolean (-1, 0), List, or Map (lengths) to Number | Primitive |
| `>num?` | x-?n | Try to parse string as Number, returning true and Number or false | Primitive |

## Loops

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `do` | qr- | Interate `while`/`until` loop, applying the second quotation (r) once| Secondary |
| `while` | qr- | While second quotation (r) is `true`, apply first quotation (q) | Secondary |
| `until` | qr- | Until second quotation (r) is `true`, apply first quotation (q) | Secondary |

## Serialization

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `load` | s-r | Load binary file as raw bytes. | Primitive |
| `store` | s r- | Store raw bytes to binary file. | Primitive |
| `read` | s-s | Read text file as a string. | Primitive |
| `source` | n- | Parse named Brief source file (path, conventionally ending with .b extension) | Primitive |
| `save` | s- | Save image of machine state (path, conventionally ending with .i extension). | Primivite |
| `open` | s- | Open image of machine state (path, conventionally ending with .i extension). | Primitive |

## Miscellaneous

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `let` | nx- | Define named quotation or value as secondary word | Primitive |
| `print` | - | Print top of stack | Primitive |
| `state` | - | Print machine state | Primitive |
| `post` | nq- | Post quotation to named actor | Primitive |
| `lex` | s-l | Tokenize source. | Primitive (redefined in brief.b) |
| `parse` | l-l | Parse tokens into structured, typed code. | Primitive (redefined in brief.b) |
| `range` | nm-l | Create a list of Numbers ranging from n to m | Secondary |
| `factorial` | n-n | Compute the factorial of a Numeber | Secondary |
| `words` | - | Display primitive and secondary words | Primitive |
| `word` | n- | Display word definition | Primitive |

## Testing

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `assertTrue` | nq- | Assert named quotation evaluates to `true` | Secondary |
| `assertFalse` | nq- | Assert named quotation evaluates to `false` | Secondary |
| `assertEqual` | nqr- | Assert named quotation (r) evaluates to match first quotation (q) | Secondary |
| `test` | - | Load unit tests (`Test.b`)| Secondary |
| `stopwatch-reset` | - | Reset and start stopwatch. | Primitive |
| `stopwatch-elapsed` | n | Return elapsed milliseconds. | Primitive |
| `time` | q-n | Apply quotation while running stopwatch; returning elapsed milliseconds. | Secondary |
| `steps-reset` | - | Reset interpreter step count. | Primitive |
| `steps-count` | n | Return current interpreter step count. | Primitive |
| `steps` | q-n | Apply quotation while returning counted steps. | Secondary |
| `perf` | q-nn | Apply quotation while running stopwatch; returning counted steps and elapsed milliseconds. | Secondary |

## Internal

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `_break` | - | Break point; pausing interpretation | Primitive |

## Tesla Actor

The Tesla actor [controls a Tesla vehicle](https://github.com/AshleyF/tesla).

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `auth` | vnp- | Authenticate with Tesla service, given VIN, name, and password | Primitive |
| `wake` | - | Wake up vehicle | Primitive |
| `honk` | - | Make vehicle honk | Primitive |
| `flash` | - | Make vehicle flash headlights | Primitive |
| `lock` | - | Lock vehicle doors | Primitive |
| `unlock` | - | Unlock vehicle doors | Primitive |
| `startac` | - | Start vehicle HVAC system | Primitive |
| `stopac` | - | Stop vehicle HVAC system | Primitive |
| `charge?` | - | Display vehicle charge information | Primitive |
| `climate?` | - | Display vehicle climate information | Primitive |
| `drive?` | - | Display vehicle drive state information | Primitive |
| `gui?` | - | Display vehicle GUI information | Primitive |
| `vehicle?` | - | Display vehicle information | Primitive |
| `charge` | x- | Set vehicle charge limit (0-100) | Primitive |
| `temperature` | x- | Set vehicle temperature | Primitive |

Commonly, a `tesla-auth` word is defined in application parameters containing the actual credentials:

```brief
let 'tesla-auth [post 'tesla [auth '<my_vin> '<my_user> '<my_password>]]
```

## Trigger Actor (IFTTT)

The Trigger actor works with If-This-Then-That ["webhooks"](https://ifttt.com/maker_webhooks).

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `hook` | kevvv- | Trigger an IFTTT Webhook using a key, event name and values | Primitive |

Commonly, an `ifttt-key` word is defined in application parameters containing the actual key, and an `ifttt` word is defined for easier triggering of events that take no arguments (both defined in the actor itself):

```brief
post 'trigger [let 'ifttt-key '<my_webhook_key>]
post 'trigger [let 'ifttt [hook ifttt-key ' ' ']]
```

Further, trigger-specific words may be defined to make working with triggers more natural (e.g. `color-lights 'red`):

```brief
post 'trigger [let 'color-lights [hook ifttt-key 'all-lights-color pick ' ']]
```

## Remote Actor

The Remote actor sends and receives Brief over TCP (e.g. `connect '127.0.0.1 11411`).

| Word | Stack | Description | Type |
| --- | --- | --- | --- |
| `connect` | hp- | Connect to remote TCP host on port. | Primitive |
