using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace StellarCoreNode
{
    public class Program
    {
        [FunctionName("StellarCoreNode")]
        public static async Task RunAsync([TimerTrigger("0 0/30 * * * *")] TimerInfo timer, TraceWriter log)
        {

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            var xd = Environment.CurrentDirectory;
            process.StartInfo.FileName = @"stellar-core.exe";
            //process.StartInfo.FileName = @"D:\home\site\wwwroot\StellarCoreNode\stellar-core.exe";
            //process.StartInfo.Arguments = @"Debug\stellar-core.cfg";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                log.Info(process.StandardOutput.ReadLine());
            }
            process.WaitForExit();
        }
    }
}