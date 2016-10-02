module Api

open System
open System.Collections.Generic
open System.Linq
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Models
open Newtonsoft.Json

type QuestionResponse = { 
    Id               : int;
    Text             : string;
    Answers          : IDictionary<string, string>
} 
    

let private toJsonOk result =
    Writers.setMimeType "application/json; charset=utf-8"
    >=> Writers.addHeader "Access-Control-Allow-Origin" "*"
    >=> OK (JsonConvert.SerializeObject result)

let private mapToQuestionResponse (question:Question) =
    { 
        Id               = question.Id;
        Text             = question.Text;
        Answers          = question.Answers
    } 

let app (questions:IDictionary<int,Question>) =
    let getRandomQuestion() =
        // TODO JB kan dit efficienter?
        let rand = Random()        
        let randNum = rand.Next(0, questions.Count)
        let randKey = questions.Keys.ToList().[randNum]
        questions.[randKey]
        |> mapToQuestionResponse

    let getSpecificQuestion id =
        // TODO JB wat als question met 'id' niet bestaat?
        questions.[id]
        |> mapToQuestionResponse

    let checkAnswer questionId answerKey =
        let question = questions.[questionId]
        {
            QuestionId       = questionId;
            AnswerKey        = answerKey;
            CorrectAnswerKey = question.CorrectAnswerKey;
            IsCorrect        = question.CorrectAnswerKey.ToLowerInvariant().Equals(answerKey.ToLowerInvariant())
        }
    
    choose [ 
        GET >=> choose 
            [ path "/question/random" >=> (fun (httpContext) -> toJsonOk (getRandomQuestion()) httpContext) ;
              pathScan "/question/%d"     (fun (questionId)  -> toJsonOk (getSpecificQuestion questionId)) ;
              pathScan "/answer/%d/%s"    (fun (questionId, answerKey) -> toJsonOk (checkAnswer questionId answerKey)) ] 
    ]

[<EntryPoint>]
let main _ =
    let questions = QuizPool.load "..\..\OpenTriviaQA\categories"
    startWebServer defaultConfig (app questions)
    0