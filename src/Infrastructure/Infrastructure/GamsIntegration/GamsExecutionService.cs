using APSSystem.Application.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading; // Adicionar este using para CancellationToken
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.GamsIntegration;

public class GamsExecutionService : IGamsExecutionService
{
    private readonly IGamsFileWriter _gamsWriter;

    // Caminhos que viriam de um arquivo de configuração
    private const string GamsExecutablePath = @"C:\GAMS\50\gams.exe";
    private const string WorkspaceRootPath = @"C:\APSSystem_Workspace";

    public GamsExecutionService(IGamsFileWriter gamsWriter)
    {
        _gamsWriter = gamsWriter;
    }

    public async Task<GamsExecutionResult> ExecutarAsync(string gamsModelPath, GamsInputData inputData, TimeSpan timeout)
    {
        string jobFolderName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        string jobFolderPath = Path.Combine(WorkspaceRootPath, jobFolderName);

        try
        {
            Directory.CreateDirectory(jobFolderPath);

            string modelFileName = Path.GetFileName(gamsModelPath);
            string localModelPath = Path.Combine(jobFolderPath, modelFileName);
            File.Copy(gamsModelPath, localModelPath);

            string inputDataPath = Path.Combine(jobFolderPath, "GamsInputData.dat");
            await _gamsWriter.GerarArquivoDeEntradaAsync(inputDataPath, inputData);

            using var process = new Process();
            process.StartInfo.FileName = GamsExecutablePath;
            process.StartInfo.Arguments = $"\"{localModelPath}\" INTERRUPT=1";
            process.StartInfo.WorkingDirectory = jobFolderPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            Console.WriteLine($"GAMS process started with ID: {process.Id}. Working directory: {jobFolderPath}");

            // --- CÓDIGO DE TIMEOUT ATUALIZADO ---
            try
            {
                // 1. Cria um CancellationTokenSource que será cancelado após o timeout
                using var cts = new CancellationTokenSource(timeout);

                // 2. Aguarda o processo terminar, passando o token
                await process.WaitForExitAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
                // 3. Se a tarefa foi cancelada, significa que o timeout foi atingido
                Console.WriteLine($"GAMS timeout of {timeout.TotalMinutes} minutes reached. Attempting graceful interrupt...");
                string interruptFilePath = Path.Combine(jobFolderPath, "gamsintr.txt");
                await File.WriteAllTextAsync(interruptFilePath, "stop");

                // Aguarda um tempo fixo para o GAMS finalizar a escrita dos resultados
                await process.WaitForExitAsync(); // Sem timeout, aguarda o quanto for necessário
            }

            if (process.ExitCode != 0)
            {
                return new GamsExecutionResult(false, jobFolderPath, $"GAMS process exited with code {process.ExitCode}.");
            }

            return new GamsExecutionResult(true, jobFolderPath);
        }
        catch (Exception ex)
        {
            return new GamsExecutionResult(false, jobFolderPath, ex.Message);
        }
    }
}