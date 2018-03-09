using System;

namespace StellarCoreNode
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting the stellar-core.exe");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            var xd = Environment.CurrentDirectory;
            process.StartInfo.FileName = @"stellar-core.exe";
            //process.StartInfo.Arguments = @"Debug\stellar-core.cfg";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            Console.WriteLine("stellar-core.exe started...");
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                Console.WriteLine(process.StandardOutput.ReadLine());
            }
            process.WaitForExit();
            Console.WriteLine(process.StandardError.ReadLine());
            Console.WriteLine($"Exit Code: {process.ExitCode}");
            Console.WriteLine("stellar-core.exe terminated");
        }
    }
}