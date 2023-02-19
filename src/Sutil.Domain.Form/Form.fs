namespace Sutil.Domain.Form

open System

open Fable.Core.JsInterop
open FsToolkit.ErrorHandling
open Sutil
open Sutil.Core
open Sutil.CoreElements


type FormElement<'T> = IObservable<Validation<'T, string>> * SutilElement

module Form =
    importSideEffects "./base.scss"

    let create () = Store.make(Ok id), []

    let add ((value', elementView): FormElement<_>) (fun', views)  =
        Store.zip fun' value'
        |> Store.map (fun (f, value) -> validation {
            let! func = f
            and! v = value
            return func >> (fun d -> d v)
        }) , views@[elementView]


    let aggreate (fun', elementView) f =
        fun'
        |> map' ((|>) f)
        |> Store.distinct 
        , Html.div 
            [ Attr.className "df-form-group" 
              yield! elementView 
            ]
    
    let dispatch (fun', elementView) dispatcher = 
        Store.subscribe dispatcher fun' |> ignore
        Html.form [elementView]

type FormElementContainer =
    { Label: SutilElement option
      CssClass: string
      Span: int 
    }
    with 
        static member create() =
            { Label = None
            ; CssClass = ""
            ; Span = 0 }

        member props.render children =
            el "form-element"
                [ if props.Span > 0 then (Attr.style $"flex-grow: {props.Span}")
                  if props.CssClass <> "" then Attr.className props.CssClass
                  Html.label (props.Label |> Option.toList)
                  el "form-content" children 
                ]


type IFormElement<'T> =
    abstract UpdateFormContainer : (FormElementContainer -> FormElementContainer) -> 'T

[<AutoOpen>]
module FormElementExtensions = 

    type IFormElement<'T> with
        member inline model.setLabel(lable) =
            model.UpdateFormContainer(fun el-> { el with Label = Some lable })

        member inline model.setLabel(lable) =
            model.UpdateFormContainer(fun el-> { el with Label = Some (text lable) })
    
        member inline model.setClass css =
            model.UpdateFormContainer(fun el -> {el with CssClass = css})

        member inline model.addClass css =
            model.UpdateFormContainer(
                fun el -> {el with CssClass = $"{el.CssClass} %s{css}"})
        
        member inline model.withColumnSpan span =
            model.UpdateFormContainer(fun el-> {el with Span = span})


    let inline (>++) form (fe: FormElement<'Q>)  = Form.add fe form
    let inline (>==) fe f  = Form.aggreate fe f 
    let inline (>||) fe f  = Form.dispatch fe f 

