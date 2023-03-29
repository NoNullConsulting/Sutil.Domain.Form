open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Utils

let root = Path.combine __SOURCE_DIRECTORY__ ".."
let docs = Path.combine root "src/Docs"

module ProjectSources =

    let library = !!(root + "/src/**/*.fsproj")

    let all = library ++ "*.fsproj"

let watchDocs _ =

    Shell.Exec(
        "dotnet",
        $"fsdocs watch --input src/docs --clean --parameters root http://localhost:8000 --properties Configuration=Release --port 8000",
        root
    )
    |> ignore

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
    Target.create "Start" (watchDocs)


[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    |> initTargets

    "Clean" ==> "Build" ==> "Start" |> ignore

    Target.runOrDefault "build"
    0
