open Fake.Core

let clean () = ()



let initTargets () =
    // *** Define Targets ***
    Target.create "Hello" (fun _ -> printfn "hello from FAKE!")

    // *** Start Build ***
    Target.runOrDefault "Hello"

[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    |> initTargets

    0
