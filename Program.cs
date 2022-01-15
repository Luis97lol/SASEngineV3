using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.IO;
using System.Threading.Tasks;

namespace SASEngine
{
    class Program
    {
        private static Dictionary<string, ALCApp> listPlugins = new Dictionary<string, ALCApp>();
        private static string pluginspath = Directory.GetCurrentDirectory() + "\\Plugins";
        static void Main(string[] args)
        {
            string[] cmd = args;
            while (true)
            {
                if (cmd.Length > 0)
                {
                    if (cmd[0] == "help")
                    {
                        printHelp();
                    }
                    if (cmd[0] == "run" & cmd.Length == 2)
                    {
                        UnloadPlugin(cmd[1]);
                        LoadPlugin(cmd[1]);
                    }
                    if (cmd[0] == "stop")
                    {
                        if (cmd.Length > 1) {
                            if (!UnloadPlugin(cmd[1]))
                            {
                                Console.WriteLine(@"The plugin """ + cmd[1] + @""" was not found");
                            }
                        }
                        else
                        {
                            Console.WriteLine(@"<HELP>: The stop command must have the following syntax - ""stop <Plugin Type>""");
                        }
                    }
                    if (cmd[0] == "cls") Console.Clear();
                    if (cmd[0] == "exit") return;
                }
                cmd = Console.ReadLine().Trim().Split(' ');
            }
            
        }

        private static void printHelp()
        {
            string h = @"This is help document. These are the available commands you can execute:
    run <pluginName>    ->  Loads and Run the plugin app into an isolated evironment.
    stop <pluginName>   ->  Diposes and unload the plugin app and it's isolated environment.
    cls                 ->  Clear the console.
    exit                ->  Finishes the whole program execution.
    help                ->  Shows this help.";
            Console.WriteLine(h);
        }

        private static bool UnloadPlugin(string name)
        {
            bool existe = listPlugins.TryGetValue(name, out ALCApp appl);
            if (existe)
            {
                appl.app.Dispose();
                appl.app = null;
                appl.alc.Unload();
                appl.alc = null;
                appl = null;
                listPlugins.Remove(name);
                GC.Collect();
            }
            return existe;
        }

        private static void LoadPlugin(string name)
        {
            try
            {
                var plugin = Directory.GetDirectories(pluginspath, name);
                if (plugin.Length == 0) throw new Exception(String.Format(@"The plugin ""{0}"" was not found",name));
                AssemblyLoadContext alc = new AssemblyLoadContext(name, true);
                try
                {
                    foreach (var dep in Directory.GetFiles(plugin[0] + "\\dependencies", "*.dll"))
                    {
                        alc.LoadFromAssemblyPath(dep);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }

                Assembly asm = alc.LoadFromAssemblyPath(Directory.GetFiles(plugin[0], "*.dll")[0]);
                IPlugin app = Activator.CreateInstance(asm.GetTypes()[0]) as IPlugin;
                Task.Run(async () =>
                {
                    await app.start();
                });
                listPlugins.Add(name, new ALCApp(alc, app));
            }
            catch(Exception ex) { Console.WriteLine(ex); }
        }

        class ALCApp
        {
            public AssemblyLoadContext alc { get; set; }
            public IPlugin app { get; set; }

            public ALCApp(AssemblyLoadContext alc, IPlugin app)
            {
                this.alc = alc;
                this.app = app;
            }
        }
    }
}
