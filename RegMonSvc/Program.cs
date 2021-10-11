using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace RegMonSvc
{
    public static class Program
    {
        /// <summary>
        /// Helper for check if we are a command in the command line arguments
        /// </summary>
        private static bool HasCommand(string[] args, string command)
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(command))
            {
                return false;
            }
            return args.Any(a => string.Equals(a, command, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Run services in interactive mode
        /// </summary>
        private static void RunInteractiveServices(ServiceBase[] servicesToRun)
        {
            Console.WriteLine();
            Console.WriteLine("Start the services in interactive mode.");
            Console.WriteLine();

            // Get the method to invoke on each service to start it
            var onStartMethod = typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);

            // Start services loop
            foreach (var service in servicesToRun)
            {
                Console.Write("Starting {0} ... ", service.ServiceName);
                onStartMethod.Invoke(service, new object[] {new string[] {}});
                Console.WriteLine("Started");
            }

            // Waiting the end
            Console.WriteLine();
            Console.WriteLine("Press a key to stop services...");
            Console.ReadKey();
            Console.WriteLine();

            // Get the method to invoke on each service to stop it
            var onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

            // Stop loop
            foreach (var service in servicesToRun)
            {
                Console.Write("Stopping {0} ... ", service.ServiceName);
                onStopMethod.Invoke(service, null);
                Console.WriteLine("Stopped");
            }

            Console.WriteLine();
            Console.WriteLine("All services are stopped.");

            // Waiting a key press to not return to VS directly
            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("=== Press a key to quit ===");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RegMonSvc()
            };

            // In interactive mode ?
            if (!Environment.UserInteractive)
            {
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // In debug mode ?
                if (Debugger.IsAttached)
                {
                    // Simulate the services execution
                    RunInteractiveServices(ServicesToRun);
                }
                else
                {
                    try
                    {
                        var hasCommands = false;
                        // Having command to run the services in interactive mode ?
                        if (HasCommand(args, "run-services"))
                        {
                            RunInteractiveServices(ServicesToRun);
                            // We don't process other commands
                            return;
                        }

                        // Having command to install and start the services ?
                        if (HasCommand(args, "start-services"))
                        {
                            // Install
                            ManagedInstallerClass.InstallHelper(new[] {typeof(Program).Assembly.Location});
                            // Start
                            foreach (var service in ServicesToRun)
                            {
                                var sc = new ServiceController(service.ServiceName);
                                sc.Start();
                                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                            }

                            hasCommands = true;
                        }

                        // Having a command to stop and uninstall the services ?
                        if (HasCommand(args, "stop-services"))
                        {
                            // Stop
                            foreach (var service in ServicesToRun)
                            {
                                var sc = new ServiceController(service.ServiceName);
                                sc.Stop();
                                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                            }

                            // Uninstall
                            ManagedInstallerClass.InstallHelper(new[] {"/u", typeof(Program).Assembly.Location});
                            hasCommands = true;
                        }

                        // Having an install command ?
                        if (HasCommand(args, "install"))
                        {
                            ManagedInstallerClass.InstallHelper(new[] {typeof(Program).Assembly.Location});
                            hasCommands = true;
                        }

                        // Having a start command ?
                        if (HasCommand(args, "start"))
                        {
                            foreach (var service in ServicesToRun)
                            {
                                var sc = new ServiceController(service.ServiceName);
                                sc.Start();
                                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                            }

                            hasCommands = true;
                        }

                        // Having a stop command ?
                        if (HasCommand(args, "stop"))
                        {
                            foreach (var service in ServicesToRun)
                            {
                                var sc = new ServiceController(service.ServiceName);
                                sc.Stop();
                                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                            }

                            hasCommands = false;
                        }

                        // Having an uninstall command ?
                        if (HasCommand(args, "uninstall"))
                        {
                            ManagedInstallerClass.InstallHelper(new[] {"/u", typeof(Program).Assembly.Location});
                            hasCommands = true;
                        }

                        // If we don't have commands we print usage message
                        if (!hasCommands)
                        {
                            Console.WriteLine("Usage : {0} [command] [command ...]", Environment.GetCommandLineArgs());
                            Console.WriteLine("Commands : ");
                            Console.WriteLine(" - install : Install the services");
                            Console.WriteLine(" - uninstall : Uninstall the services");
                            Console.WriteLine(" - start : Start the services");
                            Console.WriteLine(" - stop : Stop the services");
                            Console.WriteLine(" - start-services : Install and start the services");
                            Console.WriteLine(" - stop-services : Stop and uninstall the services");
                            Console.WriteLine(" - run-services : Run the services in interactive mode");
                        }
                    }
                    catch (Exception ex)
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error : {ex.GetBaseException().Message}");
                        Console.ForegroundColor = oldColor;
                    }
                }
            }
        }
    }
}