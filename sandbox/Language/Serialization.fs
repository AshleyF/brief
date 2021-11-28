module Serialization

open System.IO
open Structure

#if DEBUG
let rec serialize (writer: BinaryWriter) =
    let rec write7bit (i: int) =
        if i >= 0x80 then
            writer.Write(byte (i ||| 0x80))
            write7bit (i >>> 7)
        else writer.Write(byte i)
    function
    | Word   w -> writer.Write(0uy); writer.Write(w.Name)
    | Symbol w -> writer.Write(1uy); writer.Write(w)
    | String s -> writer.Write(2uy); writer.Write(s)
    | Number n -> writer.Write(3uy); writer.Write(n)
    | List   l -> writer.Write(4uy); write7bit(l.Length); List.iter (serialize writer) l
    | Map    m ->
        writer.Write(5uy); write7bit(m.Count)
        Map.iter (fun (k: string) v -> writer.Write(k); serialize writer v) m
#endif

let rec deserialize primitives (reader: BinaryReader) =
    let read7bit () =
        let rec read shift i =
            let b = reader.ReadByte()
            let i' = i ||| ((int b &&& 0x7f) <<< shift)
            if b &&& 0x80uy = 0uy then i' else read (shift + 7) i'
        read 0 0
    match reader.ReadByte() with
    | 0uy -> let n = reader.ReadString() in match Map.tryFind n primitives with Some (Word w) -> Word w | _ -> sprintf "Unknown primitive: '%s'" n |> failwith
    | 1uy -> reader.ReadString() |> Symbol
    | 2uy -> reader.ReadString() |> String
    | 3uy -> reader.ReadDouble() |> Number
    | 4uy -> List.init (read7bit ()) (fun _ -> deserialize primitives reader) |> List
    | 5uy -> Seq.init (read7bit ()) (fun _ -> (reader.ReadString(), (deserialize primitives reader))) |> Map.ofSeq |> Map
    | _ -> failwith "Unknown type tag"
