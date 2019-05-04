
# Brief Wasm

The [`.wat` format](http://webassembly.github.io/spec/core/text/index.html) is nice. Indeed, Lisp is my second favorite language family. First is Forth, of which BriefWasm is a dialect.

The BriefWasm code fragment `2 3 + 4 *` replaces:

```
i32.const 2
i32.const 3
i32.add
i32.const 4
i32.mul
```

Notice that literals become `const` and types are inferred. 

## Literals

Literals become `const` instructions. They may contain a decimal (`.`) to denote floats and may be suffixed with `L` for i64 and `f` for f32.

| Brief | Compiled |
| --- | --- |
| `123` | `i32.const 123` |
| `123L` | `i64.const 123` |
| `2.` | `f64.const 2.0` |
| `2.71` | `f64.const 123` |
| `2.71f` | `f32.const 123` |

The stack effect of these are tracked and used to infer instruction types to follow (and to validate code).

## Arithmetic

Binary arithmetic operations consume two values from the stack and produce a single result. The types must match.

| Brief | Compiled |
| --- | --- |
| `+` | `[i32|i64|f32|f64].add` |
| `-` | `[i32|i64|f32|f64].sub` |
| `*` | `[i32|i64|f32|f64].mul` |
| `/` | `[i32|i64|f32|f64].div_s` |
| `u/` | `[i32|i64].div_u` |
| `rem` | `[i32|i64].rem_s` |
| `urem` | `[i32|i64].rem_u` |
| `and` | `[i32|i64].and` |
| `or` | `[i32|i64].or` |
| `xor` | `[i32|i64].xor` |
| `shl` | `[i32|i64].shl` |
| `shr` | `[i32|i64].shr_s` |
| `ushr` | `[i32|i64].ushr_u` |
| `rotl` | `[i32|i64].rotl` |
| `rotr` | `[i32|i64].rotr` |
| `min` | `[f32|f64].min` |
| `max` | `[f32|f64].max` |
| `copysign` | `[f32|f64].copysign` |

The types are inferred by the state of the stack. Notice that some operators only apply to integer argument and some only to floating point.

Notice the `u` prefix on operations interpreting operands as _unsigned_ integers.

Additional unary arithmetic operators consuming and producing a _single_ stack value include:

| Brief | Compiled |
| --- | --- |
| `abs` | `[f32|f64].abs` |
| `neg` | `[f32|f64].neg` |
| `sqrt` | `[f32|f64].sqrt` |
| `ceil` | `[f32|f64].ceil` |
| `floor` | `[f32|f64].floor` |
| `trunc` | `[f32|f64].trunc` |
| `nearest` | `[f32|f64].nearest` |
| `clz` | `[i32|i64].clz` |
| `ctz` | `[i32|i64].ctz` |
| `popcnt` | `[i32|i64].popcnt` |

## Comparison

Comparison operators consume two values (of matching types) and produce a singe boolean integer value:

| Brief | Compiled |
| --- | --- |
| `=` | `[i32|f32|f32|f64].eq` |
| `<>` | `[i32|f32|f32|f64].ne` |
| `<` | `[i32|f32|f32|f64].lt_s` |
| `u<` | `[i32|f32].lt_u` |
| `>` | `[i32|f32|f32|f64].gt_s` |
| `u>` | `[i32|f32].gt_u` |
| `<=` | `[i32|f32|f32|f64].le_s` |
| `u<=` | `[i32|f32].le_u` |
| `>=` | `[i32|f32|f32|f64].ge_s` |
| `u>=` | `[i32|f32].ge_u` |

Finally, a unary zero-comparison operation:

| Brief | Compiled |
| --- | --- |
| `0=` | `[f32|f64].eqz` |

## Conversion

Conversion words change the top stack value from one type to another:

| Brief | Compiled (`i32`) | Compiled (`i64`) | Compiled (`f32`) | Compiled (`f64`) |
| --- | --- | --- | --- | --- |
| `>i32` | | `i32.wrap_i64` | `i32.trunc_f32_s` | `i32.trunc_f64_s` |
| `u>i32` | | | `i32.trunc_f32_u` | `i32.trunc_f64_u` |
| `>i64` | `i64.extend_i32_s` | | `i64.trunc_f32_s` | `i64.trunc_f64_s` |
| `u>i64` | `i64.extend_i32_u` | | `i64.trunc_f32_u` | `i64.trunc_f64_u` |
| `>f32` | `f32.convert_i32_s` | `f32.convert_i64_s` | | `f32.demote_f64` |
| `u>f32` | `f32.convert_i32_u` | `f32.convert_i64_u` | | |
| `>f64` | `f64.convert_i32_s` | `f64.convert_i64_s` | `f64.promote_f32` | |
| `u>f64` | `f64.convert_i32_u` | `f64.convert_i64_u` | | |

Reinterpretation words cast the top stack value:

| Brief | Compiled |
| --- | --- |
| `>>i32` | `i32.reinterpret_f32` |
| `>>i64` | `i64.reinterpret_f64` |
| `>>f32` | `f32.reinterpret_i32` |
| `>>f64` | `i64.reinterpret_i64` |

## Parametric

The `drop` word merely discards the top stack value (any type), while `select` produces one of the top two values depending on the third (top two types must match).

| Brief | Compiled |
| --- | --- |
| `drop` | `drop` |
| `select` | `select` |

## Memory

The following words fetch (`@`) and store (`!`) values (`i32`, `i64`, `f32`, `f64`) from/to memory, interpreted as signed or unsigned (`u`). They may also use a storage size smaller than the value (`32`-, `16`-, `8`-bit):

| Brief | Compiled |
| --- | --- |
| `@i32 <o> <a>` | `i32.load <offset> <align>`
| `@i64 <o> <a>` | `i64.load <offset> <align>`
| `@f32 <o> <a>` | `f32.load <offset> <align>`
| `@f64 <o> <a>` | `f64.load <offset> <align>`
| `8@i32 <o> <a>` | `i32.load8_s <offset> <align>`
| `u8@i32 <o> <a>` | `i32.load8_u <offset> <align>`
| `8@i64 <o> <a>` | `i64.load8_s <offset> <align>`
| `u8@i64 <o> <a>` | `i64.load8_u <offset> <align>`
| `16@i32 <o> <a>` | `i32.load16_s <offset> <align>`
| `u16@i32 <o> <a>` | `i32.load16_u <offset> <align>`
| `16@i64 <o> <a>` | `i64.load16_s <offset> <align>`
| `u16@i64 <o> <a>` | `i64.load16_u <offset> <align>`
| `32@i64 <o> <a>` | `i64.load32_s <offset> <align>`
| `u32@i64 <o> <a>` | `i64.load32_u <offset> <align>`
| `!i32 <o> <a>` | `i32.store <offset> <align>`
| `!i64 <o> <a>` | `i64.store <offset> <align>`
| `!f32 <o> <a>` | `f32.store <offset> <align>`
| `!f64 <o> <a>` | `f64.store <offset> <align>`
| `8!i32 <o> <a>` | `i32.store8 <offset> <align>`
| `8!i64 <o> <a>` | `i64.store8 <offset> <align>`
| `16!i32 <o> <a>` | `i32.store16 <offset> <align>`
| `16!i64 <o> <a>` | `i64.store16 <offset> <align>`
| `32!i64 <o> <a>` | `i64.store32 <offset> <align>`

Additionally, the current memory size may be retrieved or changed (growing returns the previous size or `-1` if allocation fails):

| Brief | Compiled |
| --- | --- |
| `memsize` | `memory.size`
| `memgrow` | `memory.grow`

## Variables

TODO

| Brief | Compiled |
| --- | --- |
| `` | `local.get <idx>`
| `` | `local.set <idx>`
| `` | `local.tee <idx>`
| `` | `global.get <idx>`
| `` | `global.set <idx>`

## Control

TODO

| Brief | Compiled |
| --- | --- |
| `` | `nop` |
| `` | `unreachable` |
| `` | `block <result> ... end` |
| `` | `loop <result> ... end` |
| `` | `if <result> ... else ... end` |
| `` | `br <label>` |
| `` | `br_if <label>` |
| `` | `br_table [<label> ...] <label>` |
| `` | `return` |
| `` | `call <func>` |
| `` | `call_indirect <type>` |

## TODO

* Integration with [Ryan Lamansky's WebAssembly for .NET](https://github.com/RyanLamansky/dotnet-webassembly)
* Support custom "name" section; giving names to modules/functions/locals for debugging