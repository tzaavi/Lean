using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine
{
    public class OptimizationEngine
    {
        public static void Main(string[] args)
        {
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            Log.DebuggingEnabled = Config.GetBool("debug-mode");

            var startTime = DateTime.Now;

            foreach (var permutation in GetPermutations())
            {
                // init system nahdlers
                var leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
                leanEngineSystemHandlers.Initialize();

                // init algorithm handlers
                var transactionHandlerTypeName = Config.Get("transaction-handler", "BacktestingTransactionHandler");
                var realTimeHandlerTypeName = Config.Get("real-time-handler", "BacktestingRealTimeHandler");
                var dataFeedHandlerTypeName = Config.Get("data-feed-handler", "FileSystemDataFeed");
                var historyProviderTypeName = Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider");

                var leanEngineAlgorithmHandlers = new LeanEngineAlgorithmHandlers(
                    new OptimizationResultHandler(startTime),
                    new OptimizationSetupHandler(permutation),
                    Composer.Instance.GetExportedValueByTypeName<IDataFeed>(dataFeedHandlerTypeName),
                    Composer.Instance.GetExportedValueByTypeName<ITransactionHandler>(transactionHandlerTypeName),
                    Composer.Instance.GetExportedValueByTypeName<IRealTimeHandler>(realTimeHandlerTypeName),
                    Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>(historyProviderTypeName)
                );
                

                // queue a job
                string assemblyPath;
                var job = leanEngineSystemHandlers.JobQueue.NextJob(out assemblyPath);

                // run engine 
                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, false);
                engine.Run(job, assemblyPath);    

                // dispose
                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }
        }

        private static IEnumerable<Dictionary<string, Tuple<Type, object>>> GetPermutations()
        {
            var path = Config.Get("algorithm-location", "QuantConnect.Algorithm.CSharp.dll");
            var lang = (Language) Enum.Parse(typeof (Language), Config.Get("algorithm-language"));
            var leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            var algo = leanEngineAlgorithmHandlers.Setup.CreateAlgorithmInstance(path, lang);
            return QCAlgorithm.ExtractPermutations(algo);
        }

    }
}
