module Tests

open Wasm

let test name result expected =
    printfn "%s -> %A" name result
    if result <> expected then failwithf "Test failure (expected %A)" expected

let testfail name fn =
    printfn "%s -> failure (as expected)" name
    let mutable failed = false
    try fn () with _ -> failed <- true
    if not failed then failwith "Test failure (expected failure)"

let testVarInt () =
    test "varuint1 0" (varuint1 0 |> Seq.toList) [0uy]
    test "varuint1 1" (varuint1 1 |> Seq.toList) [1uy]
    testfail "varuint1 -1" (fun () -> varuint1 -1 |> ignore)
    testfail "varuint1 2" (fun () -> varuint1 2 |> ignore)

    test "varuint7 127" (varuint7 127 |> Seq.toList) [127uy]
    testfail "varuint7 -1" (fun () -> varuint7 -1 |> ignore)
    testfail "varuint7 128" (fun () -> varuint7 128 |> ignore)

    test "varuint32 123" (varuint32 123u |> Seq.toList) [123uy]
    test "varuint32 456" (varuint32 456u |> Seq.toList) [200uy; 3uy]
    test "varuint32 12345" (varuint32 12345u |> Seq.toList) [185uy; 96uy]
    test "varuint32 624485" (varuint32 624485u |> Seq.toList) [229uy; 142uy; 38uy]
    test "varuint32 3000000000" (varuint32 3000000000u |> Seq.toList) [128uy; 188uy; 193uy; 150uy; 11uy]

    // test "varint7 127" (varint7 127 []) [127uy]

    test "varint32 -624485" (varint32 -624485 |> Seq.toList) [155uy; 241uy; 89uy]
    // testfail "varuint7 -1" (fun () -> varuint7 -1 [] |> ignore)
    // testfail "varuint7 128" (fun () -> varuint7 128 [] |> ignore)

let testAll () =
    printfn "Running tests..."
    testVarInt ()