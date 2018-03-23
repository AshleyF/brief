module Utility

open System.IO

let save (wasm: byte seq) =
    let array = wasm |> Seq.rev |> Seq.map (sprintf "%i") |> String.concat ","
    use writer = File.CreateText("./main.js")
    writer.WriteLine(sprintf "run([%s])" array)