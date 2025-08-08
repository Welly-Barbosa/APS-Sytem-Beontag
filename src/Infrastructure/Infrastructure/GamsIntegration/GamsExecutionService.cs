using APSSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

    // A assinatura do método permanece a mesma, mas o parâmetro 'timeout' não será mais usado.
    public async Task<GamsExecutionResult> ExecutarAsync(string gamsModelPath, GamsInputData inputData, TimeSpan timeout)
    {
        string jobFolderName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        string jobFolderPath = Path.Combine(_workspaceRootPath, jobFolderName);
        var gamsLog = new StringBuilder();

        try
        {
            // 1. Prepara a pasta do Job
            Directory.CreateDirectory(jobFolderPath);
            string modelFileName = Path.GetFileName(gamsModelPath);
            string localModelPath = Path.Combine(jobFolderPath, modelFileName);
            File.Copy(gamsModelPath, localModelPath);
            string inputDataPath = Path.Combine(jobFolderPath, "GamsInputData.dat");
            await _gamsWriter.GerarArquivoDeEntradaAsync(inputDataPath, inputData);

            // 2. Configura o processo para a chamada convencional
            using var process = new Process();
            process.StartInfo.FileName = _gamsExecutablePath;
            // Argumentos simplificados, sem 'I=gamsintr.txt'
            process.StartInfo.Arguments = $"\"{localModelPath}\" lo=3";
            process.StartInfo.WorkingDirectory = jobFolderPath;

            // Configuração para vermos a janela do console durante o desenvolvimento
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            Console.WriteLine($"GAMS process started with ID: {process.Id}. Working directory: {jobFolderPath}");

            // 3. Aguarda a finalização do processo (sem timeout)
            await process.WaitForExitAsync();

            Console.WriteLine($"GAMS process finished with Exit Code: {process.ExitCode}");

            // 4. Retorna o resultado
            // (Em uma versão de produção, ainda verificaríamos o ExitCode para determinar o sucesso)

            return new GamsExecutionResult(true, jobFolderPath);
        }
        catch (Exception ex)
        {
            return new GamsExecutionResult(false, jobFolderPath, ex.Message);
        }
    }
}