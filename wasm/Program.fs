printfn "Hello WASM World!"

open System
open Utility
open Wasm
open Tests

testAll ()

printfn "Building module..."

let bytes =
    wasm [
        Type [{ Parameters = []; Returns = Some I32 }]
        Function [0]
        Todo (5uy, [1uy; 0uy; 1uy])
        Todo (7uy, [2uy; 6uy; 109uy; 101uy; 109uy; 111uy; 114uy; 121uy; 2uy; 0uy; 4uy; 109uy; 97uy; 105uy; 110uy; 0uy; 0uy])
        Todo (10uy, [1uy; 132uy; 128uy; 128uy; 128uy; 0uy; 0uy; 65uy; 42uy; 11uy])
    ]

save bytes