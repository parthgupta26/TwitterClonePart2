# COP5615 Distributed Operating System Principles Project 4 Part2

## Twitter-Clone-Actor-Model-F#-Akka.Net

## Project Description

Use WebSharper web framework to implement a WebSocket interface to your part I implementation. That means that, even though the F#  implementation (Part I) you could use AKKA messaging to allow client-server implementation, you now need to design and use a proper WebSocket interface. Specifically: 

- You need to design a JSON based API that  represents all messages and their replies (including errors)
- You need to re-write parts of your engine using WebSharper to implement the WebSocket interface
- You need to re-write parts of your client to use WebSockets.

### Implement a Twitter-like engine with the following functionality:

- Register account
- Send tweet. Tweets can have hashtags (e.g. #COP5615isgreat) and mentions (@bestuser).
- Subscribe to user's tweets.
- Re-tweets (so that your subscribers get an interesting tweet you got by other means).
- Allow querying tweets subscribed to, tweets with specific hashtags, tweets in which the user is mentioned (my mentions).
- If the user is connected, deliver the above types of tweets live (without querying).

## Submitted By:

Name: Parth Gupta, UFID: 91997064 & Mayank Garg, UFID: 59919115

## Instructions on How to Run the Code

- Download the code from canvas submission.
- After going to the folder where the file is downloaded, now got the the folder named TwitterServerProject twice inorder to run the server file. First run the command "dotnet run". After running the command server will now be active and will run on localhost on port : 5000.
- After that move back one folder in order to run the client file. Now open differnet command promts and run the command "dotnet fsi Fclient.fsx" inorder to activate client.

## Implementation

- We are implementing the REST API using websharper.
- It contains GET and POST requests which include : "/hashtag", "/mention", "/feed" and "/pending" and "/register", "/tweet", "/retweet" and "/subscribe" respectievely.
- All these are of the format "http://localhost:5000/api/feed/".
- If the server gets the request, the work will be delegated to other functions.
- We get the responses from the client side as JSON.
- JSON can be parsed in a straightforward manner for GET requests.
- For POST requests, the response is present in the JSON body. After that, it is converted to a string, which is displayed to the user.
- We are using a separate actor to get live updates of the feed. It continuously polls the feed at regular intervals to get updates.

## Built On

- Programming language: F# 
- Framework: AKKA.NET
- Operating System: Windows 10
- Programming Tool: Visual Studio Code