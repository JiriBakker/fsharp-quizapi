module QuizPool

open System
open System.IO
open System.Collections.Generic
open Models

type private QuestionParsePhase = 
    | Idle
    | QuestionText
    | Answers


type private QuestionParseState = {
    QuestionTextLines : string list;
    CorrectAnswerText : string option;
    Answers           : (string * string) list;
}

type private OverallParseState = { 
    Phase           : QuestionParsePhase;
    ParsedQuestions : Question list;
    CurrentQuestion : QuestionParseState;
}

let private formatAsString (input:obj) =
    match input with
    | :? string      as s  -> s
    | :? (char list) as cl -> new String(cl |> List.toArray)
    | :? char        as c  -> new String([|c|])
    | _                    -> input.ToString()
    |> (fun s -> s.Trim())

let private emptyQuestionParseState = { QuestionTextLines = []; Answers = []; CorrectAnswerText = None }

let rec private parseQuestionLines id (stateSoFar:OverallParseState) (remainingLines:string list) =
    let appendQuestionTextLine line =
        { stateSoFar with CurrentQuestion = { stateSoFar.CurrentQuestion with QuestionTextLines = (formatAsString line) :: stateSoFar.CurrentQuestion.QuestionTextLines } }

    let setFirstQuestionLine line =
        { (appendQuestionTextLine line) with Phase = QuestionText }

    let appendAnswer (key, text) = 
        { stateSoFar with CurrentQuestion = { stateSoFar.CurrentQuestion with Answers = (formatAsString key, formatAsString text) :: stateSoFar.CurrentQuestion.Answers } }

    let setCorrectAnswerText text =
        { stateSoFar with Phase = Answers;  CurrentQuestion = { stateSoFar.CurrentQuestion with CorrectAnswerText = Some(formatAsString text); } }

    let parseQuestion questionParseState =        
        let correctAnswerKey = 
            questionParseState.Answers
            |> List.tryFind (fun answer -> (snd answer) = questionParseState.CorrectAnswerText.Value)
            |> (fun result -> match result with | None -> None | Some(key, text) -> Some(key))

        match correctAnswerKey with
        | None      -> []
        | Some(key) -> [{ 
                           Id = id; 
                           Text = String.Join("\n", List.rev questionParseState.QuestionTextLines); 
                           CorrectAnswerKey = key
                           Answers = List.rev questionParseState.Answers |> dict
                       }]        

    let parseQuestionLine (line:string) =
        match line.ToCharArray() |> List.ofArray with
        | [] -> 
            match stateSoFar.Phase with
            | Idle         -> stateSoFar
            | QuestionText -> { stateSoFar with CurrentQuestion = { stateSoFar.CurrentQuestion with QuestionTextLines = "" :: stateSoFar.CurrentQuestion.QuestionTextLines } }
            | Answers      -> { stateSoFar with Phase = Idle; ParsedQuestions = (parseQuestion stateSoFar.CurrentQuestion) @ stateSoFar.ParsedQuestions; CurrentQuestion = emptyQuestionParseState }
        | c1::c2::text when c1 = '#' && c2 = 'Q' -> setFirstQuestionLine text
        | c1::text     when c1 = '^'             -> setCorrectAnswerText text
        | key::text                              -> 
            match stateSoFar.Phase with
            | Idle          -> stateSoFar
            | QuestionText  -> appendQuestionTextLine line 
            | Answers       -> appendAnswer (key, text)

    match remainingLines with
    | []          -> stateSoFar
    | line::lines -> parseQuestionLines (id + 1) (parseQuestionLine line) lines
        

let private loadQuestionsFromFile (fileInfo:FileInfo) =
    File.ReadAllLines(fileInfo.FullName)
    |> List.ofArray 
    |> parseQuestionLines 1 { Phase = Idle; ParsedQuestions = []; CurrentQuestion = emptyQuestionParseState }
    |> (fun parseState -> parseState.ParsedQuestions)

let load path = 
    let dir = new DirectoryInfo(path)
    dir.GetFiles()
    |> List.ofArray
    |> List.fold (fun acc fileInfo -> (loadQuestionsFromFile fileInfo) @ acc) []
    |> List.map (fun q -> (q.Id, q)) |> dict