module Serialization

open System.IO
open Structure

let rec serialize (writer: BinaryWriter) = function
    | Word w -> writer.Write(0uy); writer.Write(w.Name)
    | Symbol w -> writer.Write(1uy); writer.Write(w)
    | String s -> writer.Write(2uy); writer.Write(s)
    | Number n -> writer.Write(3uy); writer.Write(n)
    | List l -> writer.Write(4uy); writer.Write(l.Length); List.iter (serialize writer) l
    | Map m ->
        writer.Write(5uy); writer.Write(m.Count)
        Map.iter (fun (k: string) v -> writer.Write(k); serialize writer v) m

let rec deserialize primitives (reader: BinaryReader) =
    match reader.ReadByte() with
    | 0uy -> let n = reader.ReadString() in match Map.tryFind n primitives with Some (Word w) -> Word w | _ -> sprintf "Unknown primitive: '%s'" n |> failwith
    | 1uy -> reader.ReadString() |> Symbol
    | 2uy -> reader.ReadString() |> String
    | 3uy -> reader.ReadDouble() |> Number
    | 4uy -> List.init (reader.ReadInt32()) (fun _ -> deserialize primitives reader) |> List
    | 5uy -> Seq.init (reader.ReadInt32()) (fun _ -> (reader.ReadString(), (deserialize primitives reader))) |> Map.ofSeq |> Map
    | _ -> failwith "Unknown type tag"
