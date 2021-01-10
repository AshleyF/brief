module Tesla

// https://github.com/AshleyF/tesla

open System
open System.IO
open System.Net.Http
open System.Text
open Newtonsoft.Json
open Structure
open Primitives
open Actor

type Tesla(user: string, password: string, vin: string) =
    let baseAddress = new Uri("https://owner-api.teslamotors.com/")
    let auth (user: string) (password: string) = async {
        use client = new HttpClient(BaseAddress = baseAddress)
        let id = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384"
        let secret = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3"
        let body = sprintf "{ \"email\": \"%s\", \"password\": \"%s\", \"grant_type\": \"password\", \"client_id\": \"%s\", \"client_secret\": \"%s\" }" user password id secret
        use content = new StringContent(body, Encoding.UTF8, "application/json")
        use! response = Async.AwaitTask (client.PostAsync("oauth/token", content))
        response.EnsureSuccessStatusCode() |> ignore
        let! data = Async.AwaitTask (response.Content.ReadAsStringAsync())
        let reader = new JsonTextReader(new StringReader(data))
        let rec readToken () =
            if reader.Read () then
                if reader.TokenType = JsonToken.PropertyName && string reader.Value = "access_token"
                then reader.ReadAsString()
                else readToken ()
            else failwith "Access token not found"
        return readToken () }
    let vehicle (vin: string) (token: string) = async {
        use client = new HttpClient(BaseAddress = baseAddress)
        client.Timeout <- TimeSpan.FromMinutes 1.
        client.DefaultRequestHeaders.Add("Authorization", sprintf "Bearer %s" token)
        use! response = Async.AwaitTask (client.GetAsync("api/1/vehicles"))
        response.EnsureSuccessStatusCode() |> ignore
        let! data = Async.AwaitTask (response.Content.ReadAsStringAsync())
        let reader = new JsonTextReader(new StringReader(data))
        let rec readId () = // TODO: support multiple vehicles
            if reader.Read () then
                if reader.TokenType = JsonToken.PropertyName && string reader.Value = "id_s"
                then reader.ReadAsString()
                else readId ()
            else failwith "Vehicle ID not found"
        return readId () }
    let token = auth user password |> Async.RunSynchronously
    let id = vehicle vin token |> Async.RunSynchronously
    let call get api = async {
        use client = new HttpClient(BaseAddress = baseAddress)
        client.DefaultRequestHeaders.Add("Authorization", sprintf "Bearer %s" token)
        let uri = sprintf "api/1/vehicles/%s/%s" id api
        use! response = Async.AwaitTask (if get then client.GetAsync(uri) else client.PostAsync(uri, new StringContent(String.Empty)))
        response.EnsureSuccessStatusCode() |> ignore
        let! data = Async.AwaitTask (response.Content.ReadAsStringAsync())
        // printfn "DEBUG: %s -> %s" uri data
        return data }
    let query api = call true api |> Async.RunSynchronously
    let command = sprintf "command/%s" >> call false >> Async.RunSynchronously
    member this.MobileEnabled() = query "mobile_enabled"
    member this.ChargeState() = query "data_request/charge_state"
    member this.ClimateState() = query "data_request/climate_state"
    member this.DriveState() = query "data_request/drive_state"
    member this.GuiSettings() = query "data_request/gui_settings"
    member this.VehicleState() = query "data_request/vehicle_state"
    member this.WakeUp() = command "wake_up"
    member this.SetValetMode(on, password) = sprintf "set_valet_mode?on=%b&password=%i" on password |> command
    member this.ResetValetPin() = command "reset_valet_pin"
    member this.ChargePortDoorOpen() = command "charge_port_door_open"
    member this.ChargeStandard() = command "charge_standard"
    member this.ChargeMaxRange() = command "charge_max_range"
    member this.SetChargeLimit(percent) = sprintf "set_charge_limit?percent=%i" percent |> command
    member this.ChargeStart() = command "charge_start"
    member this.ChargeStop() = command "charge_stop"
    member this.FlashLights() = command "flash_lights"
    member this.HonkHorn() = command "honk_horn"
    member this.DoorUnlock() = command "door_unlock"
    member this.DoorLock() = command "door_lock"
    member this.SetTemperatures(driver, passenger) = sprintf "set_temps?driver_temp=%.1f&passenger_temp=%.1f" driver passenger |> command
    member this.AutoConditioningStart() = command "auto_conditioning_start"
    member this.AutoConditioningStop() = command "auto_conditioning_stop"
    member this.SunRoofControl(state, percent) = sprintf "sun_roof_control?state=%s&percent=%i" state percent |> command
    member this.RemoteStartDrive(password) = sprintf "remote_start_drive?password=%s" password |> command

let teslaActor =

    let mutable car = None

    let teslaCommand name fn = primitive name (fun s ->
        match car with
        | Some c -> pushStack (String (fn c)) s
        | None -> failwith "No Tesla car connected")

    let teslaState =
        [
            primitive "auth" (fun s ->
                match getStack s with
                | String vin :: String name :: String pass :: t ->
                    car <- Some (new Tesla(name, pass, vin))
                    setStack t s
                | _ :: _ :: _ :: _ -> failwith "Expected sss"
                | _ -> failwith "Stack underflow")

            teslaCommand "wake" (fun c -> c.WakeUp())
            teslaCommand "honk" (fun c -> c.HonkHorn())
            teslaCommand "flash" (fun c -> c.FlashLights())
            teslaCommand "lock" (fun c -> c.DoorLock())
            teslaCommand "unlock" (fun c -> c.DoorUnlock())
            teslaCommand "startac" (fun c -> c.AutoConditioningStart())
            teslaCommand "stopac" (fun c -> c.AutoConditioningStop())
            teslaCommand "charge?" (fun c -> c.ChargeState())
            teslaCommand "climate?" (fun c -> c.ClimateState())
            teslaCommand "drive?" (fun c -> c.DriveState())
            teslaCommand "gui?" (fun c -> c.GuiSettings())
            teslaCommand "vehicle?" (fun c -> c.VehicleState())

            primitive "charge" (fun s ->
                match car with
                | Some c ->
                    match getStack s with
                    | Number limit :: t -> setStack (String (c.SetChargeLimit(int limit)) :: t) s
                    | _ :: _ -> failwith "Expected n"
                    | _ -> failwith "Stack underflow"
                | None -> failwith "No Tesla car connected")

            primitive "temperature" (fun s ->
                match car with
                | Some c ->
                    match getStack s with
                    | Number driver :: Number passenger :: t ->
                        setStack (String (c.SetTemperatures(float driver, float passenger)) :: t) s
                    | _ :: _ :: _ -> failwith "Expected nn"
                    | _ -> failwith "Stack underflow"
                | None -> failwith "No Tesla car connected")

        ] |> addPrimitives primitiveState

    actor teslaState
