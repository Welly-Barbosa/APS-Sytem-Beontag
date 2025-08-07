using APSSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration; // Adicionar este using
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        // Lê os caminhos do appsettings.json
        _gamsExecutablePath = configuration.GetValue<string>("GamsSettings:ExecutablePath");
        _workspaceRootPath = configuration.GetValue<string>("GamsSettings:WorkspaceRootPath");
    }

    public async Task<GamsExecutionResult> ExecutarAsync(string gamsModelPath, GamsInputData inputData, TimeSpan timeout)
    {
        string jobFolderName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Guid.NewGuid().ToString().Substring(0, 8)}";
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
            process.StartInfo.Arguments = $"\"{localModelPath}\" INTERRUPT=1";
            process.StartInfo.WorkingDirectory = jobFolderPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            var processExitedTcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => processExitedTcs.SetResult(true);

            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(processExitedTcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout atingido
                string interruptFilePath = Path.Combine(jobFolderPath, "gamsintr.txt");
                await File.WriteAllTextAsync(interruptFilePath, "stop");
                await processExitedTcs.Task; // Aguarda o processo terminar após a interrupção
            }

            if (process.ExitCode != 0)
            {
                string errorOutput = await process.StandardError.ReadToEndAsync();
                return new GamsExecutionResult(false, jobFolderPath, $"GAMS process exited with code {process.ExitCode}. Error: {errorOutput}");
            }

            return new GamsExecutionResult(true, jobFolderPath);
        }
        catch (Exception ex)
        {
            return new GamsExecutionResult(false, jobFolderPath, ex.Message);
        }
    }
}