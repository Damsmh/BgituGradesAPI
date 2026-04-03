using System.Diagnostics;

namespace BgituGrades.Services
{
    public interface IScheduleLoaderService
    {
        Task<bool> RunAsync(CancellationToken cancellationToken);
    }
    public class ScheduleLoaderService(IConfiguration config) : IScheduleLoaderService
    {
        private readonly IConfiguration _config = config;

        public async Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            var loaderPath = _config["ScheduleLoader:ExecutablePath"];
            if (string.IsNullOrEmpty(loaderPath) || !File.Exists(loaderPath))
            {
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = loaderPath,
                Arguments = "--headless",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment = {
                    ["GRADES_API_KEY"] = _config["ApiKey"] ?? ""
                }
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
    }
}
