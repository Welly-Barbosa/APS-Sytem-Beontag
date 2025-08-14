using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace APSSystem.Presentation.WPF.Diagnostics
{
    /// <summary>
    /// Logger básico que escreve logs em %LOCALAPPDATA%\APSSystem\Logs\.
    /// </summary>
    public sealed class ExceptionLogger : IExceptionLogger
    {
        private readonly string logDir;
        private readonly string logFile;

        /// <summary>
        /// Cria uma instância do logger e prepara o arquivo do dia.
        /// </summary>
        public ExceptionLogger()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            logDir = Path.Combine(baseDir, "APSSystem", "Logs");
            Directory.CreateDirectory(logDir);
            logFile = Path.Combine(logDir, $"app-{DateTime.Now:yyyyMMdd-HHmmss}.log");

            // Também envia para a janela Output do VS
            Trace.Listeners.Add(new TextWriterTraceListener(logFile, "WpfFileTrace"));
            Trace.AutoFlush = true;
        }

        /// <inheritdoc />
        public string LogException(Exception ex, string? context = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== EXCEPTION =====");
            if (!string.IsNullOrWhiteSpace(context))
                sb.AppendLine($"Context: {context}");
            DumpException(ex, sb, 0);
            sb.AppendLine("=====================");
            Write(sb.ToString());
            return logFile;
        }

        /// <inheritdoc />
        public string LogInfo(string message)
        {
            Write($"[INFO {DateTime.Now:HH:mm:ss}] {message}");
            return logFile;
        }

        private static void DumpException(Exception ex, StringBuilder sb, int level)
        {
            var pad = new string(' ', level * 2);
            sb.AppendLine($"{pad}Type: {ex.GetType().FullName}");
            sb.AppendLine($"{pad}Message: {ex.Message}");
            sb.AppendLine($"{pad}StackTrace:");
            sb.AppendLine(ex.StackTrace ?? $"{pad}<no stacktrace>");
            if (ex.Data != null && ex.Data.Count > 0)
            {
                sb.AppendLine($"{pad}Data:");
                foreach (var key in ex.Data.Keys)
                    sb.AppendLine($"{pad}  {key}: {ex.Data[key]}");
            }
            if (ex.InnerException != null)
            {
                sb.AppendLine($"{pad}-- InnerException --");
                DumpException(ex.InnerException, sb, level + 1);
            }
        }

        private static void Write(string text)
        {
            try
            {
                Trace.WriteLine(text);
            }
            catch
            {
                // Evita exceções em cascata no logger
            }
        }
    }
}
