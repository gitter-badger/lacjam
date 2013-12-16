﻿namespace Lacjam.Core


module MessageHandlers =
    open System
    open Autofac
    open NServiceBus
    open NServiceBus.MessageInterfaces
    open Lacjam
    open Lacjam.Core
    open Lacjam.Core.Messages
    open Lacjam.Core.Jobs
    type BedlamPollHandler() =
         interface IHandleMessages<Lacjam.Core.Messages.BedlamPoll> with
              member this.Handle(bp) = 
                let result = bp.JobName
                let job:IAmAJob = new SiteScraper("Bedlam", "http://www.bedlam.net.au") :> IAmAJob
                let html = job.Execute
                let bus = Lacjam.Core.Ioc.Container.Resolve<IBus>()
                try
                    let rv = new SiteRetrieverResult()
                    rv.Html <- html
                    bus.Reply(rv)
                with | ex -> Console.WriteLine ex
                
                //Console.WriteLine(html)
                
                
    

