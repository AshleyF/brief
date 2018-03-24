module Wasm

open System
open System.Text

let rec leb128U (v: int64) = seq { // https://en.wikipedia.org/wiki/LEB128
    let b = v &&& 0x7fL |> byte
    let v' = v >>> 7
    if v' = 0L || v' = -1L then yield b else
        yield b ||| 0x80uy
        yield! leb128U v' }

let varuint1 (v: int) = if v = 0 || v = 1 then v |> int64 |> leb128U else failwith "Out of range (varuint1)"
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

type Section =
    | Type = 1uy // function signature declarations
    | Import = 2uy // import declarations
    | Function = 3uy // function declarations
    | Table = 4uy // indirect function table and other tables
    | Memory = 5uy // memory attributes
    | Global = 6uy // global declarations
    | Export = 7uy // exports
    | Start = 8uy // start function declaration
    | Element = 9uy // elements section
    | Code = 10uy // function bodies (code)
    | Data = 11uy // data segments

let section (id: Section) (payload: byte seq) = seq {
    yield byte id
    yield! varuint32 (Seq.length payload |> uint32)
    yield! payload }

let customSection (name: string) (payload: byte seq) = seq {
    let nameUtf8 = Encoding.UTF8.GetBytes(name)
    let nameLen = nameUtf8.Length |> uint32
    let nameLenVar = nameLen |> varuint32
    let payloadLen = uint32 (Seq.length payload + (nameLen |> varuint32 |> Seq.length)) + nameLen
    yield 0uy // custom section type id
    yield! varuint32 payloadLen // payload_len
    yield! varuint32 nameLen // name_len
    yield! nameUtf8 // name
    yield! payload } // payload_data

type Value = // value_type
    | i32 = 0x7fuy
    | i64 = 0x7euy
    | f32 = 0x7duy
    | f64 = 0x7cuy

type FuncType = { // func_type
    Parameters: Value seq
    Returns: Value option } // future multiple supported: http://webassembly.org/docs/future-features/#multiple-return

let TypeSection types = seq {
    let funcType (func: FuncType) = seq { // func_type
        yield 0x60uy // func form
        yield! (Seq.length func.Parameters |> uint32 |> varuint32)
        yield! (Seq.map byte func.Parameters)
        match func.Returns with
        | Some r -> yield! [1uy; byte r]
        | None -> yield 0uy }
    let payload = seq {
        yield Seq.length types |> byte // count
        yield! Seq.map funcType types |> Seq.concat } // func_type*
    yield! section Section.Type payload }

// --------------------------------------------------------------------------------

type Block = Value option // block_type (None -> 0x40)

type Element = // elem_type
    | anyfunc = 0x70