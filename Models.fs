module Models 

open System.Collections.Generic

type Question = { 
    Id               : int;
    Text             : string;
    Answers          : IDictionary<string, string>
    CorrectAnswerKey : string
}

type CheckedAnswer = {
    QuestionId       : int;
    AnswerKey        : string;
    CorrectAnswerKey : string;
    IsCorrect        : bool;
}

type ErrorResult = {
    ErrorCode    : int;
    ErrorMessage : string;
}