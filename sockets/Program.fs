open System
open System.Net.Sockets
open System.Net
open System.IO
open System.Text
open System.Security.Cryptography

let listener = TcpListener(IPAddress.Any, 11311)
listener.Start()

let acceptTcpClient () =
    let client = listener.AcceptTcpClient()
    let stream = client.GetStream()
    let reader = new StreamReader(stream)
    let request = reader.ReadLine().Split(' ')
    printfn "HTTP Request: %A" request
    let rec readHeaders headers =
        let h = reader.ReadLine()
        if h.Length > 0 then h :: headers |> readHeaders else headers
    let headers = readHeaders []
    printfn "HTTP Headers: %A" headers
    let writer = new StreamWriter(stream)
    request, headers, reader, writer

let httpResponse (writer: StreamWriter) =
    writer.Write("HTTP/1.1 200 OK\r\n")
    writer.Write("Content-Type: text/html\r\n")
    writer.Write("\r\n")
    writer.Write("""<script>
    alert('v4')
    var ws = new WebSocket("ws://localhost:11311/socket")
    ws.onopen = function() { alert('Open'); }
    ws.onclose = function() { alert('Close'); }
    ws.onerror = function(error) { alert('Error: ' + error); }
    ws.onmessage = function(message) {
        var reply = prompt('Message: ' + message.data);
        ws.send(reply);
    }
    </script>""")
    writer.Flush()

let http404 (writer: StreamWriter) =
    writer.Write("HTTP/1.1 404 OK\r\n")
    writer.Write("\r\n")
    writer.Flush()

let sendFrame (payload: byte[]) start len op final (writer: StreamWriter) =
    writer.BaseStream.WriteByte((byte)(op ||| (if final then 0x80 else 0)))
    if len < 126 then
        writer.BaseStream.WriteByte(byte len)
    elif uint16 len < UInt16.MaxValue then
        writer.BaseStream.WriteByte(byte 126) // indicate 16-bit
        writer.BaseStream.WriteByte(byte len >>> 8)
        writer.BaseStream.WriteByte(byte len)
    else
        writer.BaseStream.WriteByte(byte 127) // indicate 64-bit
        (* TODO
        for (var i = 56; i >= 0; i -= 8)
                {
                    stream.WriteByte((byte)(len >> i));
                }
            }
            *)
 
    // TODO: support masking? (currently, mask bit always 0 - otherwise, send 4 bytes here)
    writer.BaseStream.Write(payload, start, len)

let sendBytes op (payload: byte[]) (writer: StreamWriter)=
    let packetSize = 1024;
    for i in 0 .. packetSize .. payload.Length - 1 do
        let remaining = payload.Length - i
        let last = remaining <= packetSize
        let len = if last then remaining else packetSize
        sendFrame payload i len (if i = 0 then op else 0) last writer

let sendString (payload: string) = Encoding.UTF8.GetBytes(payload) |> sendBytes 1

let close writer = sendFrame (Array.zeroCreate 0) 0 0 8 true writer

let webSocketResponse (headers: string list) (reader: StreamReader) (writer: StreamWriter) =
    printfn "WS Headers: %A" headers
    let key =
        headers
        |> Seq.map (fun h -> let x = h.Split(": ") in x.[0], x.[1])
        |> Seq.filter (fun (n, _) -> n = "Sec-WebSocket-Key")
        |> Seq.map snd
        |> Seq.head
    printfn "Key: '%s'" key
    let salted = Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
    let sha1 = SHA1.Create().ComputeHash(salted)
    let accept = Convert.ToBase64String(sha1)
    writer.Write("HTTP/1.1 101 Switching Protocols\r\n")
    writer.Write("Upgrade: websocket\r\n")
    writer.Write("Connection: Upgrade\r\n")
    writer.Write("Sec-WebSocket-Accept: " + accept + "\r\n")
    writer.Write("\r\n")
    writer.Flush()

    sendString "Hello weirdo!" writer

    while true do
        let h0 = reader.BaseStream.ReadByte()
        if h0 = -1 then failwith "Socket closed"
        let h1 = reader.BaseStream.ReadByte()
        let fin = (h0 &&& 0x80) = 0x80
        let op = h0 &&& 0xF
        let maskflag = (h1 &&& 0x80) = 0x80
        let len =
            let len' = (int)(h1 &&& 0x7F)
            if len' = 126 then
                let len = reader.BaseStream.ReadByte()
                (len <<< 8) ||| reader.BaseStream.ReadByte()
            elif len' = 127 then
                for i in 0 .. 3 do
                    if reader.BaseStream.ReadByte() <> 0
                    then failwith "64-bit length unsupported" // TODO: support 64-bit length
                (reader.BaseStream.ReadByte() <<< 24) ||| (reader.BaseStream.ReadByte() <<< 16) ||| (reader.BaseStream.ReadByte() <<< 8) ||| reader.BaseStream.ReadByte()
            else len'

        let mask = Array.zeroCreate 4
        if maskflag then
            reader.BaseStream.Read(mask, 0, 4) |> ignore

        let payload = Array.zeroCreate<byte> len
        let count = reader.BaseStream.Read(payload, 0, len) // TODO: count not used?
        for i in 0 .. len - 1 do
            payload.[i] <- (byte)((payload.[i]) ^^^ (mask.[i % 4]))

        if payload.Length = 7 && payload.[0] = byte '_' && payload.[1] = byte 'c' && payload.[2] = byte 'l' && payload.[3] = byte 'o' && payload.[4] = byte 's' && payload.[5] = byte 'e' && payload.[6] = byte '_' then
            // special signal from client (otherwise browsers hold socket open for up to a minute!)
            failwith "Client requested close"

        match op with
            | 0 -> failwith "Continuation frames are unsupported."
            | 1 ->
                let text = Encoding.UTF8.GetString(payload)
                if fin then
                    printfn "TEXT: %s" text // TODO
                    let reply = Console.ReadLine()
                    sendString reply writer
                else failwith "Continuation frames are unsupported."
            | 2 ->
                if fin then printfn "BINARY: %A" payload // TODO
                else failwith "Continuation frames are unsupported."
            | 8 -> failwith "Closed"
            | 9 -> // ping
                sendBytes 10 payload writer
            | 10 -> () // pong - ignored
            | _ -> ()

let acceptClient () =
    let request, headers, reader, writer = acceptTcpClient ()
    match request.[1] with
    | "/" -> httpResponse writer
    | "/socket" -> webSocketResponse headers reader writer
    | req ->
        printfn "404: %A" req
        http404 writer

while true do
    try
        acceptClient ()
    with ex -> printfn "Error: %A" ex