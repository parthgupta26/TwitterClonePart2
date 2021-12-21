#r "nuget: Akka" 
#r "nuget: Akka.FSharp" 
#r "nuget: FSharp.Data, Version=3.0.1"
//#r "nuget: WebSharper" 
//#r "nuget: WebSharper.Suave, Version=4.7.0.266" 
//#r "nuget: WebSharper.FSharp, Version=4.7.0.423" 
//#r "nuget: FsPickler, Version=3.4.0" 
//#r "nuget: FSharp.Core, Version=4.5.1"
open System
open Akka.Actor
open Akka.Actor
open Akka.Configuration
open Akka.Dispatch.SysMsg
open Akka.FSharp
open System.Threading
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

let system = ActorSystem.Create("FSharp")

type Information = 
    | RunClient
    | ID of IActorRef

type Json = JsonProvider<"""{
    "msg": "***sample tweet 1 #test"
}""">

let mutable loggedIn = false
let mutable uid = ""

let FeedActors (mailbox:Actor<_>) =
    let rec loop () = actor {
        let! message = mailbox.Receive()
        match message with 
        | RunClient -> 
                 while (true) do
                     //printfn "Inside"
                     if (uid <> "") then
                         let samples = Json.Load("http://localhost:5000/api/pending/"+uid)
                         match samples.Msg with
                         | "No feed" -> printf ""
                         |_ ->
                             //printfn "%s" samples.Msg
                             let feed = samples.Msg.Split("***")
                             printfn "You recieved new tweets :"
                             let mutable tweetMap: Map<int, string> = Map.empty
                             let mutable count = 1
                             for tweet in feed do
                                if(tweet <> "") then
                                    printfn "%i. %A" count tweet
                                    tweetMap <- tweetMap.Add(count, tweet)
                                    count <- count + 1  
                     Thread.Sleep 5000
        | _ -> ()
    }
    loop()

let feedActor = spawn system "feedActor" FeedActors
feedActor <! RunClient

module TwitterApiRequests = 
    let url = "http://localhost:5000/"
    let getResponseBody (level1: string) (level2: string) (json: string) =
        let response = Http.Request(
           url + level1 + "/" + level2,
           httpMethod = "POST",
           headers = [ ContentType HttpContentTypes.Json ],
           body = TextRequest json
           )
        response.Body

    let getResponse (level1: string) (level2: string) (json: string) =
        let response = Http.Request(
           url + level1 + "/" + level2,
           httpMethod = "POST",
           headers = [ ContentType HttpContentTypes.Json ],
           body = TextRequest json
           )
        response

    let loadJSON (level1: string)(level2: string)(identifier: string) =
        Json.Load(url + level1 + "/" + level2 + "/" + identifier)


