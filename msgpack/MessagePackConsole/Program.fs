open System
open MessagePack

printfn "Testing.."

let test name values =
    //printfn ""; values |> pack |> Seq.iter (printf "%x "); printfn " %s" name
    let input = values |> pack |> unpack |> List.ofSeq
    let expected = List.ofSeq values
    if input <> expected then printfn "FAIL: %s %A <> %A" name input expected

test "Nil" [Nil]
test "True" [Bool true]
test "False" [Bool false]
test "-123456890123456890" [Int -12345689012345689I]
test "-4000000000" [Int -4000000000I]
test "-100000" [Int -100000I]
test "-65535" [Int -65535I]
test "-456" [Int -456I]
test "-123" [Int -123I]
test "-12" [Int -12I]
test "0" [Int 0I]
test "123" [Int 123I]
test "456" [Int 456I]
test "65535" [Int 65535I]
test "100000" [Int 100000I]
test "4000000000" [Int 4000000000I]
test "123456890123456890" [Int 12345689012345689I]
test "3.14159" [Float 3.14159]
test "0." [Float 0.]
test "+Infinity" [Float Double.PositiveInfinity]
test "-Infinity" [Float Double.NegativeInfinity]
test "-Zero" [Float Double.NegativeZero]
test "'foo'" [String "foo"]
test "'the quick brown fox ...'" [String "the quick brown fox jumps over the"]
test "Long string" [String "Now is the time for all good men to come to the aid of their country. The quick brown fox jumps over the lazy dog. Now is the time for all good men to come to the aid of their country. The quick brown fox jumps over the lazy dog. Now is the time for all good men..."]
test "[1,2,3]" [Bin [1uy; 2uy; 3uy]]
test "[1,2,3, ..., 254, 255, 256]" [Bin [1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy; 9uy; 10uy; 11uy; 12uy; 13uy; 14uy; 15uy; 16uy; 17uy; 18uy; 19uy; 20uy; 21uy; 22uy; 23uy; 24uy; 25uy; 26uy; 27uy; 28uy; 29uy; 30uy; 31uy; 32uy; 33uy; 34uy; 35uy; 36uy; 37uy; 38uy; 39uy; 40uy; 41uy; 42uy; 43uy; 44uy; 45uy; 46uy; 47uy; 48uy; 49uy; 50uy; 51uy; 52uy; 53uy; 54uy; 55uy; 56uy; 57uy; 58uy; 59uy; 60uy; 61uy; 62uy; 63uy; 64uy; 65uy; 66uy; 67uy; 68uy; 69uy; 70uy; 71uy; 72uy; 73uy; 74uy; 75uy; 76uy; 77uy; 78uy; 79uy; 80uy; 81uy; 82uy; 83uy; 84uy; 85uy; 86uy; 87uy; 88uy; 89uy; 90uy; 91uy; 92uy; 93uy; 94uy; 95uy; 96uy; 97uy; 98uy; 99uy; 100uy; 101uy; 102uy; 103uy; 104uy; 105uy; 106uy; 107uy; 108uy; 109uy; 110uy; 111uy; 112uy; 113uy; 114uy; 115uy; 116uy; 117uy; 118uy; 119uy; 120uy; 121uy; 122uy; 123uy; 124uy; 125uy; 126uy; 127uy; 128uy; 129uy; 130uy; 131uy; 132uy; 133uy; 134uy; 135uy; 136uy; 137uy; 138uy; 139uy; 140uy; 141uy; 142uy; 143uy; 144uy; 145uy; 146uy; 147uy; 148uy; 149uy; 150uy; 151uy; 152uy; 153uy; 154uy; 155uy; 156uy; 157uy; 158uy; 159uy; 160uy; 161uy; 162uy; 163uy; 164uy; 165uy; 166uy; 167uy; 168uy; 169uy; 170uy; 171uy; 172uy; 173uy; 174uy; 175uy; 176uy; 177uy; 178uy; 179uy; 180uy; 181uy; 182uy; 183uy; 184uy; 185uy; 186uy; 187uy; 188uy; 189uy; 190uy; 191uy; 192uy; 193uy; 194uy; 195uy; 196uy; 197uy; 198uy; 199uy; 200uy; 201uy; 202uy; 203uy; 204uy; 205uy; 206uy; 207uy; 208uy; 209uy; 210uy; 211uy; 212uy; 213uy; 214uy; 215uy; 216uy; 217uy; 218uy; 219uy; 220uy; 221uy; 222uy; 223uy; 224uy; 225uy; 226uy; 227uy; 228uy; 229uy; 230uy; 231uy; 232uy; 233uy; 234uy; 235uy; 236uy; 237uy; 238uy; 239uy; 240uy; 241uy; 242uy; 243uy; 244uy; 245uy; 246uy; 247uy; 248uy; 249uy; 250uy; 251uy; 252uy; 253uy; 254uy; 255uy; 255uy]]
test "[|Nil 123 true|]" [Array [Nil; Int 123I; Bool true]]
test "[|Nil 123 true ... 456|]" [Array [Nil; Int 123I; Bool true; Nil; Int 123I; Bool true; Nil; Int 123I; Bool true; Nil; Int 123I; Bool true; Nil; Int 123I; Bool true; Int 456I]]
test "{ 123: true, 'foo': false }" [Map (Map.ofList [Int 123I, Bool true; String "foo", Bool false])]
test "{ 0: Nil, 1: Nil, ... 16: Nil }" [Map (Map.ofList [Int 0I, Nil; Int 1I, Nil; Int 2I, Nil; Int 3I, Nil; Int 4I, Nil; Int 5I, Nil; Int 6I, Nil; Int 7I, Nil; Int 8I, Nil; Int 9I, Nil; Int 10I, Nil; Int 11I, Nil; Int 12I, Nil; Int 13I, Nil; Int 14I, Nil; Int 15I, Nil])]
test "Ext (123, [42])" [Extension (123y, [42uy])]
test "Ext (123, [7,42])" [Extension (123y, [7uy; 42uy])]
test "Ext (123, [7,42,0])" [Extension (123y, [7uy; 42uy; 0uy])]
test "Ext (123, [1,2,3,4])" [Extension (123y, [1uy; 2uy; 3uy; 4uy])]
test "Ext (123, [1,2,3,...,8])" [Extension (123y, [1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy])]
test "Ext (123, [1,2,3,...,16])" [Extension (123y, [1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy; 9uy; 10uy; 11uy; 12uy; 13uy; 14uy; 15uy; 16uy])]
test "Ext (123, [1,2,3, ...,256]" [Extension (123y, [1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy; 9uy; 10uy; 11uy; 12uy; 13uy; 14uy; 15uy; 16uy; 17uy; 18uy; 19uy; 20uy; 21uy; 22uy; 23uy; 24uy; 25uy; 26uy; 27uy; 28uy; 29uy; 30uy; 31uy; 32uy; 33uy; 34uy; 35uy; 36uy; 37uy; 38uy; 39uy; 40uy; 41uy; 42uy; 43uy; 44uy; 45uy; 46uy; 47uy; 48uy; 49uy; 50uy; 51uy; 52uy; 53uy; 54uy; 55uy; 56uy; 57uy; 58uy; 59uy; 60uy; 61uy; 62uy; 63uy; 64uy; 65uy; 66uy; 67uy; 68uy; 69uy; 70uy; 71uy; 72uy; 73uy; 74uy; 75uy; 76uy; 77uy; 78uy; 79uy; 80uy; 81uy; 82uy; 83uy; 84uy; 85uy; 86uy; 87uy; 88uy; 89uy; 90uy; 91uy; 92uy; 93uy; 94uy; 95uy; 96uy; 97uy; 98uy; 99uy; 100uy; 101uy; 102uy; 103uy; 104uy; 105uy; 106uy; 107uy; 108uy; 109uy; 110uy; 111uy; 112uy; 113uy; 114uy; 115uy; 116uy; 117uy; 118uy; 119uy; 120uy; 121uy; 122uy; 123uy; 124uy; 125uy; 126uy; 127uy; 128uy; 129uy; 130uy; 131uy; 132uy; 133uy; 134uy; 135uy; 136uy; 137uy; 138uy; 139uy; 140uy; 141uy; 142uy; 143uy; 144uy; 145uy; 146uy; 147uy; 148uy; 149uy; 150uy; 151uy; 152uy; 153uy; 154uy; 155uy; 156uy; 157uy; 158uy; 159uy; 160uy; 161uy; 162uy; 163uy; 164uy; 165uy; 166uy; 167uy; 168uy; 169uy; 170uy; 171uy; 172uy; 173uy; 174uy; 175uy; 176uy; 177uy; 178uy; 179uy; 180uy; 181uy; 182uy; 183uy; 184uy; 185uy; 186uy; 187uy; 188uy; 189uy; 190uy; 191uy; 192uy; 193uy; 194uy; 195uy; 196uy; 197uy; 198uy; 199uy; 200uy; 201uy; 202uy; 203uy; 204uy; 205uy; 206uy; 207uy; 208uy; 209uy; 210uy; 211uy; 212uy; 213uy; 214uy; 215uy; 216uy; 217uy; 218uy; 219uy; 220uy; 221uy; 222uy; 223uy; 224uy; 225uy; 226uy; 227uy; 228uy; 229uy; 230uy; 231uy; 232uy; 233uy; 234uy; 235uy; 236uy; 237uy; 238uy; 239uy; 240uy; 241uy; 242uy; 243uy; 244uy; 245uy; 246uy; 247uy; 248uy; 249uy; 250uy; 251uy; 252uy; 253uy; 254uy; 255uy; 255uy])]
test "Timestamp 03NOV1971" [Timestamp ((new DateTime(1971, 11, 3)).AddTicks(123))]

test "Sequence" [Nil; Int 123I; Bool true; Int 200I; Array [Nil; Int 123I; Bool true]; Bool false; Extension (123y, [7uy; 42uy]); String "foo"; Int 123I; String "the quick brown fox jumps over the"; Int 42I]

//test "NaN" [Float Double.NaN] // fails test because on NaN semantics, but does serdes
//test "123456890123456890123456890123456890" [Int 123456890123456890123456890123456890I]
//test "-123456890123456890123456890123456890" [Int -123456890123456890123456890123456890I]
