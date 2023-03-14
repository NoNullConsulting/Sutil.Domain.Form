namespace Sutil.Domain.Form

open System

open FsToolkit.ErrorHandling
open Sutil

type NumberParser<'T> =
    | IntParser of (int -> Validation<'T, string>)
    | DoubleParser of (double -> Validation<'T, string>)
    | DecimalParser of (decimal -> Validation<'T, string>)

module NumberInputHelper =
    let parseInt (str: string) =
        match Int32.TryParse str with
        | true, number -> Ok number
        | _ -> Validation.error "Not a valid int"

    let parseDouble (str: string) =
        match Double.TryParse str with
        | true, number -> Ok number
        | _ -> Validation.error "Not a valid double"

    let parseDecimal (str: string) =
        match Decimal.TryParse str with
        | true, number -> Ok number
        | _ -> Validation.error "Not a valid decimal"

    let parseFromString =
        function
        | IntParser p -> parseInt >> Validation.bind p
        | DoubleParser p -> parseDouble >> Validation.bind p
        | DecimalParser p -> parseDecimal >> Validation.bind p

    let isValidNumberOrSkip parser (store: Store<string>) str =
        match parser with
        | IntParser _ -> parseInt str |> Result.isOk
        | DoubleParser _ -> parseDouble str |> Result.isOk
        | DecimalParser _ -> parseDecimal str |> Result.isOk
        |> fun isValid ->
            if isValid then
                Store.set store str
            else
                Store.set store (Store.current store)


module NumberInput =

    type NumberInput<'TResult when 'TResult: equality> =
        { Placeholder: string option
          Parser: NumberParser<'TResult>
          AutoCompleate: string option
          ContainerProps: FormElementContainer }

        interface IFormElement<NumberInput<'TResult>> with
            member model.UpdateFormContainer(mapper) =
                { model with
                    ContainerProps = mapper model.ContainerProps }

    let create () =
        { Placeholder = None
          Parser = IntParser Ok
          AutoCompleate = None
          ContainerProps = FormElementContainer.create () }

    type NumberInput<'TResult when 'TResult: equality> with

        member model.setParser(parser) =
            { Placeholder = model.Placeholder
              Parser = parser
              AutoCompleate = model.AutoCompleate
              ContainerProps = model.ContainerProps }

        member inline model.withParser(parser) = model.setParser (DecimalParser parser)
        member inline model.withParser(parser) = model.setParser (DoubleParser parser)
        member inline model.withParser(parser) = model.setParser (IntParser parser)

        member inline model.setAutoCompleateName(str) = { model with AutoCompleate = Some str }

        member inline model.placeholder(str) = { model with Placeholder = Some str }

        member inline model.render() : FormElement<_> =
            let parseFromString = NumberInputHelper.parseFromString model.Parser
            let result = Store.make ("" |> parseFromString)

            let view =
                model.ContainerProps.render
                    [ Bind.toggleClass (isError' result, "error")
                      Html.input [ Attr.typeNumber; Bind.attr ("value", parseFromString >> Store.set result) ]
                      Html.div [ Bind.each (getErrors' result, (fun err -> Html.span [ Html.text err ])) ] ]

            result, view
