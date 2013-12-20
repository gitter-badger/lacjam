﻿namespace Lacjam.Core


module JobHandlers =
    open System
    open Autofac
    open NServiceBus
    open NServiceBus.MessageInterfaces
    open Lacjam
    open Lacjam.Core
    open Lacjam.Core.Runtime
    open Lacjam.Core.Payloads
    open Lacjam.Core.Payloads.Jobs

    type JobResultHandler(logger:ILogWriter) =
        interface IHandleMessages<Lacjam.Core.Payloads.Jobs.JobResult> with      
            member this.Handle(jr) = 
                try
                     logger.Write(LogMessage.Debug(jr.ToString()))
                with | ex -> logger.Write(LogMessage.Error(jr.ToString(), ex, true))

    type SiteScraperHandler(logger:ILogWriter) =
        interface IHandleMessages<Lacjam.Core.Payloads.Jobs.SiteScraper> with      
            member this.Handle(sc) = 
                let job = (sc:>JobBase)
                logger.Write(LogMessage.Debug(job.CreateDate.ToString() + "   " + job.Name))
                let html = job.Execute
                let bus = Lacjam.Core.Runtime.Ioc.Resolve<IBus>()
                try
                    let rv = Jobs.SiteScraper("Bedlam", Some("http://www.bedlam.net.au"))
                    let html = rv.Execute
                    let jr = JobResult(rv.Id(),true,html)
                    bus.Reply(jr)
                with | ex -> logger.Write(LogMessage.Error(job.Name, ex, true))
                
                //Console.WriteLine(html)
                
                
    

