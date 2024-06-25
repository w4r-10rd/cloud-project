using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

class TaskQueueMaster
{
    static ConcurrentQueue<string> taskQueue = new ConcurrentQueue<string>();
    static ConcurrentDictionary<string, string> results = new ConcurrentDictionary<string, string>();

    static void Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://+:8080/");
        listener.Start();
        Console.WriteLine("Master is listening...");

        Task.Run(() => ProcessRequests(listener));

        Console.WriteLine("Enter tasks (type 'exit' to stop):");
        string task;
        while ((task = Console.ReadLine()) != "exit")
        {
            taskQueue.Enqueue(task);
        }

        listener.Stop();
    }

    static void ProcessRequests(HttpListener listener)
    {
        while (listener.IsListening)
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            if (request.Url.AbsolutePath == "/getTask")
            {
                if (taskQueue.TryDequeue(out string task))
                {
                    // Check if the task is executable
                    if (IsExecutableTask(task))
                    {
                        // Send the executable task as response
                        byte[] buffer = Encoding.UTF8.GetBytes(task);
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        // Handle non-executable tasks as needed
                        // For example, log or skip them
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                }
            }
            else if (request.Url.AbsolutePath == "/submitResult")
            {
                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string result = reader.ReadToEnd();
                    results[Guid.NewGuid().ToString()] = result;
                }
                response.StatusCode = (int)HttpStatusCode.OK;
            }

            response.Close();
        }
    }

    static bool IsExecutableTask(string task)
    {
        // Implement logic to determine if the task is executable
        // This can be based on a specific prefix, file extension, etc.
        // Example: Check if the task starts with "execute:"
        return task.StartsWith("execute:");
    }
}