while (true) do
    printfn "Please select an option from the menu below :"
    if (loggedIn) then
        printfn "1. Send Tweet"
        printfn "2. Subscribe"
        printfn "3. Get Feed"
        printfn "4. Query Hashtag"
        printfn "5. Query Mention"
        printfn "6. Logout"
        printf "Please enter your choice : "
        let option = System.Console.ReadLine()
        match option with
        | "1" -> printf "Please enter the tweet you wish to send : "
                 let tweet = System.Console.ReadLine()
                 let json = "{\"uid\":\"" + uid + "\",\"tweet\":\"" + tweet + "\"}"
                 let body = TwitterApiRequests.getResponseBody "api" "tweet" json
                 let content =
                    match body with
                    | Text a -> a
                    | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
                 let uiResult = content.Split('"').[1]
                 printfn "%s" uiResult
        | "2" -> printf "Please enter your user you wish to subscribe to : "
                 let username = System.Console.ReadLine()
                 let json = "{\"uid\":\"" + uid + "\",\"sid\":\"" + username + "\"}"
                 //printfn "%A" json
                 let body = TwitterApiRequests.getResponseBody "api" "subscribe" json
                 let content =
                    match body with
                    | Text a -> a
                    | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
                 let uiResult = content.Split('"').[1]
                 printfn "%s" uiResult
        | "3" -> let samples = TwitterApiRequests.loadJSON "api" "feed" uid
                 //Json.Load("http://localhost:5000/api/feed/"+uid)
                 match samples.Msg with
                 | "No feed" -> printfn "%s" samples.Msg
                 |_ ->
                     let feed = samples.Msg.Split("***")
                     printfn "Below is your feed :"
                     let mutable tweetMap: Map<int, string> = Map.empty
                     let mutable count = 1
                     for tweet in feed do
                        if(tweet <> "") then
                            printfn "%i. %A" count tweet
                            tweetMap <- tweetMap.Add(count, tweet)
                            count <- count + 1
                     printf "Do you wish to retweet (Y/N) : "
                     let option = System.Console.ReadLine()
                     match option with
                     | "Y" -> printf "Please enter the tweets numbers you wish to retweet (seperate by semicolon ';') : "
                              let nums = System.Console.ReadLine().Split(";")
                              for i in nums do
                                    let n = i |> int
                                    if(tweetMap.ContainsKey n) then
                                         //printfn "a"
                                         let json = "{\"uid\":\"" + uid + "\",\"tweet\":\"" + tweetMap.Item n + "\"}"
                                         //printfn "%A" json
                                         let response = TwitterApiRequests.getResponse "api" "retweet" json
                                         //let response = Http.Request(
                                         //   "http://localhost:5000/api/retweet",
                                         //   httpMethod = "POST",
                                         //   headers = [ ContentType HttpContentTypes.Json ],
                                         //   body = TextRequest json
                                         //   )
                                         printf ""
                              printfn "Tweets retweeted successfully"
                     |_ -> printf ""
        | "4" -> printf "Enter the hashtag you wish to query : "
                 let ht = System.Console.ReadLine()
                 let samples = TwitterApiRequests.loadJSON "api" "hashtag" ht
                 //Json.Load("http://localhost:5000/api/hashtag/"+ht)
                 match samples.Msg with
                 | "no such hashtags" -> printfn "%s" samples.Msg
                 |_ ->
                     let feed = samples.Msg.Split("***")
                     printfn "Below is your feed :"
                     let mutable tweetMap: Map<int, string> = Map.empty
                     let mutable count = 1
                     for tweet in feed do
                        if(tweet <> "") then
                            printfn "%i. %A" count tweet
                            tweetMap <- tweetMap.Add(count, tweet)
                            count <- count + 1
                     printf "Do you wish to retweet (Y/N) : "
                     let option = System.Console.ReadLine()
                     match option with
                     | "Y" -> printf "Please enter the tweets numbers you wish to retweet (seperate by semicolon ';') : "
                              let nums = System.Console.ReadLine().Split(";")
                              for i in nums do
                                    let n = i |> int
                                    printfn "%i" n
                                    if(tweetMap.ContainsKey n) then
                                         //printfn "a"
                                         let json = "{\"uid\":\"" + uid + "\",\"tweet\":\"" + tweetMap.Item n + "\"}"
                                         //printfn "%A" json
                                         let response = TwitterApiRequests.getResponse "api" "retweet" json
                                         //let response = Http.Request(
                                         //   "http://localhost:5000/api/retweet",
                                         //   httpMethod = "POST",
                                         //   headers = [ ContentType HttpContentTypes.Json ],
                                         //   body = TextRequest json
                                         //   )
                                         printf ""
                              printfn "Tweets retweeted successfully"
                     |_ -> printf ""
        | "5" -> let samples = TwitterApiRequests.loadJSON "api" "mention" uid
                 //Json.Load("http://localhost:5000/api/mention/"+uid)
                 match samples.Msg with
                 | "No feed" -> printfn "%s" samples.Msg
                 |_ ->
                     let feed = samples.Msg.Split("***")
                     printfn "Below is your feed :"
                     let mutable tweetMap: Map<int, string> = Map.empty
                     let mutable count = 1
                     for tweet in feed do
                        if(tweet <> "") then
                            printfn "%i. %A" count tweet
                            tweetMap <- tweetMap.Add(count, tweet)
                            count <- count + 1
                     printf "Do you wish to retweet (Y/N) : "
                     let option = System.Console.ReadLine()
                     match option with
                     | "Y" -> printf "Please enter the tweets numbers you wish to retweet (seperate by semicolon ';') : "
                              let nums = System.Console.ReadLine().Split(";")
                              for i in nums do
                                    let n = i |> int
                                    if(tweetMap.ContainsKey n) then
                                         //printfn "a"
                                         let json = "{\"uid\":\"" + uid + "\",\"tweet\":\"" + tweetMap.Item n + "\"}"
                                         //printfn "%A" json
                                         let response = TwitterApiRequests.getResponse "api" "retweet" json
                                         //let response = Http.Request(
                                           // "http://localhost:5000/api/retweet",
                                           // httpMethod = "POST",
                                           // headers = [ ContentType HttpContentTypes.Json ],
                                           // body = TextRequest json
                                           // )
                                         printf ""
                              printfn "Tweets retweeted successfully"
                     |_ -> printf ""
        | "6" -> let json = "{\"code\": 2,\"uid\":\"" + uid + "\",\"password\":\"" + "" + "\"}"
                 //printfn "%A" json
                 //let response = Http.Request(
                 //   "http://localhost:5000/api/register",
                 //   httpMethod = "POST",
                  //  headers = [ ContentType HttpContentTypes.Json ],
                  //  body = TextRequest json
                 //   )
                 let body = TwitterApiRequests.getResponseBody "api" "register" json
                 let content =
                    match body with
                    | Text a -> a
                    | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
                 let uiResult = content.Split('"').[1]
                 match uiResult with
                 | "successfully logged out" -> loggedIn <- false
                                                uid <- ""
                 |_ -> printfn ""
                 printfn "%s" uiResult
        |_ -> printfn ""
    else
        printfn "1. Register"
        printfn "2. Login"
        printf "Please enter your choice : "
        let option = System.Console.ReadLine()
        match option with
        | "1" -> printf "Please enter your username : "
                 let username = System.Console.ReadLine()
                 printf "Please enter your password : "
                 let password = System.Console.ReadLine()
                 //let json1 = " {\"command\":\"" + cmd + "\"} "
                 let json = "{\"code\": 0,\"uid\":\"" + username + "\",\"password\":\"" + password + "\"}"
                 //printfn "%A" json
                 //let response = Http.Request(
                  //  "http://localhost:5000/api/register",
                  //  httpMethod = "POST",
                   // headers = [ ContentType HttpContentTypes.Json ],
                   // body = TextRequest json
                   // )
                 let body = TwitterApiRequests.getResponseBody "api" "register" json
                 let content =
                    match body with
                    | Text a -> a
                    | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
                 let uiResult = content.Split('"').[1]
                 match uiResult with
                 | "registration is successful" -> loggedIn <- true
                                                   uid <- username
                                                   //feedActor <! Start
                 |_ -> printfn ""
                 printfn "%s" uiResult
                 
        | "2" -> printf "Please enter your username : "
                 let username = System.Console.ReadLine()
                 printf "Please enter your password : "
                 let password = System.Console.ReadLine()
                 //let json1 = " {\"command\":\"" + cmd + "\"} "
                 let json = "{\"code\": 1,\"uid\":\"" + username + "\",\"password\":\"" + password + "\"}"
                 //printfn "%A" json
                 //let response = Http.Request(
                  //  "http://localhost:5000/api/register",
                  //  httpMethod = "POST",
                  //  headers = [ ContentType HttpContentTypes.Json ],
                  //  body = TextRequest json
                  //  )
                 let body = TwitterApiRequests.getResponseBody "api" "register" json
                 let content =                 
                    match body with
                    | Text a -> a
                    | Binary b -> System.Text.ASCIIEncoding.ASCII.GetString b
                 let uiResult = content.Split('"').[1]
                 match uiResult with
                 | "login success" -> loggedIn <- true
                                      uid <- username
                                      //feedActor <! Start
                 |_ -> printfn ""
                 printfn "%s" uiResult
        |_ -> printfn ""