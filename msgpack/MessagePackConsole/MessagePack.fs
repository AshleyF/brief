module MessagePack // Very simple MessagePack implementation https://msgpack.org/

open System
open System.Text

type Value =
    | Nil
    | Bool of bool
    | Int of bigint
    | Float of float
    | String of string
    | Bin of byte list
    | Array of Value list
    | Map of Map<Value, Value>
    | Extension of sbyte * byte list
    | Timestamp of DateTime

let rec pack values = seq {
    let recurse value = seq { yield! value; yield! Seq.tail values |> pack }
    let byteOrder = if BitConverter.IsLittleEndian then Seq.rev >> List.ofSeq else List.ofSeq
    let packInt i =
        if i < bigint Int64.MinValue then failwith "Negative integer out of range"
        elif i < bigint Int32.MinValue then 0xd3uy :: (BitConverter.GetBytes(int64 i) |> byteOrder) // int64
        elif i < bigint Int16.MinValue then 0xd2uy :: (BitConverter.GetBytes(int32 i) |> byteOrder) // int32
        elif i < bigint SByte.MinValue then 0xd1uy :: (BitConverter.GetBytes(int16 i) |> byteOrder) // int16
        elif i < -32I then [0xd0uy; i |> sbyte |> byte] // int8
        elif i <= 127I then [i |> sbyte |> byte] // fixint
        elif i <= bigint Byte.MaxValue then [0xccuy; byte i] // uint8
        elif i <= bigint UInt16.MaxValue then 0xcduy :: (BitConverter.GetBytes(uint16 i) |> byteOrder) // uint16
        elif i <= bigint UInt32.MaxValue then 0xceuy :: (BitConverter.GetBytes(uint32 i) |> byteOrder) // uint32
        elif i <= bigint UInt64.MaxValue then 0xcfuy :: (BitConverter.GetBytes(uint64 i) |> byteOrder) // uint64
        else failwith "Positive integer out of range"
    let packFloat f =
        if f.Equals(f |> single |> float)
        then 0xcauy :: (BitConverter.GetBytes(single f) |> byteOrder) // float32
        else 0xcbuy :: (BitConverter.GetBytes(float f) |> byteOrder) // float64
    let packBytes code8 code16 code32 (bytes : byte list) =
        let len = bytes.Length
        if len <= int Byte.MaxValue then code8 :: (byte len) :: bytes // <= 8-bit len
        elif len <= int UInt16.MaxValue then code16 :: (BitConverter.GetBytes(uint16 len) |> byteOrder) @ bytes // <= 16-bit len
        else code32 :: (BitConverter.GetBytes(uint32 len) |> byteOrder) @ bytes // <= 32-bit len
    let packString (s : string) =
        let bytes = Encoding.UTF8.GetBytes(s) |> List.ofSeq
        let len = bytes.Length
        if len < 32 then (0b10100000uy ||| (byte len)) :: bytes // string (< 32)
        else packBytes 0xd9uy 0xdauy 0xdbuy bytes
    let packBin bytes = packBytes 0xc4uy 0xc5uy 0xc6uy bytes
    let packCollection code8 code16 code32 len packed =
        if len < 16 then (code8 ||| (byte len)) :: packed
        elif len <= int UInt16.MaxValue then code16 :: (BitConverter.GetBytes(uint16 len) |> byteOrder) @ packed
        else code32 :: (BitConverter.GetBytes(uint32 len) |> byteOrder) @ packed
    let packArray (values : Value list) = pack values |> List.ofSeq |> packCollection 0b10010000uy 0xdcuy 0xdduy values.Length
    let packMap map = map |> Map.toSeq |> Seq.map (fun (k, v) -> [k; v]) |> Seq.concat |> pack |> List.ofSeq |> packCollection 0b10000000uy 0xdeuy 0xdfuy (Map.count map)
    let packExt typ bytes =
        let data = (byte typ) :: bytes
        match bytes.Length with
        | 1 -> 0xd4uy :: data | 2 -> 0xd5uy :: data | 4 -> 0xd6uy :: data | 8 -> 0xd7uy :: data | 16 -> 0xd8uy :: data
        | len when len <= int Byte.MaxValue -> 0xc7uy :: (byte len) :: data // <= 8-bit len
        | len when len <= int UInt16.MaxValue -> 0xc8uy :: (BitConverter.GetBytes(uint16 len) |> byteOrder) @ data // <= 16-bit len
        | len -> 0xc9uy :: (BitConverter.GetBytes(uint32 len) |> byteOrder) @ data // <= 32-bit len
    let packTimestamp dt =
        let span = dt - (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc))
        let unixTime = span.TotalSeconds;
        let sec = Math.Floor(unixTime) |> int64
        let nano = Math.Round((unixTime - float sec) * 1000000000.) |> uint32
        0xc7uy :: 12uy :: 255uy :: (BitConverter.GetBytes(nano) |> byteOrder) @ (BitConverter.GetBytes(sec) |> byteOrder)
    if not (Seq.isEmpty values) then
        match Seq.head values with
        | Nil -> yield! [0xc0uy] |> recurse
        | Bool b -> yield! [if b then 0xc3uy else 0xc2uy] |> recurse
        | Int i -> yield! packInt i |> recurse
        | Float f -> yield! packFloat f |> recurse
        | String s -> yield! packString s |> recurse
        | Bin b -> yield! b |> List.ofSeq |> packBin |> recurse
        | Array v -> yield! v |> packArray |> recurse
        | Map m -> yield! m |> packMap |> recurse
        | Extension (t, b) -> yield! packExt t b |> recurse
        | Timestamp dt -> yield! packTimestamp dt |> recurse }

