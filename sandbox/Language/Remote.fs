module Remote

open System.IO
open System.Net.Sockets
open System.Threading
open Syntax
open Primitives
open Actor

let remoteActor host port =
    let channel = actor primitiveState
    let client = new TcpClient(host, port)
    let stream = client.GetStream()
    let reader = new BinaryReader(stream)
    let rec read () =
        let source = reader.ReadString()
        printfn "Remote: %s" source
        source |> brief |> channel.Post
        read ()
    (new Thread(new ThreadStart(read), IsBackground = true)).Start()

