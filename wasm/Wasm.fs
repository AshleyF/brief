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

type Section =
    | Todo of byte * byte seq
    | Type of FuncType seq
    | Import of ImportEntry seq
    | Function of int seq // indices into Types (TODO: higher level?)
    | Table of ResizableLimits
    | Memory of ResizableLimits
    // | Global
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
        yield value v // content_type
        yield if m then 1uy else 0uy } // mutability

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

let wasm sections = seq { // TODO: test
    let section = function
        | Todo (id, bytes) -> section id bytes
        | Type types -> typeSection types
        | Import entries -> importSection entries
        | Function indices -> functionSection indices
        | Table limits -> tableSection limits
        | Memory limits -> memorySection limits
        | Custom (name, bytes) -> customSection name bytes
    yield! moduleHeader
    yield! sections |> Seq.map section |> Seq.concat }

// TODO: replace fixed bytes written as varint