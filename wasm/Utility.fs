module Utility

open System
open System.IO

let save (wasm: byte seq) =
    let base64 = wasm |> Array.ofSeq |> Convert.ToBase64String
    use writer = File.CreateText("./main.js")
    writer.WriteLine(sprintf "run('%s');" base64)