printfn "Hello WASM World!"

open System
open Utility
open Wasm
open Tests

testAll ()

printfn "Building module..."

let bytes =
    Seq.concat [
        moduleHeader
        TypeSection [{ Parameters = []; Returns = Some Value.i32 }]
        section Section.Function [1uy; 0uy]
        section Section.Memory [1uy; 0uy; 1uy]
        section Section.Export [2uy; 6uy; 109uy; 101uy; 109uy; 111uy; 114uy; 121uy; 2uy; 0uy; 4uy; 109uy; 97uy; 105uy; 110uy; 0uy; 0uy]
        section Section.Code [1uy; 132uy; 128uy; 128uy; 128uy; 0uy; 0uy; 65uy; 42uy; 11uy]
    ]

save bytes