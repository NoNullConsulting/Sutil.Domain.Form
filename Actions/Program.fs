open System
open System.IO

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Utils
open FSharp.Formatting.ApiDocs

let root = Path.combine __SOURCE_DIRECTORY__ ".."
let docs = Path.combine root "src/Docs"

module ProjectSources =

    let library = !!(root + "/src/**/*.fsproj")

    let all = library ++ "*.fsproj"

let generateDocsFromProjectPath (projectPath: string) =

    Shell.cd root

    let output = Path.combine root "src/docs/" |> Dir.createPathIfMissing

    Fsdocs.build (fun p ->
        { p with
            Input = Some(Path.combine root "src/docs")
            Projects = Some [ projectPath ]
            Output = Some output
            Properties = Some " Configuration=Release "
            Parameters = Some [ "root", "http://localhost:8000/" ] })

    Shell.mv "./src/docs/index.json" "./src/docs/content/index.json"

let generateDocs _ =
    ProjectSources.library |> Seq.iter generateDocsFromProjectPath

let watchDocs _ =
    Shell.cd (Path.combine root "src/")
    Fsdocs.watch (fun p -> { p with Port = Some(8000) })

let failOnBadExitAndPrint (p: ProcessResult) =
    if p.ExitCode <> 0 then
        p.Errors |> Seq.iter Trace.traceError
        failwithf "failed with exitcode %d" p.ExitCode

let clean =
    let cleanDirs _ =
        !! "../**/bin/Release" ++ "../**/bin/Debug" ++ "../**/obj" ++ "../**/.ionide"
        -- "../Actions/**"
        |> Shell.cleanDirs

    cleanDirs |> skipOn "no-clean"


let build () =
    ProjectSources.all |> Seq.iter (DotNet.build id)


let initTargets () =
    Target.initEnvironment ()

    Target.create "Clean" clean
    Target.create "Build" (fun _ -> build ())
    Target.create "Docs" (generateDocs)
    Target.create "startDocsServer" (watchDocs)


[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    |> initTargets

    "Clean" ==> "Build" ==> "Docs" ==> "startDocsServer" |> ignore

    Target.runOrDefault "build"
    0
