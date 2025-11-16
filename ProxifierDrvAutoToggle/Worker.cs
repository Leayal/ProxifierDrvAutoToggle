using System.Diagnostics;
using WmiLight;
using Windows.Win32;
using System.Timers;

namespace ProxifierDrvAutoToggle
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private static readonly string DefaultPath_ProxifierInstallationBinaryPath = Path.GetFullPath(Path.Combine("Proxifier", "Proxifier.exe"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        private readonly System.Timers.Timer timer;

        public Worker(ILogger<Worker> logger)
        {
            this.timer = new System.Timers.Timer(TimeSpan.FromSeconds(5)) { AutoReset = false };
            this.timer.Elapsed += Timer_Elapsed;
            this.timer.Stop();
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (WmiConnection connection = new WmiConnection("root\\cimv2", new WmiConnectionOptions() { EnablePackageEncryption = true }))
            {
                using (WmiEventSubscription sub = connection.CreateEventSubscription("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'", this.EventSubscription_EventArrived))
                {
                    var procs = Process.GetProcessesByName("proxifier");
                    var buffer = new char[2048];
                    foreach (var proc in procs)
                    {
                        try
                        {
                            uint cap = 2047;
                            if (PInvoke.QueryFullProcessImageName(proc.SafeHandle, Windows.Win32.System.Threading.PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer.AsSpan(), ref cap).Value != 0)
                            {
                                var exe_path = new string(buffer, 0, (int)cap);
                                if (string.Equals(exe_path, DefaultPath_ProxifierInstallationBinaryPath, StringComparison.OrdinalIgnoreCase) || IsProxifierBinary(exe_path))
                                {
                                    OnProxifierFound(proc);
                                }
                                else
                                {
                                    proc.Dispose();
                                }
                            }
                            else
                            {
                                proc.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            proc.Dispose();
                        }
                    }
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(500, stoppingToken);
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        catch (Exception ex)
                        {
                            this._logger.LogError(ex, "Proxifier Driver Auto Toggle encountered an error: " + ex.Message);
                            Environment.Exit(2);
                        }
                    }
                }
            }
            this.timer.Dispose();
        }

        private void EventSubscription_EventArrived(WmiObject obj)
        {
            try
            {
                var win32_process = obj.GetPropertyValue<WmiObject>("TargetInstance");
                var exe_name = win32_process.GetPropertyValue<string>("Name");

                if (string.Equals(exe_name, "proxifier.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var exe_path = win32_process.GetPropertyValue<string>("ExecutablePath");

                    if (string.Equals(exe_path, DefaultPath_ProxifierInstallationBinaryPath, StringComparison.OrdinalIgnoreCase) || IsProxifierBinary(exe_path))
                    {
                        var exe_id = win32_process.GetPropertyValue<uint>("ProcessId");
                        var procId = unchecked((int)exe_id);
                        Process? proc = null;
                        try
                        {
                            proc = Process.GetProcessById(procId);
                            OnProxifierFound(proc);
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogError(ex, "Proxifier Driver Auto Toggle encountered an error while reading process info: " + ex.Message);
                            proc?.Dispose();
                        }
                    }
                }
            }
            finally
            {
                obj.Dispose();
            }
        }

        private void OnProxifierFound(Process proc)
        {
            proc.Exited += ProxifierProc_Exited;
            proc.EnableRaisingEvents = true;
            _ = proc.HasExited;
            this.timer.Stop();
            Process.Start(Program.STR_SC, "start ProxifierDrv")?.Dispose();
        }

        private void ProxifierProc_Exited(object? sender, EventArgs e)
        {
            if (sender is Process proc) proc.Dispose();
            this.timer.Start();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Process.Start(Program.STR_SC, "stop ProxifierDrv")?.Dispose();
        }

        private static bool IsProxifierBinary(string filename)
        {
            if (!File.Exists(filename)) return false;
            var fvi = FileVersionInfo.GetVersionInfo(filename);
            if (string.Equals(fvi.ProductName, "Proxifier Standard Edition") && (fvi.LegalCopyright?.Contains("VentoByte") ?? false)) return true;
            return false;
        }
    }
}
