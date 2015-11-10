using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Configuration;
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


            for (int i = 0; i < 2; i++)
            {
                var leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
                leanEngineSystemHandlers.Initialize();

                var leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance); ;

                string assemblyPath;
                var job = leanEngineSystemHandlers.JobQueue.NextJob(out assemblyPath);

                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, false);
                engine.Run(job, assemblyPath);    

                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }
        }

    }
}
