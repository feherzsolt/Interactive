using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Interactive
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] wait = new[] { "o....", ".o...", "..o..", "...o.", "....o" };

            Func<Task<ScriptState<object>>, ScriptState> Execute = t =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();

                t.ContinueWith(_ => { cts.Cancel(); });

                CancellationToken cancellationToken = cts.Token;

                int index = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write("\r{0}", wait[index]);
                    index = (index + 1) % wait.Length;
                    try
                    {
                        Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).GetAwaiter().GetResult();
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }

                Console.Write("\r{0}\r", new string(' ', 20));

                return t.Result;
            };

            ScriptOptions scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithReferences("System");

            ScriptState scriptState = Execute(CSharpScript.RunAsync("using System;", scriptOptions));

            while (true)
            {
                Console.Write("? ");
                string line = Console.ReadLine();

                try
                {
                    if (line.StartsWith("#"))
                    {
                        if (line.StartsWith("#reference"))
                        {
                            scriptOptions = scriptOptions.WithReferences(line.Substring("#reference".Length).Trim());
                        }

                        if (line == "#quit")
                            break;

                        continue;
                    }

                    scriptState = Execute(scriptState.ContinueWithAsync(line, scriptOptions));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                if (scriptState.ReturnValue != null)
                    Console.WriteLine(scriptState.ReturnValue);
            }
        }
    }
}
