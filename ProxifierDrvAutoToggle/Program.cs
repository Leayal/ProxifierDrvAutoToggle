using System.Diagnostics;

namespace ProxifierDrvAutoToggle
{
    public class Program
    {
        internal static readonly string STR_SC = Path.GetFullPath("sc.exe", Environment.GetFolderPath(Environment.SpecialFolder.System));
        internal const string ServiceName = "Leayal_ProxifierDrvAutoToggle";
        internal const string ServiceName_ProxifierDrv = "ProxifierDrv";
        public static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                if (string.Equals(args[0], "install", StringComparison.OrdinalIgnoreCase))
                {
                    RunProcess(STR_SC, $"config {ServiceName_ProxifierDrv} start= demand");
                    RunProcess(STR_SC, new string[]
                    {
                        "create",
                        ServiceName,
                        "binpath=",
                        $"\"{Environment.ProcessPath}\" --service",
                        "start=",
                        "auto"
                    });
                    RunProcess(STR_SC, $"start \"{ServiceName}\"");

                    Console.WriteLine($"Service {ServiceName} has been installed successfully.");
                    return;
                }
                else if (string.Equals(args[0], "uninstall", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "remove", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "delete", StringComparison.OrdinalIgnoreCase))
                {
                    RunProcess(STR_SC, $"stop \"{ServiceName}\"");
                    RunProcess(STR_SC, $"delete \"{ServiceName}\"");
                    RunProcess(STR_SC, $"config {ServiceName_ProxifierDrv} start= auto");
                    Console.WriteLine($"Service {ServiceName} has been removed successfully.");
                    return;
                }
                else if (string.Equals(args[0], "--service", StringComparison.OrdinalIgnoreCase))
                {
                    IHost host = Host.CreateDefaultBuilder(args)
                      .ConfigureServices(services =>
                      {
                          services.AddHostedService<Worker>();
                      })
                      // Configure as a Windows Service
                      .UseWindowsService(options =>
                      {
                          options.ServiceName = "ProxifierDrvAutoToggle";
                      })
                      .Build();
                    host.Run();
                }
            }
            Console.WriteLine($"Use launch param \"install\" or \"uninstall\" to install or remove the service.");
        }

        internal static void RunProcess(string filename, string param)
        {
            var proc = new Process();
            proc.StartInfo.FileName = filename;
            proc.StartInfo.Arguments = param;
            proc.Start();
            proc.WaitForExit();
            proc.Dispose();
        }

        internal static void RunProcess(string filename, IEnumerable<string> param)
        {
            var proc = new Process();
            proc.StartInfo.FileName = filename;
            foreach (var p in param) proc.StartInfo.ArgumentList.Add(p);
            proc.Start();
            proc.WaitForExit();
            proc.Dispose();
        }
    }
}