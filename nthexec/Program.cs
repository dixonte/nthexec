using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace nthexec
{
    class Program
    {
        static int Main(string[] args)
        {
            var cmd = new RootCommand
            {
                new Option<int>(new string[] { "-n", "--num-execs" }, () => 5, "Number of executions required to trigger."),
                new Option<string>(new string[] { "-s", "--state-path" }, () => "/tmp/nthexec", "Path to the state file."),
                new Option<int>(new string[] { "-t", "--timeout" }, () => 1000, "Timeout in milliseconds."),
                new Option<bool>(new string[] { "-i", "--include-overflow" }, () => false, "With this option, executions past the --num-execs count also match."),
                new Option<bool>(new string[] { "-v", "--verbose" }, () => false)
            };

            cmd.Description = "Increments a count with every execution, returning true upon reaching the nth execution in a given timeout, and optionally on every subsequent execution within the timeout.";

            cmd.Handler = CommandHandler.Create<int, string, int, bool, bool>(RootCommand);

            return cmd.Invoke(args);
        }

        private static int RootCommand(int numExecs, string statePath, int timeout, bool includeOverflow, bool verbose)
        {
            var execCount = 0;

            var stateInfo = new FileInfo(statePath);
            var stateDirInfo = new DirectoryInfo(stateInfo.DirectoryName);

            if (!stateDirInfo.Exists)
                stateDirInfo.Create();

            if (stateInfo.Exists && DateTime.Now.Subtract(stateInfo.LastWriteTime) <= TimeSpan.FromMilliseconds(timeout))
            {
                using (var fs = File.OpenRead(statePath))
                using (var sr = new StreamReader(fs))
                {
                    int.TryParse(sr.ReadLine(), out execCount);
                }
            }

            execCount++;

            using (var fs = File.OpenWrite(statePath))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine(execCount.ToString());
            }

            if (verbose)
                Console.WriteLine($"nthexec: {execCount}");
            
            return (includeOverflow && execCount >= numExecs) || (!includeOverflow && execCount == numExecs) ? 0 : -1;
        }
    }
}
