module internal Utils

open Fake.Core
open System
open System.IO
open FSharp.Data
open FSharp.Json

let tee f a =
    f a
    a

let skipOn option action p =
    if p.Context.Arguments |> Seq.contains option then
        Trace.tracefn "Skipped ..."
    else
        action p

let createProcess exe arg dir =
    CreateProcess.fromRawCommandLine exe arg
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode

let run proc arg dir = proc arg dir |> Proc.run |> ignore

let orFail =
    function
    | Error e -> raise e
    | Ok ok -> ok

let stringToOption =
    function
    | null
    | "" -> None
    | string -> Some string

let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

let readJson (path: string) =
    use stream = new StreamReader(path)
    JsonValue.Load stream

let writeJson (path: string) content =
    use x = new StreamWriter(path)
    x.Write(Json.serialize content)



[<RequireQualifiedAccess>]
module Dotnet =
    let dotnet = createProcess "dotnet"

    let run command dir =
        try
            run dotnet command dir |> Ok
        with e ->
            Error e

    let runInRoot command = run command "."
    let runOrFail command dir = run command dir |> orFail
    let runInRootOrFail command = run command "." |> orFail

module Dir =
    let chop = String.split '/' >> List.rev
    let build = List.rev >> (fun (x: List<string>) -> String.Join('/', x))
    let removeLast = chop >> List.tail >> build

    let createPathIfMissing path =
        if Directory.Exists path |> not then
            Directory.CreateDirectory path |> ignore

        path

module DependencyUtil =

    let readLibPathFromDependencySpec path =
        let json = readJson path

        let libPaths =
            json.GetProperty("targets").Properties()
            |> Array.collect (snd >> fun x -> x.Properties())
            |> Array.map (fun (name, value) -> name, value.GetProperty("runtime").Properties() |> Array.map fst)
            |> Map.ofArray


        json.GetProperty("libraries").Properties()
        |> Array.map (fun (name, json) ->
            printfn $"{name} - {json}"
            Map.tryFind name libPaths, json.TryGetProperty("path") |> Option.map (fun x -> x.AsString()))
        |> Array.map (function
            | Some(names), Some(path) ->
                names
                |> Array.map (Dir.removeLast >> sprintf "%s/.nuget/packages/%s/%s" home path)
            | _ -> [||])
        |> Array.collect id
        |> Array.toList
