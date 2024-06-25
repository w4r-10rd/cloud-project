using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class ChatServer
{
    static List<TcpClient> clients = new List<TcpClient>();
    static readonly object lockObj = new object();

    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Console.WriteLine("Server started...");

        // Thread to handle server messages
        Thread serverInputThread = new Thread(HandleServerInput);
        serverInputThread.Start();

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            lock (lockObj) clients.Add(client);

            Console.WriteLine("New client connected...");
            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    static void HandleClient(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byteCount;

        try
        {
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Console.WriteLine("Received: " + message);
                BroadcastMessage(message, client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }
        finally
        {
            lock (lockObj) clients.Remove(client);
            client.Close();
            Console.WriteLine("Client disconnected...");
        }
    }

    static void HandleServerInput()
    {
        while (true)
        {
            string message = Console.ReadLine();
            BroadcastMessage("Server: " + message, null);
        }
    }

    static void BroadcastMessage(string message, TcpClient excludeClient)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);

        lock (lockObj)
        {
            foreach (var client in clients)
            {
                if (client != excludeClient)
                {
                    NetworkStream stream = client.GetStream();
                    try
                    {
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception while broadcasting: " + ex.Message);
                    }
                }
            }
        }
    }
}
