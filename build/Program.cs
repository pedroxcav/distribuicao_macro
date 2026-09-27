using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Ocorrências")]
[assembly: AssemblyDescription("Inicializador da planilha de ocorrências")]
[assembly: AssemblyCompany("Projeto de demonstração")]
[assembly: AssemblyProduct("Ocorrências")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

internal static class Program
{
    private const string ResourceName = "Ocorrencias.Payload.zip";

    [STAThread]
    private static void Main()
    {
        try
        {
            string appDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Ocorrencias");
            string workbook = Path.Combine(appDirectory, "consulta.xlsm");
            string packageMarker = Path.Combine(appDirectory, ".pacote.sha256");

            InstallPayloadIfNeeded(appDirectory, workbook, packageMarker);
            ApplyEnvironmentConfiguration(appDirectory);

            if (!File.Exists(workbook))
            {
                throw new FileNotFoundException(
                    "A planilha do pacote não foi encontrada.", workbook);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = workbook,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Não foi possível abrir a aplicação.\n\n" + exception.Message,
                "Ocorrências",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void InstallPayloadIfNeeded(
        string appDirectory,
        string workbook,
        string marker)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream resource = assembly.GetManifestResourceStream(ResourceName))
        {
            if (resource == null)
            {
                throw new InvalidOperationException(
                    "Os arquivos internos da aplicação não foram encontrados.");
            }

            byte[] payload;
            using (var buffer = new MemoryStream())
            {
                resource.CopyTo(buffer);
                payload = buffer.ToArray();
            }

            string payloadHash;
            using (SHA256 sha256 = SHA256.Create())
            {
                payloadHash = BitConverter.ToString(sha256.ComputeHash(payload))
                    .Replace("-", string.Empty);
            }

            if (File.Exists(workbook) &&
                File.Exists(marker) &&
                string.Equals(
                    File.ReadAllText(marker).Trim(),
                    payloadHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Directory.CreateDirectory(appDirectory);

            using (var payloadStream = new MemoryStream(payload, false))
            using (var archive = new ZipArchive(payloadStream, ZipArchiveMode.Read))
            {
                string root = Path.GetFullPath(appDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destination = Path.GetFullPath(
                        Path.Combine(appDirectory, entry.FullName));

                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(
                            "O pacote contém um caminho de arquivo inválido.");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    string parent = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    using (Stream input = entry.Open())
                    using (var output = new FileStream(
                        destination, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        input.CopyTo(output);
                    }
                }
            }

            File.WriteAllText(marker, payloadHash);
        }
    }

    private static void ApplyEnvironmentConfiguration(string appDirectory)
    {
        string configurationPath = Path.Combine(
            appDirectory, "configuracao", "ambiente.json");

        if (!File.Exists(configurationPath))
        {
            return;
        }

        var serializer = new JavaScriptSerializer();
        EnvironmentConfiguration configuration =
            serializer.Deserialize<EnvironmentConfiguration>(
                File.ReadAllText(configurationPath));

        if (configuration == null || !configuration.Habilitado)
        {
            return;
        }

        string completionMarker = Path.Combine(
            appDirectory, ".ambiente-configurado");

        if (File.Exists(completionMarker))
        {
            return;
        }

        if (!HasEnabledRules(configuration))
        {
            throw new InvalidDataException(
                "A preparação do ambiente foi habilitada sem nenhuma regra ativa.");
        }

        if (configuration.Arquivos != null)
        {
            foreach (FileRule rule in configuration.Arquivos)
            {
                if (rule != null && rule.Habilitado)
                {
                    ApplyFileRule(appDirectory, rule);
                }
            }
        }

        if (configuration.Variaveis != null)
        {
            foreach (EnvironmentVariableRule rule in configuration.Variaveis)
            {
                if (rule != null && rule.Habilitado)
                {
                    ApplyEnvironmentVariableRule(rule);
                }
            }
        }

        if (configuration.Python != null && configuration.Python.Habilitado)
        {
            PreparePython(appDirectory, configuration.Python);
        }

        File.WriteAllText(
            completionMarker,
            DateTime.Now.ToString("O"));
    }

    private static bool HasEnabledRules(EnvironmentConfiguration configuration)
    {
        if (configuration.Python != null && configuration.Python.Habilitado)
        {
            return true;
        }

        if (configuration.Variaveis != null)
        {
            foreach (EnvironmentVariableRule rule in configuration.Variaveis)
            {
                if (rule != null && rule.Habilitado)
                {
                    return true;
                }
            }
        }

        if (configuration.Arquivos != null)
        {
            foreach (FileRule rule in configuration.Arquivos)
            {
                if (rule != null && rule.Habilitado)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ApplyFileRule(string appDirectory, FileRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Origem) ||
            string.IsNullOrWhiteSpace(rule.Destino))
        {
            throw new InvalidDataException(
                "Uma regra de arquivo da configuração está incompleta.");
        }

        string root = Path.GetFullPath(appDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string source = Path.GetFullPath(Path.Combine(appDirectory, rule.Origem));

        if (!source.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "A configuração contém uma origem de arquivo inválida.");
        }

        if (!File.Exists(source))
        {
            throw new FileNotFoundException(
                "Um arquivo habilitado na configuração não foi encontrado.",
                source);
        }

        string destination = Environment.ExpandEnvironmentVariables(rule.Destino);
        destination = Path.GetFullPath(destination);

        string destinationDirectory = Path.GetDirectoryName(destination);
        if (string.IsNullOrEmpty(destinationDirectory))
        {
            throw new InvalidDataException(
                "Uma regra possui um destino de arquivo inválido.");
        }

        Directory.CreateDirectory(destinationDirectory);

        if (!File.Exists(destination))
        {
            File.Copy(source, destination, false);
        }
    }

    private static void ApplyEnvironmentVariableRule(EnvironmentVariableRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Nome) ||
            string.IsNullOrWhiteSpace(rule.Valor) ||
            rule.Valor.IndexOf("PREENCHER", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidDataException(
                "Uma variável de ambiente habilitada está incompleta.");
        }

        string configuredValue = Environment.ExpandEnvironmentVariables(rule.Valor);
        string currentValue = Environment.GetEnvironmentVariable(
            rule.Nome, EnvironmentVariableTarget.User);

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            Environment.SetEnvironmentVariable(
                rule.Nome,
                configuredValue,
                EnvironmentVariableTarget.User);
            currentValue = configuredValue;
        }

        Environment.SetEnvironmentVariable(
            rule.Nome,
            currentValue,
            EnvironmentVariableTarget.Process);
    }

    private static void PreparePython(
        string appDirectory,
        PythonConfiguration configuration)
    {
        string version = string.IsNullOrWhiteSpace(configuration.Versao)
            ? "3.12"
            : configuration.Versao.Trim();
        int timeoutMinutes = configuration.TimeoutMinutos > 0
            ? configuration.TimeoutMinutos
            : 30;
        string logDirectory = Path.Combine(appDirectory, "logs");
        string logPath = Path.Combine(logDirectory, "preparacao-python.log");
        var log = new StringBuilder();
        Directory.CreateDirectory(logDirectory);

        try
        {
            string basePython = DiscoverPython(version, log);
            string runtimePython = basePython;
            string scriptsDirectory;

            if (configuration.CriarAmbienteVirtual)
            {
                string virtualEnvironment = Path.Combine(
                    appDirectory, "runtime", ".venv");
                runtimePython = Path.Combine(
                    virtualEnvironment, "Scripts", "python.exe");

                if (!File.Exists(runtimePython))
                {
                    ProcessResult venvResult = RunProcess(
                        basePython,
                        "-m venv \"" + virtualEnvironment + "\"",
                        appDirectory,
                        timeoutMinutes);
                    AppendProcessLog(log, "Criação do ambiente virtual", venvResult);
                    EnsureSuccess(
                        venvResult,
                        "Não foi possível criar o ambiente virtual do projeto.");
                }

                scriptsDirectory = Path.Combine(virtualEnvironment, "Scripts");
            }
            else
            {
                scriptsDirectory = DiscoverScriptsDirectory(
                    runtimePython, appDirectory, timeoutMinutes, log);
            }

            if (!string.IsNullOrWhiteSpace(configuration.ComandoPip))
            {
                string pipCommand = ExpandConfigurationValue(
                    configuration.ComandoPip,
                    appDirectory);

                if (pipCommand.IndexOf(
                    "PREENCHER", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new InvalidDataException(
                        "O comando pip ainda possui um valor não preenchido.");
                }

                ProcessResult pipResult = RunProcess(
                    runtimePython,
                    pipCommand,
                    appDirectory,
                    timeoutMinutes);
                AppendProcessLog(log, "Instalação das bibliotecas", pipResult);
                EnsureSuccess(
                    pipResult,
                    "Não foi possível instalar as bibliotecas Python.");
            }

            if (configuration.InstalarKernelsGlue)
            {
                string kernelInstaller = FindKernelInstaller(scriptsDirectory);
                ProcessResult kernelResult = RunProcess(
                    kernelInstaller,
                    string.Empty,
                    appDirectory,
                    timeoutMinutes);
                AppendProcessLog(log, "Instalação dos kernels do Glue", kernelResult);
                EnsureSuccess(
                    kernelResult,
                    "Não foi possível registrar os kernels do AWS Glue.");
            }

            if (configuration.AdicionarAoPath)
            {
                var pathEntries = new List<string>();
                string basePythonDirectory = Path.GetDirectoryName(basePython);

                if (!string.IsNullOrWhiteSpace(basePythonDirectory))
                {
                    pathEntries.Add(basePythonDirectory);
                }

                pathEntries.Add(scriptsDirectory);
                AddUserPathEntries(pathEntries);
            }

            string runtimeDirectory = Path.Combine(appDirectory, "runtime");
            Directory.CreateDirectory(runtimeDirectory);
            File.WriteAllText(
                Path.Combine(runtimeDirectory, "python-executavel.txt"),
                runtimePython,
                Encoding.UTF8);

            log.AppendLine("Python base: " + basePython);
            log.AppendLine("Python da aplicação: " + runtimePython);
            log.AppendLine("Scripts: " + scriptsDirectory);
        }
        catch (Exception exception)
        {
            log.AppendLine("ERRO: " + exception);
            throw new InvalidOperationException(
                exception.Message + " Consulte o log em: " + logPath,
                exception);
        }
        finally
        {
            File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);
        }
    }

    private static string DiscoverPython(string version, StringBuilder log)
    {
        string probeCode =
            "import sys; print(str(sys.version_info.major)+'.'+" +
            "str(sys.version_info.minor)+'|'+sys.executable)";

        try
        {
            ProcessResult launcherResult = RunProcess(
                "py.exe",
                "-" + version + " -c \"" + probeCode + "\"",
                Environment.CurrentDirectory,
                2);
            AppendProcessLog(log, "Descoberta pelo Python Launcher", launcherResult);

            string launcherPython = ParsePythonProbe(launcherResult, version);
            if (!string.IsNullOrWhiteSpace(launcherPython))
            {
                return launcherPython;
            }
        }
        catch (Exception exception)
        {
            log.AppendLine("Python Launcher indisponível: " + exception.Message);
        }

        ProcessResult whereResult = RunProcess(
            "where.exe",
            "python.exe",
            Environment.CurrentDirectory,
            2);
        AppendProcessLog(log, "Fallback com where.exe", whereResult);

        if (whereResult.ExitCode == 0)
        {
            string[] candidates = whereResult.Output.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawCandidate in candidates)
            {
                string candidate = rawCandidate.Trim();
                if (!File.Exists(candidate))
                {
                    continue;
                }

                try
                {
                    ProcessResult candidateResult = RunProcess(
                        candidate,
                        "-c \"" + probeCode + "\"",
                        Environment.CurrentDirectory,
                        2);
                    AppendProcessLog(log, "Teste do Python " + candidate, candidateResult);

                    string discovered = ParsePythonProbe(candidateResult, version);
                    if (!string.IsNullOrWhiteSpace(discovered))
                    {
                        return discovered;
                    }
                }
                catch (Exception exception)
                {
                    log.AppendLine(
                        "Falha ao testar " + candidate + ": " + exception.Message);
                }
            }
        }

        throw new FileNotFoundException(
            "Não foi possível localizar automaticamente o Python " + version + ".");
    }

    private static string ParsePythonProbe(ProcessResult result, string version)
    {
        if (result.ExitCode != 0)
        {
            return null;
        }

        string[] lines = result.Output.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);

        for (int index = lines.Length - 1; index >= 0; index--)
        {
            string line = lines[index].Trim();
            int separator = line.IndexOf('|');

            if (separator <= 0)
            {
                continue;
            }

            string foundVersion = line.Substring(0, separator);
            string executable = line.Substring(separator + 1).Trim();

            if (string.Equals(
                    foundVersion,
                    version,
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(executable))
            {
                return executable;
            }
        }

        return null;
    }

    private static string DiscoverScriptsDirectory(
        string pythonExecutable,
        string workingDirectory,
        int timeoutMinutes,
        StringBuilder log)
    {
        ProcessResult result = RunProcess(
            pythonExecutable,
            "-c \"import sysconfig; print(sysconfig.get_path('scripts'))\"",
            workingDirectory,
            timeoutMinutes);
        AppendProcessLog(log, "Descoberta da pasta Scripts", result);
        EnsureSuccess(result, "Não foi possível localizar a pasta Scripts do Python.");

        string[] lines = result.Output.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            throw new InvalidDataException(
                "O Python não retornou o caminho da pasta Scripts.");
        }

        return lines[lines.Length - 1].Trim();
    }

    private static string FindKernelInstaller(string scriptsDirectory)
    {
        string[] candidates =
        {
            Path.Combine(scriptsDirectory, "install-glue-kernels.exe"),
            Path.Combine(scriptsDirectory, "install-glue-kernels")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "O comando install-glue-kernels não foi encontrado após a instalação.");
    }

    private static void AddUserPathEntries(List<string> configuredEntries)
    {
        string userPath = Environment.GetEnvironmentVariable(
            "Path", EnvironmentVariableTarget.User) ?? string.Empty;
        var userEntries = new List<string>(
            userPath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        var addedEntries = new List<string>();

        foreach (string configuredEntry in configuredEntries)
        {
            if (string.IsNullOrWhiteSpace(configuredEntry))
            {
                continue;
            }

            string expandedEntry = Environment
                .ExpandEnvironmentVariables(configuredEntry)
                .Trim()
                .Trim('"');

            if (!ContainsPathEntry(userEntries, expandedEntry))
            {
                userEntries.Add(expandedEntry);
                addedEntries.Add(expandedEntry);
            }
        }

        if (addedEntries.Count == 0)
        {
            return;
        }

        Environment.SetEnvironmentVariable(
            "Path",
            string.Join(";", userEntries.ToArray()),
            EnvironmentVariableTarget.User);

        string processPath = Environment.GetEnvironmentVariable(
            "Path", EnvironmentVariableTarget.Process) ?? string.Empty;
        var processEntries = new List<string>(
            processPath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        foreach (string addedEntry in addedEntries)
        {
            if (!ContainsPathEntry(processEntries, addedEntry))
            {
                processEntries.Add(addedEntry);
            }
        }

        Environment.SetEnvironmentVariable(
            "Path",
            string.Join(";", processEntries.ToArray()),
            EnvironmentVariableTarget.Process);
    }

    private static bool ContainsPathEntry(List<string> entries, string candidate)
    {
        string normalizedCandidate = NormalizePathEntry(candidate);

        foreach (string entry in entries)
        {
            if (string.Equals(
                NormalizePathEntry(entry),
                normalizedCandidate,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePathEntry(string value)
    {
        return Environment
            .ExpandEnvironmentVariables(value ?? string.Empty)
            .Trim()
            .Trim('"')
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static ProcessResult RunProcess(
        string executable,
        string arguments,
        string workingDirectory,
        int timeoutMinutes)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();
        object outputLock = new object();
        object errorLock = new object();

        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                {
                    lock (outputLock)
                    {
                        output.AppendLine(args.Data);
                    }
                }
            };

            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                {
                    lock (errorLock)
                    {
                        error.AppendLine(args.Data);
                    }
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Não foi possível iniciar o processo: " + executable);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool completed = process.WaitForExit(timeoutMinutes * 60 * 1000);
            if (!completed)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }

                return new ProcessResult
                {
                    ExitCode = -1,
                    Output = output.ToString(),
                    Error = error + "Tempo limite excedido."
                };
            }

            process.WaitForExit();
            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output.ToString(),
                Error = error.ToString()
            };
        }
    }

    private static void EnsureSuccess(ProcessResult result, string message)
    {
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AppendProcessLog(
        StringBuilder log,
        string title,
        ProcessResult result)
    {
        log.AppendLine("=== " + title + " ===");
        log.AppendLine("Código de saída: " + result.ExitCode);
        log.AppendLine(result.Output);
        log.AppendLine(result.Error);
    }

    private static string ExpandConfigurationValue(
        string value,
        string appDirectory)
    {
        return Environment.ExpandEnvironmentVariables(
            value.Replace("%APPDIR%", appDirectory));
    }

    private sealed class EnvironmentConfiguration
    {
        public bool Habilitado { get; set; }
        public List<EnvironmentVariableRule> Variaveis { get; set; }
        public PythonConfiguration Python { get; set; }
        public List<FileRule> Arquivos { get; set; }
    }

    private sealed class EnvironmentVariableRule
    {
        public bool Habilitado { get; set; }
        public string Nome { get; set; }
        public string Valor { get; set; }
    }

    private sealed class PythonConfiguration
    {
        public bool Habilitado { get; set; }
        public string Versao { get; set; }
        public bool CriarAmbienteVirtual { get; set; }
        public string ComandoPip { get; set; }
        public bool InstalarKernelsGlue { get; set; }
        public bool AdicionarAoPath { get; set; }
        public int TimeoutMinutos { get; set; }
    }

    private sealed class FileRule
    {
        public bool Habilitado { get; set; }
        public string Origem { get; set; }
        public string Destino { get; set; }
    }

    private sealed class ProcessResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
    }
}
