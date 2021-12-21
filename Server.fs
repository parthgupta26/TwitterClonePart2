module PeopleApi.App

//-r "nuget: Akka" 
//-r "nuget: Akka.FSharp" 
open System
open System.Collections.Generic
open WebSharper
open WebSharper.Sitelets
(*open Akka.Actor
open Akka.Configuration
open Akka.FSharp*)

/// The types used by this application.
module Core =

    /// Data about a person. Used both for storage and JSON parsing/writing.
    type RegisterMsg = {code : int; uid : string; password : string}
    type TweetMsg = {uid : string; tweet : string}
    type SubscribeMsg = {uid : string; sid : string}
    type HashTagMsg = {ht : string; uid : string}
    type MentionMsg = {men : string; uid : string}
    type RetweetMsg = {uid : string; tweet : string}
    type ResultMsg = {msg : string}


    /// The type of REST API endpoints.
    /// This defines the set of requests accepted by our API.
    type ApiEndPoint =

        | [<EndPoint "POST /register"; Json "registerMsg">]
            RegAPI of registerMsg: RegisterMsg

        | [<EndPoint "POST /tweet"; Json "tweetMsg">]
            TweetAPI of tweetMsg: TweetMsg

        | [<EndPoint "POST /retweet"; Json "retweetMsg">]
            RetweetAPI of retweetMsg: RetweetMsg
        
        | [<EndPoint "POST /subscribe"; Json "subscribeMsg">]
            SubscribeAPI of subscribeMsg: SubscribeMsg

        | [<EndPoint "GET /hashtag">]
            HashtagAPI of ht: string

        | [<EndPoint "GET /mention">]
            MentionAPI of men: string
        
        | [<EndPoint "GET /feed">]
            FeedAPI of feed: string

        | [<EndPoint "GET /pending">]
            PendingAPI of pending: string
        
    /// The type of all endpoints for the application.
    type WebAPIEndPoint =
        
        /// Accepts requests to /
        | [<EndPoint "/">] Index

        /// Accepts requests to /api/...
        | [<EndPoint "/api">] Api of Cors<ApiEndPoint>

    /// Error result value.
    type Error = { error : string }

    /// Alias representing the success or failure of an operation.
    /// The Ok case contains a success value to return as JSON.
    /// The Error case contains an HTTP status and a JSON error to return.
    type ApiResult<'T> = Result<'T, Http.Status * Error>

    /// Result value for CreatePerson.
    type Id = { id : int }

open Core



let mutable userIdSet: Set<string> = Set.empty
let mutable userIdMap:  Map<string, string> = Map.empty
let mutable onlineUsersMap:  Map<string, bool> = Map.empty
let mutable hashTagsMap: Map<string, list<string>> = Map.empty
let mutable mentionMap: Map<string, list<string>> = Map.empty
let mutable pendingMap: Map<string, list<string>> = Map.empty
let mutable feedMap: Map<string, list<string>> = Map.empty
let mutable followersMap: Map<string, list<string>> = Map.empty
let mutable followingMap: Map<string, list<string>> = Map.empty

let registerUser (id: string) (password: string) =
    let mutable res = ""
    if (userIdSet.Contains id) then
        res <- "user already exists"
    else 
        userIdSet <- userIdSet.Add(id)
        userIdMap <- userIdMap.Add(id,password)
        onlineUsersMap <- onlineUsersMap.Add(id,true)
        mentionMap <- mentionMap.Add(id, [])
        followingMap <- followingMap.Add(id, List.empty)
        followersMap <- followersMap.Add(id, List.empty)
        pendingMap <- pendingMap.Add(id, [])
        feedMap <- feedMap.Add(id, [])
        res <- "registration is successful"
    res

let ActivateUser (userId: string) (newStatus: bool) =
    onlineUsersMap <- onlineUsersMap.Add(userId, newStatus)

let isUserLoggedIn (userId: string) =
    let mutable res = false
    if(onlineUsersMap.ContainsKey userId) then
        if(onlineUsersMap.Item userId) then
            res <- true
    res

let addUserAsFollower (user1: string) (user2: string) = 
    let mutable temp = followersMap.Item user2
    temp <- temp @ [ user1 ]
    followersMap <- followersMap.Add(user2, temp)

let addUserAsFollowing (user1: string) (user2: string) = 
    let mutable temp1 = followingMap.Item user2
    temp1 <- temp1 @ [ user1 ]
    followingMap <- followingMap.Add(user2, temp1)


let addFollowerAndLeader (userId: string) (leaderId: string) =
    // check if both users exist
    if((userIdSet.Contains(userId)) && (userIdSet.Contains(leaderId))) then
        // userId added into leader's followers
        addUserAsFollower userId leaderId
        // Adding leaderId into userId's following list
        addUserAsFollowing leaderId userId
        true
    else 
        false


