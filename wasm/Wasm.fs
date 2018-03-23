module Wasm

open System

let rec leb128 (v: int64) = seq { // https://en.wikipedia.org/wiki/LEB128
    let b = v &&& 0x7fL |> byte
    let v' = v >>> 7
    if v' = 0L || v' = -1L then yield b else
        yield b ||| 0x80uy
        yield! leb128 v' }

let varuint1 (v: int) = if v = 0 || v = 1 then v |> int64 |> leb128 else failwith "Out of range (varuint1)"
let varuint7 (v: int) = if v >= 0 && v <= 127 then v |> int64 |> leb128 else failwith "Out of range (varuint7)"
let varuint32 (v: uint32) = v |> int64 |> leb128

let varint7 (v: int) = if v >= 0 && v <= 127 then v |> int64 |> leb128 else failwith "Out of range (varint7)"
let varint32 (v: int) = v |> int64 |> leb128
let varint64 = leb128

type Types =
    | i32 = 0x7fuy
    | i64 = 0x7euy
    | f32 = 0x7duy
    | f64 = 0x7cuy
    | anyfunc = 0x70uy
    | func = 0x60uy
    | block_type = 0x40uy

type value_type =
    | i32 = 0x7fuy
    | i64 = 0x7euy
    | f32 = 0x7duy
    | f64 = 0x7cuy

type block_type = value_type option // None -> 0x40o

type elem_type =
    | anyfunc = 0x70

let moduleHeader =
    [0x00uy; 0x61uy; 0x73uy; 0x6duy // magic number (\0asm)
     0x00uy; 0x00uy; 0x00uy; 0x01uy] // version
    |> List.toSeq