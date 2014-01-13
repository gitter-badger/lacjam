﻿namespace Lacjam.Integration

module Jobs =

    open System
    open System.ServiceModel
    open System.Linq
    open System.Runtime.Serialization.Json
    open System.Net.Mail;
    open System.Diagnostics
    open Microsoft.FSharp.Linq
    open Microsoft.FSharp.Data.TypeProviders
    open Newtonsoft.Json
    open HtmlAgilityPack
    open NServiceBus
    open NServiceBus.MessageInterfaces
    open NServiceBus.Mailer
    open log4net
    open Lacjam
    open Lacjam.Core
    open Lacjam.Core.Runtime
    open Lacjam.Core.User
    open Lacjam.Core.Domain
    open Lacjam.Core.Scheduler
    open Lacjam.Integration
    open Lacjam.Core.Scheduler.Jobs
    open Lacjam.Core.Utility.Html
    
    
    [<Serializable>]
    type JiraRoadMapOutputJob() =
        inherit Lacjam.Core.Scheduler.Jobs.Job()
        interface NServiceBus.IMessage


    type JiraRoadMapOutputJobHandler(log : ILogWriter) =
        interface NServiceBus.IHandleMessages<JiraRoadMapOutputJob> with
            member x.Handle(sc) =
                log.Write (LogMessage.Debug(sc.CreatedDate.ToString() + "   " + sc.GetType().ToString()))                    

                try
                    Lacjam.Integration.Jira.outputRoadmap()
                with ex -> log.Write(LogMessage.Error(sc.GetType().ToString(), ex, true)) //Console.WriteLine(html)


    [<Serializable>]
    type SwellNetRatingJob() =
        inherit Lacjam.Core.Scheduler.Jobs.Job()
        interface NServiceBus.IMessage

    type SwellNetRatingHandler(log : ILogWriter  ,  bus : IBus) =
        interface NServiceBus.IHandleMessages<SwellNetRatingJob> with
            member x.Handle(job) =
                log.Write (LogMessage.Debug(job.CreatedDate.ToString() + "   " + job.GetType().ToString()))                    
                log.Write (LogMessage.Debug(job.Payload))    
                try
                    let doc = new HtmlAgilityPack.HtmlDocument()
                    doc.OptionFixNestedTags<-true;
                    doc.LoadHtml(job.Payload)
                    match doc.ParseErrors with | s -> if (s.Count() > 0) then failwith ("Errors" + doc.ParseErrors.First().ToString()) |> ignore

                    let lastUpdatedSpan = doc.DocumentNode.Descendants().FirstOrDefault(fun d -> d.Attributes.Contains("class") && d.Attributes.Item("class").Value.Contains("views-field views-field-field-surf-report-date") )

                    let lastUpdated = Utility.Html.findNodesByClassName(lastUpdatedSpan, "field-content")  
                    let dt = lastUpdated.Value.OwnerNode.InnerText |> System.Convert.ToDateTime
                    log.Write(Debug(dt.ToString()))

                    if (dt.DayOfWeek = System.DateTime.Now.DayOfWeek) then
                        let (ratingSpan:HtmlNode) = doc.DocumentNode.Descendants().FirstOrDefault(fun d -> d.Attributes.Contains("class") && d.Attributes.Item("class").Value.Contains("views-field views-field-field-surf-report-rating") )
                        let rating = findNodesByClassName(ratingSpan, "field-content")
                        log.Write(Debug(rating.Value.OwnerNode.InnerHtml))
                        let al = new AddressList()
                        al.Add("chris@mckelt.com")
                        let mail = new Mail(
                                        To = al,
                                        
                                        From = "chris@mckelt.com",
                                        Body = "body", //rating.Value.OwnerNode.InnerHtml,
                                        Subject = "subject" //rating.Value.OwnerNode.InnerHtml
                                    )
                        bus.SendMail(mail)
                with ex -> log.Write(LogMessage.Error(job.GetType().ToString(), ex, true)) //Console.WriteLine(html)

