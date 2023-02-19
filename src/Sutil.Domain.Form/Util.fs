namespace global

open System

open FsToolkit.ErrorHandling
open Sutil
open Sutil.Core

module Option =
    let sequence =
        function
        | Some v -> Validation.map Some v
        | None -> Ok None

    let isRequired = 
        function 
        | Some v -> Validation.ok v
        | None -> Validation.error "Required"

[<AutoOpen>]
module ObservableUtil =
    let (..>) (value : IObservable<'T>) (mapper : 'T -> 'R) =
        value .> mapper |> Store.distinct

    let tryGetError  = 
        function
        | Ok _ -> None
        | Result.Error error -> Some error

    let getErrorList =
        function
        | Ok _ -> []
        | Validation.Error errs -> errs

    type IObservable<'T> with
        member inline obs.debounce(timeout : int) : IObservable<'T>=
            let count = Store.make 0
            let value = Store.make (Store.current obs)

            Observable.add (fun t -> async {
                count <~= (+) 1
                do! Async.Sleep timeout
                count <~= (fun c -> 
                    match c with
                    | 0 -> 
                        value <~ t
                        0
                    | x -> x
                )
                return ()
            } >> Async.Start) obs

            value

    type IObservable<'T> with
        member inline obs.map(mapper) =
            Observable.map mapper obs

type ObservableValidation<'T> = 
    IObservable<Result<'T,string>>

[<AutoOpen>]
module ObservableValidationUtil = 
    let inline map' f observableResult = Store.map (Result.bind f) observableResult  
    let inline isError' observableResult = (..>) observableResult Result.isError
    let inline getErrors' observableResult =  (..>) observableResult getErrorList
            
[<AutoOpen>]
module List = 
    type List<'T> with
        member inline lst.setI(indexToUpdate, updater) =
            lst
            |> List.mapi 
                (fun index value -> 
                    if index = indexToUpdate
                    then updater value
                    else value)
        
        member inline lst.setI(indexToUpdate, value) =
            lst.setI(indexToUpdate, fun _ -> value )
                                        

    let updateI indexToUpdate updater (lst : List<'T>) =
        lst.setI(indexToUpdate, updater)


[<AutoOpen>]
module Bind = 
    let inline eachoi (lst : IObservable<seq<'T>>, fn: IObservable<'T> -> int -> SutilElement) =
        let getItem index = lst ..> Seq.item index
        Bind.el(lst, fun items -> Html.div[for index in 0.. (Seq.length items) -> fn (getItem index) index] )