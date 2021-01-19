module Serialization

open System.IO
open Structure

let rec serialize (writer: BinaryWriter) = function
    | Symbol s -> writer.Write(0uy); writer.Write(s)
    | String s -> writer.Write(1uy); writer.Write(s)
    | Number n -> writer.Write(2uy); writer.Write(n)
    | List l -> writer.Write(3uy); writer.Write(l.Length); List.iter (serialize writer) l
    | Map m ->
        writer.Write(4uy); writer.Write(m.Count)
        Map.iter (fun (k: string) v -> writer.Write(k); serialize writer v) m
    | Word w -> writer.Write(5uy); writer.Write(w.Name)

let rec deserialize primitives (reader: BinaryReader) =
    match reader.ReadByte() with
    | 0uy -> reader.ReadString() |> Symbol
    | 1uy -> reader.ReadString() |> String
    | 2uy -> reader.ReadDouble() |> Number
    | 3uy -> List.init (reader.ReadInt32()) (fun _ -> deserialize primitives reader) |> List
    | 4uy -> Seq.init (reader.ReadInt32()) (fun _ -> reader.ReadString(), deserialize primitives reader) |> Map.ofSeq |> Map
    | 5uy -> let n = reader.ReadString() in match Map.tryFind n primitives with Some (Word w) -> Word w | _ -> sprintf "Unknown primitive: %s" n |> failwith
    | _ -> failwith "Unknown type tag"
