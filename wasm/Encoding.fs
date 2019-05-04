module Encoding // http://webassembly.org/docs/binary-encoding/

open System
open System.Text
open Structure

let rec leb128 condition (v: int64) = seq { // https://en.wikipedia.org/wiki/LEB128
    let b = v &&& 0x7fL |> byte
    let v' = v >>> 7
    if condition b v' then yield b else
        yield b ||| 0x80uy
        yield! leb128 condition v' }

let varint64 = leb128 (fun b v -> let c = b &&& 0x40uy = 0uy in (v = 0L && c) || (v = -1L && not c))
let varint32 (v: int) = v |> int64 |> varint64
let varuint32 (v: uint32) = v |> int64 |> leb128 (fun _ v -> v = 0L || v = -1L)

let value = function I32 -> 0x7fuy | I64 -> 0x7euy | F32 -> 0x7duy | F64 -> 0x7cuy
let optionalValue = function Some v -> value v | None -> 0x40uy // block_type

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
    match limits with
    | initial, Some maximum ->
        yield 1uy
        yield! initial |> uint32 |> varuint32
        yield! maximum |> uint32 |> varuint32
    | initial, None ->
        yield 0uy
        yield! initial |> uint32 |> varuint32 }

let tableType limits = seq { // table_type
    yield 0x70uy // anyfunc (only elem_type currently supported)
    yield! resizable limits }

let globalType v m = seq {
    yield value v
    yield if m then 1uy else 0uy }

let externalKind = function
    | ExternalKind.Function -> 0uy
    | ExternalKind.Table    -> 1uy
    | ExternalKind.Memory   -> 2uy
    | ExternalKind.Global   -> 3uy

let importEntry (entry: ImportEntry) = seq {
    let name (n: ImportName) = seq {
        yield! stringUtf8 n.Module
        yield! stringUtf8 n.Field }
    match entry with
    | ImportEntry.Function (n, i) ->
        yield! name n
        yield externalKind ExternalKind.Function
        yield! i |> uint32 |> varuint32
    | ImportEntry.Table (n, r) ->
        yield! name n
        yield externalKind ExternalKind.Table
        yield! tableType r
    | ImportEntry.Memory (n, r) ->
        yield! name n
        yield externalKind ExternalKind.Memory
        yield! resizable r
    | ImportEntry.Global (n, v, m) ->
        yield! name n
        yield externalKind ExternalKind.Global
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
    let bytes = function // https://webassembly.github.io/spec/core/binary/instructions.html#memory-instructions
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
            yield! globalType v m // global_type (only immutable currently supported, but not checked here)
            yield! instructions i } // init_expr
        yield! Seq.length globals |> uint32 |> varuint32 // count
        yield! Seq.map var globals |> Seq.concat } // global_variable*
    yield! section 6uy payload }

let exportEntry (entry: ExportEntry) = seq {
    yield! stringUtf8 entry.Field
    yield externalKind entry.Kind
    yield! entry.Index |> uint32 |> varuint32 }

let exportSection exports = seq {
    let payload = seq {
        yield! Seq.length exports |> uint32 |> varuint32 // count
        yield! Seq.map exportEntry exports |> Seq.concat } // export_entry*
    yield! section 7uy payload }

let startSection index = seq {
    let payload = index |> uint32 |> varuint32
    yield! section 8uy payload }

let elementSegment (segment: ElementSegment) = seq {
    yield! segment.Index |> uint32 |> varuint32
    yield! segment.Offset |> instructions // init_expr
    yield! Seq.length segment.Elements |> uint32 |> varuint32 // num_elements
    yield! Seq.map (fun s -> s |> uint32 |> varuint32) segment.Elements |> Seq.concat }

let elementSection segments = seq {
    let payload = seq {
        yield! Seq.length segments |> uint32 |> varuint32 // count
        yield! Seq.map elementSegment segments |> Seq.concat } // elem_segment*
    yield! section 9uy payload }

let codeSection (functions: FunctionBody seq) = seq {
    let payload = seq {
        let body (b: FunctionBody) = seq {
            let bytes = seq {
                let local loc = seq {
                    yield! loc.Number |> uint32 |> varuint32 // number of given type
                    yield loc.Type |> value }
                yield! Seq.length b.Locals |> uint32 |> varuint32 // count
                yield! Seq.map local b.Locals |> Seq.concat // locals
                yield! instructions b.Code }
            yield! Seq.length bytes |> uint32 |> varuint32 // body size in bytes
            yield! bytes }
        yield! Seq.length functions |> uint32 |> varuint32 // count
        yield! Seq.map body functions |> Seq.concat }
    yield! section 10uy payload }

let dataSegment (segment: DataSegment) = seq {
    yield! segment.Index |> uint32 |> varuint32
    yield! segment.Offset |> instructions // init_expr
    yield! Seq.length segment.Data |> uint32 |> varuint32
    yield! segment.Data }

let dataSection segments = seq {
    let payload = seq {
        yield! Seq.length segments |> uint32 |> varuint32 // count
        yield! Seq.map dataSegment segments |> Seq.concat } // data_segment*
    yield! section 11uy payload }

let moduleHeader =
    [0x00uy; 0x61uy; 0x73uy; 0x6duy // magic number (\0asm)
     0x01uy; 0x00uy; 0x00uy; 0x00uy] // version
    |> List.toSeq

let wasm sections = seq { // TODO: test
    let section = function
        | Todo (id, bytes) -> section id bytes
        | Type types -> typeSection types
        | Import entries -> importSection entries
        | Function indices -> functionSection indices
        | Table limits -> tableSection limits
        | Memory limits -> memorySection limits
        | Global globals -> globalSection globals
        | Export exports -> exportSection exports
        | Start index -> startSection index
        | Element segments -> elementSection segments
        | Code code -> codeSection code
        | Custom (name, bytes) -> customSection name bytes
    yield! moduleHeader
    yield! sections |> Seq.map section |> Seq.concat }