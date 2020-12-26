open System.IO
open System.Net
open System.Net.Sockets
open System.Speech.Synthesis
open System.Speech.Recognition
open System.Threading

let mutable writer : BinaryWriter option = None
let server () =
    let listener = new TcpListener(IPAddress.Loopback, 11411)
    listener.Start()
    while true do
        try
            printfn "Waiting for connection..."
            writer <- Some (new BinaryWriter(listener.AcceptTcpClient().GetStream()))
            printfn "Connected"
        with ex -> printfn "Connection Error: %s" ex.Message
(new Thread(new ThreadStart(server), IsBackground = true)).Start()

let post brief =
    printfn "Brief: %s" brief
    match writer with
    | Some w -> w.Write(brief)
    | None -> printf "No connection"

let synth = new SpeechSynthesizer(Rate = 2)
synth.SelectVoiceByHints(VoiceGender.Neutral)

let reco = new SpeechRecognitionEngine()
try
    reco.SetInputToDefaultAudioDevice()
with _ -> failwith "No default audio device! Plug in a microphone, man."

type GrammarAST<'a> =
    | Phrase   of string * string option
    | Optional of GrammarAST<'a>
    | Sequence of GrammarAST<'a> list
    | Choice   of GrammarAST<'a> list
    | Dictation

let rec speechGrammar = function
    | Phrase (say, Some value) ->
        let g = new GrammarBuilder(say)
        g.Append(new SemanticResultValue(value))
        g
    | Phrase (say, None) -> new GrammarBuilder(say)
    | Optional g -> new GrammarBuilder(speechGrammar g, 0, 1)
    | Sequence gs ->
        let builder = new GrammarBuilder()
        List.iter (fun g -> builder.Append(speechGrammar g)) gs
        builder
    | Choice cs -> new GrammarBuilder(new Choices(List.map speechGrammar cs |> Array.ofList))
    | Dictation ->
        let dict = new GrammarBuilder()
        dict.AppendDictation()
        dict

let briefLightsOn = "'Turning lights on|post 'trigger [hook ifttt-key 'all-lights-on ' ' ']"
let briefLightsOff = "Turning lights off|post 'trigger [hook ifttt-key 'all-lights-off ' ' ']"
let briefLightsDim = "Dimming lights|post 'trigger [hook ifttt-key 'all-lights-dim '50 ' ']"
let briefLightsBright = "Making lights bright|post 'trigger [hook ifttt-key 'all-lights-dim '100 ' ']"

let lightsOn = Choice [
    Phrase ("Illuminate",         Some briefLightsOn)
    Phrase ("Turn on",            Some briefLightsOn)
    Phrase ("Lights on",          Some briefLightsOn)
    Phrase ("Turn lights on",     Some briefLightsOn)
    Phrase ("Turn the lights on", Some briefLightsOn)]

let lightsOff = Choice [
    Phrase ("Turn off",            Some briefLightsOff)
    Phrase ("Lights off",          Some briefLightsOff)
    Phrase ("Turn lights off",     Some briefLightsOff)
    Phrase ("Turn the lights off", Some briefLightsOff)]

let lightsDim = Choice [
    Phrase ("Dim",            Some briefLightsDim)
    Phrase ("Dim lights",     Some briefLightsDim)
    Phrase ("Lights dim",     Some briefLightsDim)
    Phrase ("Dim the lights", Some briefLightsDim)]

let lightsBright = Choice [
    Phrase ("Bright",                 Some briefLightsBright)
    Phrase ("Brighten",               Some briefLightsBright)
    Phrase ("Bright lights",          Some briefLightsBright)
    Phrase ("Lights bright",          Some briefLightsBright)
    Phrase ("Brigten lights",         Some briefLightsBright)
    Phrase ("Brighten the lights",    Some briefLightsBright)
    Phrase ("Make the lights bright", Some briefLightsBright)]

let lightsColor =
    let lights = Choice [
        Phrase ("Lights",          None)
        Phrase ("Make lights",     None)
        Phrase ("Make the lights", None)
        Phrase ("Turn the lights", None)
        Phrase ("Turn lights",     None)]
    let color = Choice [
        Phrase ("Red",    Some "lights-color:red")
        Phrase ("Orange", Some "lights-color:orange")
        Phrase ("Yellow", Some "lights-color:yellow")
        Phrase ("Green",  Some "lights-color:green")
        Phrase ("Blue",   Some "lights-color:blue")
        Phrase ("Purple", Some "lights-color:purple")
        Phrase ("White",  Some "lights-color:white")]
    Sequence [lights; color]

let lightsFade = Choice [
    Phrase ("Go to sleep",      Some "lights-fade")
    Phrase ("Sleep lights",     Some "lights-fade")
    Phrase ("Sleep mode",       Some "lights-fade")
    Phrase ("Fade lights",      Some "lights-fade")
    Phrase ("Fade away lights", Some "lights-fade")]

let lightsExtra = Choice [
    Phrase ("Fuck mode", Some "lights-color:red")]

let lights = Choice [lightsOn; lightsOff; lightsDim; lightsBright; lightsColor; lightsFade; lightsExtra]

let grammar = new Grammar(speechGrammar lights)
reco.LoadGrammar(grammar)

synth.Speak("Talk to me Goose!")

let mutable fading = false
let rec fade () =
    fading <- true
    synth.Speak("Slowly dimming lights")
    let rec fade' level =
        printfn "Fade: %i" level
        level |> sprintf "post 'trigger [hook ifttt-key 'all-lights-dim '%i ' ']" |> post
        Thread.Sleep(5 * 60 * 1000 / 100)
        if fading then
            if level > 0 then fade' (level - 1)
            else post "post 'trigger [hook ifttt-key 'all-lights-off ' ' ']"
    (new Thread(new ThreadStart(fun () -> fade' 100), IsBackground = true)).Start()

while true do
    let res = reco.Recognize()
    if res <> null then
        printfn "Sync Reco: %s %f" res.Text res.Confidence
        let sem = if res.Semantics.Value = null then None else Some (res.Semantics.Value :?> string)
        match sem with
        | Some s ->
            if res.Confidence > 0.7f then
                fading <- false
                match s with
                | "lights-fade" -> fade ()
                | s ->
                    if s.StartsWith("lights-color:") then
                        let color = s.Substring(13)
                        synth.Speak(sprintf "Lights %s" color)
                        color |> sprintf "post 'trigger [hook ifttt-key 'all-lights-color '%s ' ']" |> post
                    else
                        let pair = s.Split('|')
                        synth.Speak(pair.[0])
                        post pair.[1]
            else synth.Speak(sprintf "I heard %s but I'm not sure..." res.Text)
        | None -> synth.Speak("what?")
