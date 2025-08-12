using APSSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.GamsIntegration;

public class GamsExecutionService : IGamsExecutionService
{
    private readonly IGamsFileWriter _gamsWriter;
    private readonly string _gamsExecutablePath;
    private readonly string _workspaceRootPath;

    public GamsExecutionService(IGamsFileWriter gamsWriter, IConfiguration configuration)
    {
        _gamsWriter = gamsWriter;
        _gamsExecutablePath = configuration.GetValue<string>("GamsSettings:ExecutablePath")!;
        _workspaceRootPath = configuration.GetValue<string>("GamsSettings:WorkspaceRootPath")!;
    }

    public async Task<GamsExecutionResult> ExecutarAsync(string gamsModelPath, GamsInputData inputData, TimeSpan timeout)
    {
        string jobFolderName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        string jobFolderPath = Path.Combine(_workspaceRootPath, jobFolderName);

        try
        {
            Directory.CreateDirectory(jobFolderPath);
            string modelFileName = Path.GetFileName(gamsModelPath);
            string localModelPath = Path.Combine(jobFolderPath, modelFileName);
            File.Copy(gamsModelPath, localModelPath);
            string inputDataPath = Path.Combine(jobFolderPath, "GamsInputData.dat");
            await _gamsWriter.GerarArquivoDeEntradaAsync(inputDataPath, inputData);

            using var process = new Process();
            process.StartInfo.FileName = _gamsExecutablePath;
            process.StartInfo.Arguments = $"\"{localModelPath}\" lo=3";
            process.StartInfo.WorkingDirectory = jobFolderPath;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            Console.WriteLine($"GAMS process started. Working directory: {jobFolderPath}");

            await process.WaitForExitAsync();

            Console.WriteLine($"GAMS process finished with Exit Code: {process.ExitCode}");

            return new GamsExecutionResult(true, jobFolderPath);
        }
        catch (Exception ex)
        {
            return new GamsExecutionResult(false, jobFolderPath, ex.Message);
        }
    }
}