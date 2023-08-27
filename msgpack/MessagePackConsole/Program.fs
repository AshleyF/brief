open System
open MessagePack

printfn "Testing.."

let test name values =
    //values |> pack |> Seq.iter (printf "%x "); printfn " %s" name
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

test "Sequence" [Nil; Int 123I; Bool true; Int 200I; Bool false; String "foo"; Int 123I; String "the quick brown fox jumps over the"; Int 42I]

//test "NaN" [Float Double.NaN] // fails test because on NaN semantics, but does serdes
//test "123456890123456890123456890123456890" [Int 123456890123456890123456890123456890I]
//test "-123456890123456890123456890123456890" [Int -123456890123456890123456890123456890I]

//[Nil] |> pack |> unpack |> List.ofSeq |> printfn "Nil: %A"
//[Bool true] |> pack |> unpack |> List.ofSeq |> printfn "True: %A"
//[Bool false] |> pack |> unpack |> List.ofSeq |> printfn "False: %A"
//
//[Nil; Int 123I; Bool true; Int 200I; Bool false] |> pack |> printfn "Bytes: %A"
//[Nil; Int 123I; Bool true; Int 200I; Bool false] |> pack |> unpack |> List.ofSeq |> printfn "Sequence: %A"

//type Type =
//| Nil
//| Integer of int
//| Boolean of bool
//| Float of float // IEEE 754 double precision floating point number including NaN and Infinity
//| Raw
//| String // extending Raw type represents a UTF-8 string
//| Binary // extending Raw type represents a byte array
//| Array // represents a sequence of objects
//| Map // represents key-value pairs of objects
//| Extension // represents a tuple of type information and a byte array where type information is an integer whose meaning is defined by applications or MessagePack specification
//| Timestamp // represents an instantaneous point on the time-line in the world that is independent from time zones or calendars. Maximum precision is nanoseconds.

// a value of an Integer object is limited from -(2^63) up to (2^64)-1
// maximum length of a Binary object is (2^32)-1
// maximum byte size of a String object is (2^32)-1
// String objects may contain invalid byte sequence and the behavior of a deserializer depends on the actual implementation when it received invalid byte sequence
// Deserializers should provide functionality to get the original byte array so that applications can decide how to handle the object
// maximum number of elements of an Array object is (2^32)-1
// maximum number of key-value associations of a Map object is (2^32)-1

// MessagePack allows applications to define application-specific types using the Extension type. Extension type consists of an integer and a byte array where the integer represents a kind of types and the byte array represents data.
// Applications can assign 0 to 127 to store application-specific type information. An example usage is that application defines type = 0 as the application's unique type system, and stores name of a type and values of the type at the payload.
// MessagePack reserves -1 to -128 for future extension to add predefined types. These types will be added to exchange more types without using pre-shared statically-typed schema across different programming environments.
// applications can decide whether they reject unknown Extension types, accept as opaque data, or transfer to another application without touching payload of them.
// e.g. Timestamp = -1

// *positive fixint  0xxxxxxx  0x00 - 0x7f
//  fixmap           1000xxxx  0x80 - 0x8f
//  fixarray         1001xxxx  0x90 - 0x9f
//  fixstr           101xxxxx  0xa0 - 0xbf
// *nil              11000000  0xc0
//  (never used)     11000001  0xc1
// *false            11000010  0xc2
// *true             11000011  0xc3
//  bin 8            11000100  0xc4
//  bin 16           11000101  0xc5
//  bin 32           11000110  0xc6
//  ext 8            11000111  0xc7
//  ext 16           11001000  0xc8
//  ext 32           11001001  0xc9
//  float 32         11001010  0xca
//  float 64         11001011  0xcb
// *uint 8           11001100  0xcc
// *uint 16          11001101  0xcd
// *uint 32          11001110  0xce
// *uint 64          11001111  0xcf
//  int 8            11010000  0xd0
//  int 16           11010001  0xd1
//  int 32           11010010  0xd2
//  int 64           11010011  0xd3
//  fixext 1         11010100  0xd4
//  fixext 2         11010101  0xd5
//  fixext 4         11010110  0xd6
//  fixext 8         11010111  0xd7
//  fixext 16        11011000  0xd8
//  str 8            11011001  0xd9
//  str 16           11011010  0xda
//  str 32           11011011  0xdb
//  array 16         11011100  0xdc
//  array 32         11011101  0xdd
//  map 16           11011110  0xde
//  map 32           11011111  0xdf
//  negative fixint  111xxxxx  0xe0 - 0xff