let getEntityList (tweet: string) (symbol: char) = 
    let arr = tweet.Split(' ') |> Array.toList
    let isEntity  (str: string) = (str.[0] = symbol)
    if symbol.Equals('@') then
        let mentionList =
            List.choose (fun elem ->
                let x = String.length elem
                match elem with
                | elem when isEntity elem -> Some(elem.[1..x - 1])
                | _ -> None) arr
        mentionList
    else
        let hashTagsList =
            List.choose (fun elem ->
                match elem with
                | elem when isEntity elem -> Some(elem)
                | _ -> None) arr
        hashTagsList

let getHashTags (tweet: string) =
     let Hts = getEntityList tweet '#'
     Hts

let getMentions (tweet: string) =
    let mentions = getEntityList tweet '@'
    mentions


let addRetweetToFeed (tweet: string) (uid: string) (follower: string) =
        if(tweet.StartsWith "Retweetedby") then
            //let currentStatus = onlineUsersMap.Item follower
            let arr = tweet.Split('|') |> Array.toList
            let currentToUserFeed = pendingMap.Item follower
            let hybridTweet = "Retweetedby:" + uid + "|" + arr.[1]
            let newToUserFeed =  [ hybridTweet ] @ currentToUserFeed
            pendingMap <- pendingMap.Add(follower, newToUserFeed)
            let currentToUserFeed1 = feedMap.Item follower
            let newToUserFeed1 =  [ hybridTweet ] @ currentToUserFeed1
            feedMap <- feedMap.Add(follower, newToUserFeed)
        else
            let currentToUserFeed = pendingMap.Item follower
            let hybridTweet = "Retweetedby:" + uid + "|" + tweet
            let newToUserFeed =  [ hybridTweet ] @ currentToUserFeed
            pendingMap <- pendingMap.Add(follower, newToUserFeed)
            let currentToUserFeed1 = feedMap.Item follower
            let newToUserFeed1 =  [ hybridTweet ] @ currentToUserFeed1
            feedMap <- feedMap.Add(follower, newToUserFeed)

let addTweetToFeed (tweet: string) (uid: string) (follower: string) =
        //let currentStatus = onlineUsersMap.Item follower
        let currentToUserFeed = pendingMap.Item follower
        let hybridTweet = "author:" + uid + " = " + tweet
        let newToUserFeed =  [ hybridTweet ] @ currentToUserFeed
        pendingMap <- pendingMap.Add(follower, newToUserFeed)
        let currentToUserFeed1 = feedMap.Item follower
        let newToUserFeed1 =  [ hybridTweet ] @ currentToUserFeed1
        feedMap <- feedMap.Add(follower, newToUserFeed1)

let pushFeedToUser (username: string) =

    let pendingFeed = pendingMap.Item username
    let newEmptyFeed = []
    pendingMap <- pendingMap.Add(username, newEmptyFeed)
    pendingFeed


let registerNewUser (data: RegisterMsg) : ApiResult<string> =
    let code = data.code
    let username = data.uid
    let pwd = data.password
    let mutable res = ""
    printfn "%s ::: %s" username pwd
    if(code = 0) then
        res <- registerUser username pwd
    else if(code = 1) then
        res <- "login failed"
        let mutable b = true
        if(userIdMap.ContainsKey username) then
            let pword = userIdMap.Item username
            if (pword = pwd) then
                ActivateUser  username true
                res <- "login success"
                (*res <- ""
                let pfeed = pushFeedToUser username
                for pf in pfeed do
                    res <- res + "***" + pf*)
    else
        ActivateUser username false
        res <- "successfully logged out"
    Ok res

let sendTweet (data: TweetMsg) : ApiResult<string> =
    let userid = data.uid
    let tweet = data.tweet
    let hashTags = getHashTags tweet
    //saveHashTags hashTags tweet
    for hashtag in hashTags do
        printfn "%s" hashtag
        if hashTagsMap.ContainsKey hashtag then
            let currentTweetList = hashTagsMap.Item hashtag
            let tweetList =  [ tweet ] @ currentTweetList
            hashTagsMap <- hashTagsMap.Add(hashtag, tweetList)
        else
            let tweetList = [ tweet ]
            hashTagsMap <- hashTagsMap.Add(hashtag, tweetList)
    
    let mentions = getMentions tweet
    //printfn "mentions: %A" mentions
    //saveMentions mentions tweet userid
    for mention in mentions do
        printfn "%s" mention
        //sendTweetToUser tweet senderUser oneMention
        if mentionMap.ContainsKey mention then
            let currentTweetList = mentionMap.Item mention
            let tweetList =  [ tweet ] @ currentTweetList
            mentionMap <- mentionMap.Add(mention, tweetList)
        else
            let tweetList = [ tweet ]
            mentionMap <- mentionMap.Add(mention, tweetList)

    let followersList = followersMap.Item userid
    for oneUser in followersList do
        // printfn "sending tweet to %A" oneUser 
        addTweetToFeed tweet userid oneUser
    Ok "success"

