﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Dapper;
using Dapper.Contrib.Extensions;
using Nancy;
using Nancy.Hosting.Self;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Optimization.Engine.Data;
using QuantConnect.Logging;
using QuantConnect.Util;
using QuantConnect.Configuration;
using QuantConnect.Optimization.Engine.Web;
using QuantConnect.Queues;

namespace QuantConnect.Optimization.Engine
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Options:");
            Console.WriteLine("\t1) Run optimization");
            Console.WriteLine("\t2) View results");
            Console.Write("\n\nChoose operation (1 is default):");
            var option = Console.Read();

            if (option == 13 || option == 49)
            {
                RunOptimization();  
            }

            if (option == 50)
            {
                StartNancy();
            }
        }

        static void RunOptimization()
        {
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            Log.DebuggingEnabled = Config.GetBool("debug-mode");

            //create database
            var opzDataFolder = Path.Combine(Config.Get("data-folder"), "optimization");
            if (!Directory.Exists(opzDataFolder))
            {
                Directory.CreateDirectory(opzDataFolder);
            }
            var dbFile = Path.Combine(opzDataFolder, string.Format("{0}-{1:yyyyMMddHHmmssfff}.sqlite", Config.Get("algorithm-type-name"), DateTime.Now));
            var db = new Database(dbFile);

            var totalResult = new OptimizationTotalResult(db);

            foreach (var permutation in GetPermutations())
            {
                // init system nahdlers
                var leanEngineSystemHandlers = new LeanEngineSystemHandlers(new JobQueue(), new Api.Api(), new Messaging.Messaging()); //LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
                leanEngineSystemHandlers.Initialize();

                // init new result instaqance
                var permutationResult = new OptimizationResultHandler();

                // init algorithm handlers
                var leanEngineAlgorithmHandlers = new LeanEngineAlgorithmHandlers(
                    permutationResult,
                    new OptimizationSetupHandler(permutation),
                    new FileSystemDataFeed(), 
                    new BacktestingTransactionHandler(), 
                    new BacktestingRealTimeHandler(), 
                    new SubscriptionDataReaderHistoryProvider(),
                    new EmptyCommandQueueHandler()
                );

                // queue a job
                string assemblyPath;
                var job = leanEngineSystemHandlers.JobQueue.NextJob(out assemblyPath);

                // run engine 
                var engine = new Lean.Engine.Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, false);
                engine.Run(job, assemblyPath);

                // save result
                totalResult.AddPermutationResult(permutation, permutationResult);

                // dispose
                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }

            // save final results
            totalResult.SendFinalResults();
        }

        static IEnumerable<Dictionary<string, Tuple<Type, object>>> GetPermutations()
        {
            var path = Config.Get("algorithm-location", "QuantConnect.Algorithm.CSharp.dll");
            var lang = (Language)Enum.Parse(typeof(Language), Config.Get("algorithm-language"));
            var leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            var algo = leanEngineAlgorithmHandlers.Setup.CreateAlgorithmInstance(path, lang);
            return QCAlgorithm.ExtractPermutations(algo);
        }

        static void StartNancy()
        {
            // may need to run this comand in cmd (admin) 
            // netsh http add urlacl url=http://+:8888/ user=Everyone

            var hostConfigs = new HostConfiguration {UrlReservations = {CreateAutomatically = true}};

            using (var nancyHost = new NancyHost(new Uri("http://localhost:8888/"), new Bootstrapper(), hostConfigs))
            {
                nancyHost.Start();

                Console.WriteLine("Nancy now listening - navigating to http://localhost:8888/. Press enter to stop");
                try
                {
                    Process.Start("http://localhost:8888/");
                }
                catch (Exception)
                {
                }
                Console.ReadKey();
            }

            Console.WriteLine("Stopped. Good bye!");
        }
    }
}