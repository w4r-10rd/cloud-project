using System;
using System.Diagnostics;
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
            ProcessStartInfo psi;
            if (OperatingSystem.IsWindows())
            {
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

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

    static void SubmitResult(string url, string result)
    {
        using (var client = new WebClient())
        {
            client.UploadString(url, result);
        }
    }
}

