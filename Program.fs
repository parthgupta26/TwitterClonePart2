namespace PeopleApi

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore

type APIController(config: IConfiguration) =
    inherit SiteletService<App.Core.WebAPIEndPoint>()

    let corsAllowedOrigins =
        config.GetSection("allowedOrigins").AsEnumerable()
        |> Seq.map (fun kv -> kv.Value)
        |> List.ofSeq

    override val Sitelet = App.Site.Main corsAllowedOrigins

type BeginServer() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddSitelet<APIController>()
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        app.UseStaticFiles()
            .UseWebSharper()
            .Run(fun context ->
                context.Response.StatusCode <- 404
                context.Response.WriteAsync("Uh on ! Can't step into the Land of Unknowns"))

module Program =

    [<EntryPoint>]
    let main args =
        printfn "Twitter Server Online"
        WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<BeginServer>()
            .Build()
            .Run()
        0
