module Wasm // http://webassembly.org/docs/binary-encoding/

open System
open System.Text

// --------------------------------------------------------------------------------

type Value = I32 | I64 | F32 | F64 // value_type

type FuncType = { // func_type
    Parameters: Value seq
    Returns: Value option } // future multiple supported: http://webassembly.org/docs/future-features/#multiple-return

type ImportName = {
    Module: string
    Field: string }

type ResizableLimits = int * int option

type ImportEntry =
    | Function of ImportName * int
    | Table of ImportName * ResizableLimits // currently only anyfunc supported
    | Memory of ImportName * ResizableLimits
    | Global of ImportName * Value * bool

type Instruction = // using int rather than uint32 throughout for convenience
    // control flow
    | Unreachable
    | Nop
    | Block of Value option // block type/empty
    | Loop of Value option // block type/empty
    | If of Value option // block type/empty
    | Else
    | End
    | Br of int // relative depth
    | BrIf of int // relative depth
    | BrTable of int * int seq * int // count, table, default
    | Return
    // call
    | Call of int // function index
    | CallIndirect of int // type index, (reserved future use)
    // parametric
    | Drop
    | Select
    // variable
    | GetLocal of int // index
    | SetLocal of int // index
    | TeeLocal of int // index
    | GetGlobal of int // index
    | SetGlobal of int // index
    // memory
    | LoadI32 of int * int // flags, offset
    | LoadI64 of int * int // flags, offset
    | LoadF32 of int * int // flags, offset
    | LoadF64 of int * int // flags, offset
    | LoadByteI32 of int * int // flags, offset
    | LoadByteUnsignedI32 of int * int // flags, offset
    | LoadShortI32 of int * int // flags, offset
    | LoadShortUnsignedI32 of int * int // flags, offset
    | LoadByteI64 of int * int // flags, offset
    | LoadByteUnsignedI64 of int * int // flags, offset
    | LoadShortI64 of int * int // flags, offset
    | LoadShortUnsignedI64 of int * int // flags, offset
    | LoadIntI64 of int * int // flags, offset
    | LoadIntUnsignedI64 of int * int // flags, offset
    | StoreI32 of int * int // flags, offset
    | StoreI64 of int * int // flags, offset
    | StoreF32 of int * int // flags, offset
    | StoreF64 of int * int // flags, offset
    | StoreByteI32 of int * int // flags, offset
    | StoreShortI32 of int * int // flags, offset
    | StoreByteI64 of int * int // flags, offset
    | StoreShortI64 of int * int // flags, offset
    | StoreIntI64 of int * int // flags, offset
    | CurrentMemory // reserved (future)
    | GrowMemory // reserved (future)
    // constants
    | ConstI32 of int
    | ConstI64 of int64
    | ConstF32 of single
    | ConstF64 of double
    // comparison
    | EqualZeroI32 // eqz
    | EqualI32 // eq
    | NotEqualI32 // neq
    | LessI32 // lt_s
    | LessUnsignedI32 // lt_u
    | GreaterI32 // gt_s
    | GreaterUnsignedI32 // gt_u
    | LessOrEqualI32 // le_s
    | LessOrEqualUnsignedI32 // le_u
    | GreaterOrEqualI32 // ge_s
    | GreaterOrEqualUnsignedI32 // ge_u
    // ----------------------------------------
    | EqualZeroI64 // eqz
    | EqualI64 // eq
    | NotEqualI64 // neq
    | LessI64 // lt_s
    | LessUnsignedI64 // lt_u
    | GreaterI64 // gt_s
    | GreaterUnsignedI64 // gt_u
    | LessOrEqualI64 // le_s
    | LessOrEqualUnsignedI64 // le_u
    | GreaterOrEqualI64 // ge_s
    | GreaterOrEqualUnsignedI64 // ge_u
    // ----------------------------------------
    | EqualF32 // eq
    | NotEqualF32 // neq
    | LessF32 // lt_s
    | GreaterF32 // gt_s
    | LessOrEqualF32 // le_s
    | GreaterOrEqualF32 // ge_s
    // ----------------------------------------
    | EqualF64 // eq
    | NotEqualF64 // neq
    | LessF64 // lt_s
    | GreaterF64 // gt_s
    | LessOrEqualF64 // le_s
    | GreaterOrEqualF64 // ge_s
    // numeric
    | ClzI32 // i32.clz
    | CtzI32 // i32.ctz
    | PopCountI32 // i32.popcnt
    | AddI32 // i32.add
    | SubI32 // i32.sub
    | MulI32 // i32.mul
    | DivI32 // i32.div_s
    | DivUnsignedI32 // i32.div_u
    | RemI32 // i32.rem_s
    | RemUnsignedI32 // i32.rem_u
    | AndI32 // i32.and
    | OrI32 // i32.or
    | XorI32 // i32.xor
    | ShiftLeftI32 // i32.shl
    | ShiftRightI32 // i32.shr_s
    | ShiftRightUnsignedI32 // i32.shr_u
    | RotateLeftI32 // i32.rotl
    | RotateRightI32 // i32.rotr
    // ----------------------------------------
    | ClzI64 // i64.clz
    | CtzI64 // i64.ctz
    | PopCountI64 // i64.popcnt
    | AddI64 // i64.add
    | SubI64 // i64.sub
    | MulI64 // i64.mul
    | DivI64 // i64.div_s
    | DivUnsignedI64 // i64.div_u
    | RemI64 // i64.rem_s
    | RemUnsignedI64 // i64.rem_u
    | AndI64 // i64.and
    | OrI64 // i64.or
    | XorI64 // i64.xor
    | ShiftLeftI64 // i64.shl
    | ShiftRightI64 // i64.shr_s
    | ShiftRightUnsignedI64 // i64.shr_u
    | RotateLeftI64 // i64.rotl
    | RotateRightI64 // i64.rotr
    // ----------------------------------------
    | AbsF32 // f32.abs
    | NegateF32 // f32.neg
    | CeilingF32 // f32.ceil
    | FloorF32 // f32.floor
    | TruncateF32 // f32.trunc
    | NearestF32 // f32.nearest
    | SqrtF32 // f32.sqrt
    | AddF32 // f32.add
    | SubF32 // f32.sub
    | MulF32 // f32.mul
    | DivF32 // f32.div
    | MinF32 // f32.min
    | MaxF32 // f32.max
    | CopySignF32 // f32.copysign
    // ----------------------------------------
    | AbsF64 // f64.abs
    | NegateF64 // f64.neg
    | CeilingF64 // f64.ceil
    | FloorF64 // f64.floor
    | TruncateF64 // f64.trunc
    | NearestF64 // f64.nearest
    | SqrtF64 // f64.sqrt
    | AddF64 // f64.add
    | SubF64 // f64.sub
    | MulF64 // f64.mul
    | DivF64 // f64.div
    | MinF64 // f64.min
    | MaxF64 // f64.max
    | CopySignF64 // f64.copysign
    // conversions
    | WrapI64asI32 // i32.wrap/i64
    | TruncateF32asI32 // i32.trunc_s/f32
    | TruncateUnsignedF32asI32 // i32.trunc_u/f32
    | TruncateF64asI32 // i32.trunc_s/f64
    | TruncateUnsignedF64asI32 // i32.trunc_u/f64
    | ExtendI32toI64 // i64.extend_s/i32
    | ExtendUnsignedI32toI64 // i64.extend_u/i32
    | TruncateF32asI64 // i64.trunc_s/f32
    | TruncateUnsignedF32asI64 // i64.trunc_u/f32
    | TruncateF64asI64 // i64.trunc_s/f64
    | TruncateUnsignedF64asI64 // i64.trunc_u/f64
    | ConvertI32toF32 // f32.convert_s/i32
    | ConvertUnsignedI32toF32 // f32.convert_u/i32
    | ConvertI64toF32 // f32.convert_s/i64
    | ConvertUnsignedI64toF32 // f32.convert_u/i64
    | DemoteF64toF32 // f32.demote/f64
    | ConvertI32toF64 // f64.convert_s/i32
    | ConvertUnsignedI32toF64 // f64.convert_u/i32
    | ConvertI64toF64 // f64.convert_s/i64
    | ConvertUnsignedI64toF64 // f64.convert_u/i64
    | PromoteF32toF64 // f64.promote/f32
    // reinterpretations
    | ReinterpretF32asI32 // i32.reinterpret/f32
    | ReinterpretF64asI64 // i64.reinterpret/f64
    | ReinterpretI32asF32 // f32.reinterpret/i32
    | ReinterpretI64asF64 // f64.reinterpret/i64

