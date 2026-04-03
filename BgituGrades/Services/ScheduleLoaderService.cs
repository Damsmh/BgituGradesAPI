using System.Diagnostics;

namespace BgituGrades.Services
{
    public interface IScheduleLoaderService
    {
        Task<bool> RunAsync(string apiKey, CancellationToken cancellationToken);
    }
    public class ScheduleLoaderService(IConfiguration config, ILogger<IScheduleLoaderService> logger) : IScheduleLoaderService
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<IScheduleLoaderService> _logger = logger;

        public async Task<bool> RunAsync(string apiKey, CancellationToken cancellationToken)
        {
            var loaderPath = _config["ScheduleLoader:ExecutablePath"];
            if (string.IsNullOrEmpty(loaderPath) || !File.Exists(loaderPath))
            {
                _logger.LogError("Loader executable not found at path: {LoaderPath}", loaderPath);
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
                    ["GRADES_API_KEY"] = apiKey
                }
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
    }
}