let sendRetweet (data: RetweetMsg) : ApiResult<string> =
    let userid = data.uid
    let tweet = data.tweet
    let followersList = followersMap.Item userid
    for oneUser in followersList do
        // printfn "sending tweet to %A" oneUser 
        addRetweetToFeed tweet userid oneUser
    Ok "success"


let subscribeUser (data: SubscribeMsg) : ApiResult<string> =
    let userid = data.uid
    let pid = data.sid
    addFollowerAndLeader userid pid |> ignore
    Ok "success"


let queryHashTag (hashtags: string) : ApiResult<ResultMsg> =
    let hashtag = "#"+hashtags
    let mutable res = ""
    if (hashTagsMap.ContainsKey hashtag) then
        let hashtagTweets = hashTagsMap.Item hashtag
        for ht in hashtagTweets do
            printfn "%s" ht
            res <- res + "***" + ht
    else
        res <- "no such hashtags"
    printfn "%s" res
    Ok { msg = res }


let queryMention (mentionedUser: string) : ApiResult<ResultMsg> =
    //let mentionedUser = data.men
    //let mentionedUser = "@"+mentionedUsers
    let mutable res = ""
    if (mentionMap.ContainsKey mentionedUser) then
        let mentionedList = mentionMap.Item mentionedUser
        for men in mentionedList do
            res <- res + "***" + men
    else
        res <- "No mentions"
    Ok { msg = res }
    
let queryFeed (feedUser: string) : ApiResult<ResultMsg> =
    //let mentionedUser = data.men
    //let mentionedUser = "@"+mentionedUsers
    let mutable res = ""
    if (feedMap.ContainsKey feedUser) then
        let pendingFeed = feedMap.Item feedUser
        for pf in pendingFeed do
            res <- res + "***" + pf
    else
        res <- "No feed"
    if(res = "") then
        res <- "No feed"
    Ok { msg = res }

let pendingFeed (feedUser: string) : ApiResult<ResultMsg> =
    //let mentionedUser = data.men
    //let mentionedUser = "@"+mentionedUsers
    let mutable res = ""
    if (pendingMap.ContainsKey feedUser) then
        let pendingFeed = pushFeedToUser feedUser
        for pf in pendingFeed do
            res <- res + "***" + pf
    else
        res <- "No feed"
    if(res = "") then
        res <- "No feed"
    Ok { msg = res }



/// This module implements the back-end of the application.
/// It's a CRUD application maintaining a basic in-memory database of people.

/// The server side website, tying everything together.
module Site =
    open WebSharper.UI
    open WebSharper.UI.Html
    open WebSharper.UI.Server

    /// Helper function to convert our internal ApiResult type into WebSharper Content.
    let JsonContent (result: ApiResult<'T>) : Async<Content<WebAPIEndPoint>> =
        match result with
        | Ok value ->
            Content.Json value
        | Error (status, error) ->
            Content.Json error
            |> Content.SetStatus status
        |> Content.WithContentType "application/json"

    /// Respond to an ApiEndPoint by calling the corresponding backend function
    /// and converting the result into Content.
    let ApiContent (ep: ApiEndPoint) : Async<Content<WebAPIEndPoint>> =
        match ep with
        | RegAPI registerMsg ->
            JsonContent (registerNewUser registerMsg)
        | TweetAPI tweetMsg ->
            JsonContent (sendTweet tweetMsg)
        | RetweetAPI retweetMsg ->
            JsonContent (sendRetweet retweetMsg)
        | SubscribeAPI subscribeMsg ->
            JsonContent (subscribeUser subscribeMsg)
        | HashtagAPI ht ->
            JsonContent (queryHashTag ht)
        | MentionAPI men ->
            JsonContent (queryMention men)
        | FeedAPI feed ->
            JsonContent (queryFeed feed)
        | PendingAPI pending ->
            JsonContent (pendingFeed pending)

    /// A simple HTML home page.
    let HomePage (ctx: Context<WebAPIEndPoint>) : Async<Content<WebAPIEndPoint>> =
        // Type-safely creates the URI: "/api/people/1"
        let person1Link = ctx.Link (Api (Cors.Of (HashtagAPI "#test")))
        Content.Page(
            Body = [
                p [] [text "API is running."]
                p [] [
                    text "Try querying: "
                    a [attr.href person1Link] [text person1Link]
                ]
            ]
        )

    /// The Sitelet parses requests into EndPoint values
    /// and dispatches them to the content function.
    let Main corsAllowedOrigins : Sitelet<WebAPIEndPoint> =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | Index -> HomePage ctx
            | Api api ->
                Content.Cors api (fun allows ->
                    { allows with
                        Origins = corsAllowedOrigins
                        Headers = ["Content-Type"]
                    }
                ) ApiContent
        )