type GlobalVariable = {
    Value: Value
    Mutable: bool
    Init: Instruction seq }

type Section =
    | Todo of byte * byte seq
    | Type of FuncType seq
    | Import of ImportEntry seq
    | Function of int seq // indices into Types (TODO: higher level?)
    | Table of ResizableLimits
    | Memory of ResizableLimits
    | Global of GlobalVariable seq
    // | Export
    // | Start
    // | Element
    // | Code
    // | Data
    | Custom of string * byte seq

type Module = Section seq

// --------------------------------------------------------------------------------

let rec leb128U (v: int64) = seq { // https://en.wikipedia.org/wiki/LEB128
    let b = v &&& 0x7fL |> byte
    let v' = v >>> 7
    if v' = 0L || v' = -1L then yield b else
        yield b ||| 0x80uy
        yield! leb128U v' }

let varuint1 (v: int) = if v = 0 || v = 1 then v |> int64 |> leb128U else failwith "Out of range (varuint1)" // TODO: still used?
let varuint7 (v: int) = if v >= 0 && v <= 127 then v |> int64 |> leb128U else failwith "Out of range (varuint7)"
let varuint32 (v: uint32) = v |> int64 |> leb128U

let rec leb128S (v: int64) = seq { // https://en.wikipedia.org/wiki/LEB128
    let b = v &&& 0x7fL |> byte
    let v' = v >>> 7
    let clear = b &&& 0x40uy = 0uy
    if (v' = 0L && clear) || (v' = -1L && not clear) then yield b else
        yield b ||| 0x80uy
        yield! leb128S v' }

let varint7 (v: int) = if v >= -128 && v <= 127 then v |> int64 |> leb128S else failwith "Out of range (varint7)"
let varint32 (v: int) = v |> int64 |> leb128S
let varint64 = leb128S

let moduleHeader =
    [0x00uy; 0x61uy; 0x73uy; 0x6duy // magic number (\0asm)
     0x01uy; 0x00uy; 0x00uy; 0x00uy] // version
    |> List.toSeq

let value = function
    | I32 -> 0x7fuy
    | I64 -> 0x7euy
    | F32 -> 0x7duy
    | F64 -> 0x7cuy

let optionalValue = function
    | Some v -> value v
    | None -> 0x40uy

let section (id: byte) (payload: byte seq) = seq {
    yield id
    yield! varuint32 (Seq.length payload |> uint32)
    yield! payload }

let stringUtf8 (str: string) = seq {
    let utf8 = Encoding.UTF8.GetBytes(str)
    yield! utf8.Length |> uint32 |> varuint32
    yield! utf8 }

let customSection name (payload: byte seq) = seq {
    let nameUtf8 = stringUtf8 name
    let payloadLen = uint32 (Seq.length payload + Seq.length nameUtf8)
    yield 0uy // custom section type id
    yield! varuint32 payloadLen // payload_len
    yield! nameUtf8 // name_len / name
    yield! payload } // payload_data

let typeSection types = seq {
    let funcType (func: FuncType) = seq { // func_type
        yield 0x60uy // func form
        yield! (Seq.length func.Parameters |> uint32 |> varuint32)
        yield! (Seq.map value func.Parameters)
        match func.Returns with
        | Some r -> yield! [1uy; value r] // future may support multiple: http://webassembly.org/docs/future-features/#multiple-return
        | None -> yield 0uy }
    let payload = seq {
        yield! Seq.length types |> uint32 |> varuint32 // count
        yield! Seq.map funcType types |> Seq.concat } // func_type*
    yield! section 1uy payload }

let resizable (limits: ResizableLimits) = seq {
    let init = limits |> fst |> uint32 |> varuint32
    match limits with
    | initial, Some maximum ->
        yield 1uy
        yield! init
        yield! maximum |> uint32 |> varuint32
    | initial, None ->
        yield 0uy
        yield! init }

let tableType limits = seq {
    yield 0x70uy // anyfunc (only elem_type currently supported)
    yield! resizable limits }

let globalType v m = seq {
    yield value v
    yield if m then 1uy else 0uy }

let importEntry (entry: ImportEntry) = seq {
    let name (n: ImportName) = seq {
        yield! stringUtf8 n.Module
        yield! stringUtf8 n.Field }
    match entry with
    | ImportEntry.Function (n, i) ->
        yield! name n
        yield 0uy // Function external_kind
        yield! i |> uint32 |> varuint32
    | ImportEntry.Table (n, r) ->
        yield! name n
        yield 1uy // Table external_kind
        yield! tableType r
    | ImportEntry.Memory (n, r) ->
        yield! name n
        yield 2uy // Memory external_kind
        yield! resizable r
    | ImportEntry.Global (n, v, m) ->
        yield! name n
        yield 3uy // Global external_kind
        yield! globalType v m }

let importSection imports = seq {
    let payload = seq {
        yield! Seq.length imports |> uint32 |> varuint32 // count
        yield! Seq.map importEntry imports |> Seq.concat } // import_entry*
    yield! section 2uy payload }

let functionSection indices = seq {
    let payload = seq {
        yield! Seq.length indices |> uint32 |> varuint32 // count
        yield! Seq.map (uint32 >> varuint32) indices |> Seq.concat } // types
    yield! section 3uy payload }

let tableSection limits = seq {
    let payload = seq {
        yield 1uy // currently no more than 1 table supported (omit section for 0)
        yield! tableType limits } // entries
    yield! section 4uy payload }

let memorySection limits = seq {
    let payload = seq {
        yield 1uy // currently no more than 1 table supported (omit section for 0)
        yield! resizable limits } // entries
    yield! section 5uy payload }

let rec instructions inst = seq { // TODO: test
    let bytes = function
        | Unreachable -> seq { yield 0x00uy }
        | Nop -> seq { yield 0x01uy }
        | Block v -> seq { yield 0x02uy; yield optionalValue v }
        | Loop v -> seq { yield 0x03uy; yield optionalValue v }
        | If v -> seq { yield 0x04uy; yield optionalValue v }
        | Else -> seq { yield 0x05uy }
        | End -> seq { yield 0x0buy }
        | Br depth -> seq { yield 0x0cuy; yield! depth |> uint32 |> varuint32 }
        | BrIf depth -> seq { yield 0x0duy; yield! depth |> uint32 |> varuint32 }
        | BrTable (count, targets, deflt) -> seq { yield 0x0euy; yield! count|> uint32 |> varuint32 ; yield! targets |> Seq.map (uint32 >> varuint32) |> Seq.concat; yield! deflt |> uint32 |> varuint32 }
        | Return -> seq { yield 0x0fuy }
        | Call i -> seq { yield 0x10uy; yield! i |> uint32 |> varuint32 }
        | CallIndirect i -> seq { yield 0x11uy; yield! i|> uint32 |> varuint32 ; yield 0uy }
        | Drop -> seq { yield 0x1auy; }
        | Select -> seq { yield 0x1buy; }
        | GetLocal i -> seq { yield 0x20uy; yield! i |> uint32 |> varuint32 }
        | SetLocal i -> seq { yield 0x21uy; yield! i |> uint32 |> varuint32 }
        | TeeLocal i -> seq { yield 0x22uy; yield! i |> uint32 |> varuint32 }
        | GetGlobal i -> seq { yield 0x23uy; yield! i |> uint32 |> varuint32 }
        | SetGlobal i -> seq { yield 0x24uy; yield! i |> uint32 |> varuint32 }
        | LoadI32 (flags, offset) -> seq { yield 0x28uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadI64 (flags, offset) -> seq { yield 0x29uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadF32 (flags, offset) -> seq { yield 0x2auy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadF64 (flags, offset) -> seq { yield 0x2buy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadByteI32 (flags, offset) -> seq { yield 0x2cuy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadByteUnsignedI32 (flags, offset) -> seq { yield 0x2duy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadShortI32 (flags, offset) -> seq { yield 0x2euy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadShortUnsignedI32 (flags, offset) -> seq { yield 0x2fuy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadByteI64 (flags, offset) -> seq { yield 0x30uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadByteUnsignedI64 (flags, offset) -> seq { yield 0x31uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadShortI64 (flags, offset) -> seq { yield 0x32uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadShortUnsignedI64 (flags, offset) -> seq { yield 0x33uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadIntI64 (flags, offset) -> seq { yield 0x34uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | LoadIntUnsignedI64 (flags, offset) -> seq { yield 0x35uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreI32 (flags, offset) -> seq { yield 0x36uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreI64 (flags, offset) -> seq { yield 0x37uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreF32 (flags, offset) -> seq { yield 0x38uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreF64 (flags, offset) -> seq { yield 0x39uy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreByteI32 (flags, offset) -> seq { yield 0x3auy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreShortI32 (flags, offset) -> seq { yield 0x3buy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreByteI64 (flags, offset) -> seq { yield 0x3cuy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreShortI64 (flags, offset) -> seq { yield 0x3duy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | StoreIntI64 (flags, offset) -> seq { yield 0x3euy; yield! flags|> uint32 |> varuint32 ; yield! offset |> uint32 |> varuint32 }
        | CurrentMemory -> seq { yield 0x3fuy; yield 0x00uy }
        | GrowMemory -> seq { yield 0x40uy; yield 0x00uy }
        | ConstI32 i -> seq { yield 0x41uy; yield! varint32 i }
        | ConstI64 i -> seq { yield 0x42uy; yield! varint64 i }
        | ConstF32 f -> seq { yield 0x43uy; yield! BitConverter.GetBytes(f) }
        | ConstF64 f -> seq { yield 0x44uy; yield! BitConverter.GetBytes(f) }
        | EqualZeroI32 -> seq { yield 0x45uy }
        | EqualI32 -> seq { yield 0x46uy }
        | NotEqualI32 -> seq { yield 0x47uy }
        | LessI32 -> seq { yield 0x48uy }
        | LessUnsignedI32 -> seq { yield 0x49uy }
        | GreaterI32 -> seq { yield 0x4auy }
        | GreaterUnsignedI32 -> seq { yield 0x4buy }
        | LessOrEqualI32 -> seq { yield 0x4cuy }
        | LessOrEqualUnsignedI32 -> seq { yield 0x4duy }
        | GreaterOrEqualI32 -> seq { yield 0x4euy }
        | GreaterOrEqualUnsignedI32 -> seq { yield 0x4uy }
        | EqualZeroI64 -> seq { yield 0x50uy }
        | EqualI64 -> seq { yield 0x51uy }
        | NotEqualI64 -> seq { yield 0x52uy }
        | LessI64 -> seq { yield 0x53uy }
        | LessUnsignedI64 -> seq { yield 0x54uy }
        | GreaterI64 -> seq { yield 0x55uy }
        | GreaterUnsignedI64 -> seq { yield 0x56uy }
        | LessOrEqualI64 -> seq { yield 0x57uy }
        | LessOrEqualUnsignedI64 -> seq { yield 0x58uy }
        | GreaterOrEqualI64 -> seq { yield 0x59uy }
        | GreaterOrEqualUnsignedI64 -> seq { yield 0x5auy }
        | EqualF32 -> seq { yield 0x5buy }
        | NotEqualF32 -> seq { yield 0x5cuy }
        | LessF32 -> seq { yield 0x5duy }
        | GreaterF32 -> seq { yield 0x5euy }
        | LessOrEqualF32 -> seq { yield 0x5fuy }
        | GreaterOrEqualF32 -> seq { yield 0x60uy }
        | EqualF64 -> seq { yield 0x61uy }
        | NotEqualF64 -> seq { yield 0x62uy }
        | LessF64 -> seq { yield 0x63uy }
        | GreaterF64 -> seq { yield 0x64uy }
        | LessOrEqualF64 -> seq { yield 0x65uy }
        | GreaterOrEqualF64 -> seq { yield 0x66uy }
        | ClzI32 -> seq { yield 0x67uy }
        | CtzI32 -> seq { yield 0x68uy }
        | PopCountI32 -> seq { yield 0x69uy }
        | AddI32 -> seq { yield 0x6auy }
        | SubI32 -> seq { yield 0x6buy }
        | MulI32 -> seq { yield 0x6cuy }
        | DivI32 -> seq { yield 0x6duy }
        | DivUnsignedI32 -> seq { yield 0x6euy }
        | RemI32 -> seq { yield 0x6fuy }
        | RemUnsignedI32 -> seq { yield 0x70uy }
        | AndI32 -> seq { yield 0x71uy }
        | OrI32 -> seq { yield 0x72uy }
        | XorI32 -> seq { yield 0x73uy }
        | ShiftLeftI32 -> seq { yield 0x74uy }
        | ShiftRightI32 -> seq { yield 0x75uy }
        | ShiftRightUnsignedI32 -> seq { yield 0x76uy }
        | RotateLeftI32 -> seq { yield 0x77uy }
        | RotateRightI32 -> seq { yield 0x78uy }
        | ClzI64 -> seq { yield 0x79uy }
        | CtzI64 -> seq { yield 0x7auy }
        | PopCountI64 -> seq { yield 0x7buy }
        | AddI64 -> seq { yield 0x7cuy }
        | SubI64 -> seq { yield 0x7duy }
        | MulI64 -> seq { yield 0x7euy }
        | DivI64 -> seq { yield 0x7fuy }
        | DivUnsignedI64 -> seq { yield 0x80uy }
        | RemI64 -> seq { yield 0x81uy }
        | RemUnsignedI64 -> seq { yield 0x82uy }
        | AndI64 -> seq { yield 0x83uy }
        | OrI64 -> seq { yield 0x84uy }
        | XorI64 -> seq { yield 0x85uy }
        | ShiftLeftI64 -> seq { yield 0x86uy }
        | ShiftRightI64 -> seq { yield 0x87uy }
        | ShiftRightUnsignedI64 -> seq { yield 0x88uy }
        | RotateLeftI64 -> seq { yield 0x89uy }
        | RotateRightI64 -> seq { yield 0x8auy }
        | AbsF32 -> seq { yield 0x8buy }
        | NegateF32 -> seq { yield 0x8cuy }
        | CeilingF32 -> seq { yield 0x8duy }
        | FloorF32 -> seq { yield 0x8euy }
        | TruncateF32 -> seq { yield 0x8fuy }
        | NearestF32 -> seq { yield 0x90uy }
        | SqrtF32 -> seq { yield 0x91uy }
        | AddF32 -> seq { yield 0x92uy }
        | SubF32 -> seq { yield 0x93uy }
        | MulF32 -> seq { yield 0x94uy }
        | DivF32 -> seq { yield 0x95uy }
        | MinF32 -> seq { yield 0x96uy }
        | MaxF32 -> seq { yield 0x97uy }
        | CopySignF32 -> seq { yield 0x98uy }
        | AbsF64 -> seq { yield 0x99uy }
        | NegateF64 -> seq { yield 0x9auy }
        | CeilingF64 -> seq { yield 0x9buy }
        | FloorF64 -> seq { yield 0x9cuy }
        | TruncateF64 -> seq { yield 0x9duy }
        | NearestF64 -> seq { yield 0x9euy }
        | SqrtF64 -> seq { yield 0x9fuy }
        | AddF64 -> seq { yield 0xa0uy }
        | SubF64 -> seq { yield 0xa1uy }
        | MulF64 -> seq { yield 0xa2uy }
        | DivF64 -> seq { yield 0xa3uy }
        | MinF64 -> seq { yield 0xa4uy }
        | MaxF64 -> seq { yield 0xa5uy }
        | CopySignF64 -> seq { yield 0xa6uy }
        | WrapI64asI32 -> seq { yield 0xa7uy }
        | TruncateF32asI32 -> seq { yield 0xa8uy }
        | TruncateUnsignedF32asI32 -> seq { yield 0xa9uy }
        | TruncateF64asI32 -> seq { yield 0xaauy }
        | TruncateUnsignedF64asI32 -> seq { yield 0xabuy }
        | ExtendI32toI64 -> seq { yield 0xacuy }
        | ExtendUnsignedI32toI64 -> seq { yield 0xaduy }
        | TruncateF32asI64 -> seq { yield 0xaeuy }
        | TruncateUnsignedF32asI64 -> seq { yield 0xafuy }
        | TruncateF64asI64 -> seq { yield 0xb0uy }
        | TruncateUnsignedF64asI64 -> seq { yield 0xb1uy }
        | ConvertI32toF32 -> seq { yield 0xb2uy }
        | ConvertUnsignedI32toF32 -> seq { yield 0xb3uy }
        | ConvertI64toF32 -> seq { yield 0xb4uy }
        | ConvertUnsignedI64toF32 -> seq { yield 0xb5uy }
        | DemoteF64toF32 -> seq { yield 0xb6uy }
        | ConvertI32toF64 -> seq { yield 0xb7uy }
        | ConvertUnsignedI32toF64 -> seq { yield 0xb8uy }
        | ConvertI64toF64 -> seq { yield 0xb9uy }
        | ConvertUnsignedI64toF64 -> seq { yield 0xbauy }
        | PromoteF32toF64 -> seq { yield 0xbbuy }
        | ReinterpretF32asI32 -> seq { yield 0xbcuy }
        | ReinterpretF64asI64 -> seq { yield 0xbduy }
        | ReinterpretI32asF32 -> seq { yield 0xbeuy }
        | ReinterpretI64asF64 -> seq { yield 0xbfuy }

    yield! Seq.map bytes inst |> Seq.concat }

let globalSection (globals: GlobalVariable seq) = seq {
    let payload = seq {
        let var { Value = v; Mutable = m; Init = i } = seq {
            yield! globalType v m // global_type
            yield! instructions i } // init_expr
        yield! Seq.length globals |> uint32 |> varuint32 // count
        yield! Seq.map var globals |> Seq.concat } // global_variable*
    yield! section 6uy payload }

let wasm sections = seq { // TODO: test
    let section = function
        | Todo (id, bytes) -> section id bytes
        | Type types -> typeSection types
        | Import entries -> importSection entries
        | Function indices -> functionSection indices
        | Table limits -> tableSection limits
        | Memory limits -> memorySection limits
        | Global globals -> globalSection globals
        | Custom (name, bytes) -> customSection name bytes
    yield! moduleHeader
    yield! sections |> Seq.map section |> Seq.concat }

// TODO: replace fixed bytes written as varint