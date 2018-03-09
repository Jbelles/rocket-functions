using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Threading.Tasks;

namespace Rocket.AzureFunctions
{
    public static class StellarCoreNode
    {

        [FunctionName("StellarCoreNode")]
        public static Task RunAsync([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, TraceWriter Log)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            var xd = Environment.CurrentDirectory;
            process.StartInfo.FileName = @"stellar-core.exe";
            //process.StartInfo.Arguments = @"Debug\stellar-core.cfg";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                Log.Info(process.StandardOutput.ReadLine());
            }
            process.WaitForExit();
            return null;
        }


    }

}
