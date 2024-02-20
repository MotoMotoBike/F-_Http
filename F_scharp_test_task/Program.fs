open System
open Akka.Actor
open System.Net.Http

type HttpRequestMessage(url: string, method: string) =
    member val Url = url
    member val Method = method

type LogResponse(response: string) =
    member val Response = response


type HttpRequestActor(responseLoggerActor: IActorRef) =
    inherit UntypedActor()

    let httpClient = new HttpClient()

    override this.OnReceive message =
        match message with
        | :? HttpRequestMessage as msg ->
            try
                let response = Async.RunSynchronously(Async.AwaitTask(httpClient.GetAsync(msg.Url)))
                let responseBody = response.Content.ReadAsStringAsync().Result

                responseLoggerActor.Tell(LogResponse(responseBody))
            with
            | ex -> Console.WriteLine(ex.Message)
        | _ -> ()

type ResponseLoggerActor() =
    inherit UntypedActor()

    override this.OnReceive message =
        match message with
        | :? LogResponse as msg ->
            Console.WriteLine(msg.Response)
        | _ -> ()

//Begin
let system = ActorSystem.Create("MySystem")

let responseLoggerActor = system.ActorOf(Props.Create(fun () -> ResponseLoggerActor()), "responseLoggerActor")
let httpRequestActor = system.ActorOf(Props.Create(fun () -> HttpRequestActor(responseLoggerActor)), "httpRequestActor")

httpRequestActor.Tell(HttpRequestMessage("https://jsonplaceholder.typicode.com/posts/10", "GET"), responseLoggerActor)
httpRequestActor.Tell(HttpRequestMessage("https://jsonplaceholder.typicode.com/posts/1", "GET"), responseLoggerActor)

Console.WriteLine "Демонстрация асинхронности"

Console.ReadLine() |> ignore
