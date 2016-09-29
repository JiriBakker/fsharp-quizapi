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

let private toJsonOk result =
    OK (JsonConvert.SerializeObject result)
    >=> Writers.setMimeType "application/json; charset=utf-8"

let app (questions:IDictionary<int,Question>) =
    let getRandomQuestion() =
        // TODO JB kan dit efficienter?
        let rand = Random()        
        let randNum = rand.Next(0, questions.Count)
        let randKey = questions.Keys.ToList().[randNum]
        questions.[randKey]

    let getSpecificQuestion id =
        // TODO JB wat als question met 'id' niet bestaat?
        questions.[id]
    
    choose [ 
        GET >=> choose 
            [ path "/question/random" >=> (fun (httpContext) -> toJsonOk (getRandomQuestion()) httpContext) ;
              pathScan "/question/%d"     (fun (questionId)  -> toJsonOk (getSpecificQuestion questionId)) ];
        POST >=> choose
            [ pathScan "/answer/%d/%s"    (fun (questionId, answerKey) -> OK ("answer " + answerKey)) ] 
    ]

[<EntryPoint>]
let main _ =
    let questions = QuizPool.load "..\..\OpenTriviaQA\categories"
    startWebServer defaultConfig (app questions)
    0