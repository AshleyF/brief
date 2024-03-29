﻿module Remote

open System.IO
open System.Net.Sockets
open System.Threading
open Structure
open Syntax
open Primitives
open Actor

// e.g. post 'remote [connect '127.0.0.1 11411]

let remoteActor =
    let mutable (channel : BriefActor option) = None
    let remoteState =
        [
            primitive "connect" (fun s ->
                match getStack s with
                | String host :: Number port :: t ->
                    let reader = new BinaryReader((new TcpClient(host, int port)).GetStream())
                    let rec read () =
                        let source = reader.ReadString()
                        printfn "Remote: %s" source
                        source |> brief |> channel.Value.Post
                        read ()
                    (new Thread(new ThreadStart(read), IsBackground = true)).Start()
                    setStack t s
                | _ :: _ :: _ -> failwith "Expected ss"
                | _ -> failwith "Stack underflow")

        ] |> addPrimitives primitiveState

    let chan = actor remoteState
    channel <- Some chan
    chan