let rec unpack bytes = seq {
    let recurse skip value = seq { yield value; yield! bytes |> Seq.skip skip |> unpack }
    let getRawBytes n = bytes |> Seq.skip 1 |> Seq.take n
    let getBytes n = getRawBytes n |> (if BitConverter.IsLittleEndian then Seq.rev else id) |> Array.ofSeq
    let getString s n = Encoding.UTF8.GetString(getRawBytes (n + s) |> Seq.skip s |> Array.ofSeq) |> String |> recurse (n + s + 1)
    let getBin s n = getRawBytes (n + s) |> Seq.skip s |> List.ofSeq |> Bin |> recurse (n + s + 1)
    let getArray s n = seq {
        let unpacked = bytes |> Seq.skip s |> unpack
        yield unpacked |> Seq.take n |> List.ofSeq |> Array
        yield! unpacked |> Seq.skip n }
    let getMap s n = seq {
        let unpacked = bytes |> Seq.skip s |> unpack
        let toPairs = Seq.chunkBySize 2 >> Seq.map (function [|a; b|] -> a, b | _ -> failwith "Malformed map values")
        yield unpacked |> Seq.take (n * 2) |> toPairs |> Map.ofSeq |> Map
        yield! unpacked |> Seq.skip (n * 2) }
    let getExt s n = seq { // 1 3 - c7 3 7 2a 0
        match getRawBytes (n + s + 1) |> Seq.skip s |> Seq.take (n + 1) |> List.ofSeq with
        | 255uy :: n0 :: n1 :: n2 :: n3 :: s0 :: s1 :: s2 :: s3 :: s4 :: s5 :: s6 :: s7 :: _ ->
            let byteOrder = if BitConverter.IsLittleEndian then Array.rev else id
            let nano = BitConverter.ToUInt32(byteOrder [|n0; n1; n2; n3|], 0)
            let sec = BitConverter.ToInt64(byteOrder [|s0; s1; s2; s3; s4; s5; s6; s7|], 0)
            let ticks = sec * 10000000L + int64 nano / 100L
            let date = (new DateTime(ticks)).AddYears(1969)
            yield date |> Timestamp
        | typ :: data -> yield! Extension ((sbyte typ), data) |> recurse (s + n + 2)
        | _ -> failwith "Malformed extension data" }
    if not (Seq.isEmpty bytes) then
        match Seq.head bytes with
        | 0xc0uy -> yield! Nil |> recurse 1
        | 0xc2uy -> yield! Bool false |> recurse 1
        | 0xc3uy -> yield! Bool true |> recurse 1
        | 0xccuy -> yield! Int (bytes |> Seq.item 1 |> bigint) |> recurse 2 // uint8
        | 0xcduy -> yield! BitConverter.ToUInt16(getBytes 2, 0) |> bigint |> Int |> recurse 3 // uint16
        | 0xceuy -> yield! BitConverter.ToUInt32(getBytes 4, 0) |> bigint |> Int |> recurse 5 // uint32
        | 0xcfuy -> yield! BitConverter.ToUInt64(getBytes 8, 0) |> bigint |> Int |> recurse 9 // uint64
        | 0xd0uy -> yield! Int (bytes |> Seq.item 1 |> sbyte |> bigint) |> recurse 2 // int8
        | 0xd1uy -> yield! BitConverter.ToInt16(getBytes 2, 0) |> bigint |> Int |> recurse 3 // int16
        | 0xd2uy -> yield! BitConverter.ToInt32(getBytes 4, 0) |> bigint |> Int |> recurse 5 // int32
        | 0xd3uy -> yield! BitConverter.ToInt64(getBytes 8, 0) |> bigint |> Int |> recurse 9 // int64
        | b when b &&& 0b10000000uy = 0uy -> yield! bigint b |> Int |> recurse 1
        | b when b &&& 0b11100000uy = 0b11100000uy -> yield! b |> sbyte |> bigint |> Int |> recurse 1
        | 0xcauy -> yield! BitConverter.ToSingle(getBytes 4, 0) |> float |> Float |> recurse 5 // float32
        | 0xcbuy -> yield! BitConverter.ToDouble(getBytes 8, 0) |> Float |> recurse 9 // float64
        | b when b &&& 0b11100000uy = 0b10100000uy -> yield! b &&& 0b00011111uy |> int |> getString 0 // string (< 32)
        | 0xd9uy -> yield! bytes |> Seq.skip 1 |> Seq.head |> int |> getString 1 // string (<= 8-bit len)
        | 0xdauy -> yield! BitConverter.ToUInt16(getBytes 2, 0) |> int |> getString 2 // string (<= 16-bit len)
        | 0xdbuy -> yield! BitConverter.ToUInt32(getBytes 4, 0) |> int |> getString 4 // string (<= 32-bit len)
        | 0xc4uy -> yield! bytes |> Seq.skip 1 |> Seq.head |> int |> getBin 1 // bin (<= 8-bit len)
        | 0xc5uy -> yield! BitConverter.ToUInt16(getBytes 2, 0) |> int |> getBin 2 // bin (<= 16-bit len)
        | 0xc6uy -> yield! BitConverter.ToUInt32(getBytes 4, 0) |> int |> getBin 4 // bin (<= 32-bit len)
        | b when b &&& 0b11110000uy = 0b10010000uy -> yield! 0b00001111uy &&& b |> int |> getArray 1
        | 0xdcuy -> yield! BitConverter.ToUInt16(getBytes 2, 0) |> int |> getArray 3
        | 0xdduy -> yield! BitConverter.ToUInt32(getBytes 4, 0) |> int |> getArray 5
        | b when b &&& 0b11110000uy = 0b10000000uy -> yield! 0b00001111uy &&& b |> int |> getMap 1
        | 0xdeuy -> yield! BitConverter.ToUInt16(getBytes 2, 0) |> int |> getMap 3
        | 0xdfuy -> yield! BitConverter.ToUInt32(getBytes 4, 0) |> int |> getMap 5
        | 0xd4uy -> yield! getExt 0 1
        | 0xd5uy -> yield! getExt 0 2
        | 0xd6uy -> yield! getExt 0 4
        | 0xd7uy -> yield! getExt 0 8
        | 0xd8uy -> yield! getExt 0 16
        | 0xc7uy -> yield! bytes |> Seq.skip 1 |> Seq.head |> int |> getExt 1
        | 0xc8uy -> yield! BitConverter.ToUInt16(getBytes 2, 0) |> int |> getExt 2
        | 0xc9uy -> yield! BitConverter.ToUInt32(getBytes 4, 0) |> int |> getExt 4
        | _ -> failwith "Invalid format" }
