printfn "Hello WASM World!"

open Utility
open Structure
open Encoding
open Brief
open Tests

testAll ()

printfn "Building module..."

let bytes =
    wasm [
        Type [{ Parameters = []; Returns = Some Value.I32 }]
        Function [0]
        Export [{ Field = "main"; Kind = ExternalKind.Function; Index = 0 }]
        Code [{ Locals = []; Code = brief "3 4 + 5 *" |> print }]
    ]

save bytes