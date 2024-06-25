using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class PollingMaster
{
    static readonly object lockObj = new object();
    static List<Poll> polls = new List<Poll>();

    class Poll
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public Dictionary<string, int> Results { get; set; }
        public HashSet<string> Participants { get; set; }
    }

    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Console.WriteLine("Polling master started...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byteCount;

        while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
            ProcessClientData(data, stream);
        }

        client.Close();
    }

    static void ProcessClientData(string data, NetworkStream stream)
    {
        string[] parts = data.Split('|');
        string command = parts[0];

        if (command == "CREATE_POLL")
        {
            string question = parts[1];
            List<string> options = new List<string>(parts[2].Split(';'));

            lock (lockObj)
            {
                int pollId = polls.Count + 1; // Generate unique ID
                polls.Add(new Poll
                {
                    Id = pollId,
                    Question = question,
                    Options = options,
                    Results = new Dictionary<string, int>(),
                    Participants = new HashSet<string>()
                });

                byte[] response = Encoding.UTF8.GetBytes($"POLL_CREATED|{pollId}");
                stream.Write(response, 0, response.Length);
            }
        }
        else if (command == "VOTE")
        {
            int pollId = int.Parse(parts[1]);
            string participantId = parts[2];
            string option = parts[3];

            lock (lockObj)
            {
                var poll = polls.Find(p => p.Id == pollId);
                if (poll != null && poll.Options.Contains(option) && !poll.Participants.Contains(participantId))
                {
                    poll.Participants.Add(participantId);
                    if (!poll.Results.ContainsKey(option))
                        poll.Results[option] = 0;
                    poll.Results[option]++;

                    byte[] response = Encoding.UTF8.GetBytes($"VOTE_RECORDED|{pollId}|{participantId}");
                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    byte[] response = Encoding.UTF8.GetBytes("VOTE_ERROR");
                    stream.Write(response, 0, response.Length);
                }
            }
        }
        else if (command == "RESULTS")
        {
            int pollId = int.Parse(parts[1]);

            lock (lockObj)
            {
                var poll = polls.Find(p => p.Id == pollId);
                if (poll != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Poll results for poll ID {pollId}:");
                    foreach (var option in poll.Options)
                    {
                        int count = poll.Results.ContainsKey(option) ? poll.Results[option] : 0;
                        sb.AppendLine($"{option}: {count} votes");
                    }

                    byte[] response = Encoding.UTF8.GetBytes(sb.ToString());
                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    byte[] response = Encoding.UTF8.GetBytes("RESULTS_ERROR");
                    stream.Write(response, 0, response.Length);
                }
            }
        }
    }
}
