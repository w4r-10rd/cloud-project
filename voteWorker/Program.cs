using System;
using System.Net.Sockets;
using System.Text;

class PollingWorker
{
    static void Main(string[] args)
    {
        string masterIP = "127.0.0.1"; // Replace with actual master IP
        int masterPort = 8080; // Replace with actual master port

        TcpClient client = new TcpClient(masterIP, masterPort);
        NetworkStream stream = client.GetStream();

        // Example: Create a poll
        CreatePoll(stream, "What is your favorite color?", new string[] { "Red", "Blue", "Green" });

        // Example: Vote in a poll
        Vote(stream, 1, "User123", "Blue");

        // Example: Get results of a poll
        GetPollResults(stream, 1);

        client.Close();
    }

    static void CreatePoll(NetworkStream stream, string question, string[] options)
    {
        string optionsString = string.Join(";", options);
        string data = $"CREATE_POLL|{question}|{optionsString}";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);

        // Example: Receive response
        byte[] responseBuffer = new byte[1024];
        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        if (response.StartsWith("POLL_CREATED|"))
        {
            int pollId = int.Parse(response.Split('|')[1]);
            Console.WriteLine($"Poll created successfully. Poll ID: {pollId}");
        }
        else
        {
            Console.WriteLine("Failed to create poll.");
        }
    }

    static void Vote(NetworkStream stream, int pollId, string participantId, string option)
    {
        string data = $"VOTE|{pollId}|{participantId}|{option}";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);

        // Example: Receive response
        byte[] responseBuffer = new byte[1024];
        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        if (response == "VOTE_RECORDED")
        {
            Console.WriteLine($"Vote recorded for poll ID {pollId} by participant {participantId}.");
        }
        else
        {
            Console.WriteLine($"Failed to record vote for poll ID {pollId}.");
        }
    }

    static void GetPollResults(NetworkStream stream, int pollId)
    {
        string data = $"RESULTS|{pollId}";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);

        // Example: Receive and display results
        byte[] responseBuffer = new byte[1024];
        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        Console.WriteLine(response);
    }
}
