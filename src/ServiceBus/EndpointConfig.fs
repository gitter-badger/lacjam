namespace Lacjam.ServiceBus 
    open System
    open System.IO
    open Autofac
    open NServiceBus
    open NServiceBus.Features
    open Lacjam.Core
    open Lacjam.Core.Utility
    open Lacjam.Core.Runtime
    open Lacjam.Core.Scheduling
    open Lacjam.Core.Jobs
    open Lacjam.Core.Runtime
    open Lacjam.Integration
    open Quartz
    open Quartz.Spi
    open Quartz.Impl
    open Autofac
    open NServiceBus.ObjectBuilder
    open NServiceBus.ObjectBuilder.Common

    module Startup = 
       

        type EndpointConfig() =
         
            
            do 
                (
                    let cb = new ContainerBuilder()
                    cb.Register(fun _ -> 
                                                                let fac = new StdSchedulerFactory()
                                                                fac.Initialize()
                                                                let scheduler = fac.GetScheduler()
                                                                scheduler.ListenerManager.AddSchedulerListener(new Scheduling.JobSchedulerListener(Lacjam.Core.Runtime.Ioc.Resolve<ILogWriter>(), Lacjam.Core.Runtime.Ioc.Resolve<IBus>()))
                                                                scheduler.Start()
                                                                scheduler
                                                            ).As<Quartz.IScheduler>().SingleInstance() |> ignore
                    cb.RegisterType<JobScheduler>().As<Scheduling.IJobScheduler>().SingleInstance() |> ignore
                    cb.Update(Ioc)
                )

            interface IConfigureThisEndpoint
            interface AsA_Server
            interface IWantCustomInitialization with
                member this.Init() = 
                     Configure.Transactions.Enable() |> ignore
                     Configure.Serialization.Json() |> ignore
                     Configure.ScaleOut(fun a-> a.UseSingleBrokerQueue() |> ignore) 
                     
                     try
                         Configure.With()
                            .DefineEndpointName("lacjam.servicebus")
                            .LicensePath((IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant(), "license.xml")))
                            .Log4Net()
                            .AutofacBuilder(Ioc)                   
                            .InMemorySagaPersister()
                            .InMemoryFaultManagement()
                            .InMemorySubscriptionStorage()
                            .UseInMemoryTimeoutPersister()  
                            .UseTransport<Msmq>()
                           // .DoNotCreateQueues()
                            .PurgeOnStartup(true)
                            .UnicastBus() |> ignore
                     with | ex -> printfn "%A" ex

         
//       type CustomInitialization() =
//          
//           interface IWantCustomInitialization with
//                member this.Init() = 
//                        Runtime.ContainerBuilder.Register(fun _ -> sf).As<Quartz.IScheduler>().SingleInstance() |> ignore
//                        Runtime.ContainerBuilder.RegisterType<JobScheduler>().As<Scheduling.IJobScheduler>().SingleInstance() |> ignore
//                        Runtime.ContainerBuilder.Update(Ioc)
//                        //Configure.Instance.Configurer.ConfigureComponent<IScheduler>(DependencyLifecycle.SingleInstance) |> ignore 
                          
      
       
       type ServiceBusStartUp() =     
            let log = Lacjam.Core.Runtime.Ioc.Resolve<ILogWriter>() 
            let bus = Ioc.Resolve<IBus>() 
            let js = Ioc.Resolve<IJobScheduler>()
                
            interface IWantToRunWhenBusStartsAndStops with
                member this.Start() = 
                    log.Write(Info("-- Service Bus Started --"))   
                    System.Net.ServicePointManager.ServerCertificateValidationCallback <- (fun _ _ _ _ -> true) //four underscores (and seven years ago?)                       
                    Configure.Instance.Configurer.ConfigureComponent<Quartz.IScheduler>(DependencyLifecycle.SingleInstance) |> ignore
                    
                    Schedule.Every(TimeSpan.FromMinutes(Convert.ToDouble(15))).Action(fun a->
                        try
                            log.Write(LogMessage.Debug("NSB -- Schedule.Every elapsed"))
                            let meta = js.Scheduler.GetMetaData()
                            log.Write(Debug("-- Quartz MetaData --"))
                            log.Write(Debug("NumberOfJobsExecuted : " + meta.NumberOfJobsExecuted.ToString()))
                            log.Write(Debug("Started : " + meta.Started.ToString()))
                            log.Write(Debug("RunningSince : " + meta.RunningSince.ToString()))
                            log.Write(Debug("SchedulerInstanceId : " + meta.SchedulerInstanceId.ToString()))
                            log.Write(Debug("ThreadPoolSize : " + meta.ThreadPoolSize.ToString()))
                            log.Write(Debug("ThreadPoolType : " + meta.ThreadPoolType.ToString()))                            
                          //  Lacjam.Integration.Jira.outputRoadmap()                                      
                        with 
                        | ex ->  log.Write(LogMessage.Error("Schedule ACTION startup:",ex, true)) 
                    )

                    try
                        let js = Ioc.Resolve<IJobScheduler>()
                        let startup = new StartupBatchJobs() :> IContainBatches
                        for bat in startup.Batches do
                            js.scheduleBatch(bat,BatchSchedule.Daily,new TimeSpan(5,30,0))
                        

                    with | ex -> log.Write(Error("EndpointConfig addJob batch1 dt", ex,true))
                    try
                        // schedule startup jobs
                        
                        log.Write(Info("EndpointConfig.Init :: SchedulerName = " + js.Scheduler.SchedulerName))
                        log.Write(Info("EndpointConfig.Init :: IsStarted = " + js.Scheduler.IsStarted.ToString()))
                        log.Write(Info("EndpointConfig.Init :: SchedulerInstanceId = " + js.Scheduler.SchedulerInstanceId.ToString()))
                        let suJobs = new StartupBatchJobs() :> IContainBatches
                        for batch in suJobs.Batches do
                            let startBatchJob = new BatchSubmitterJob() 
                            startBatchJob.Batch <- batch
                            bus.Send(startBatchJob) |> ignore
                        ()
                        
                    with | ex -> log.Write(Error("ServiceBusStartUp", ex,true))

                member this.Stop() = 
                    Lacjam.Core.Runtime.Ioc.Resolve<IScheduler>().Shutdown(true);    
                    log.Write(Info("-- Service Bus Stopped --"))
                    Ioc.Dispose()
                      

//            interface ISpecifyMessageHandlerOrdering  with
//                member x.SpecifyOrder(order)  = order.Specify(First<NServiceBus.Timeout.TimeoutMessageHandler>.Then<SagaMessageHandler>())
