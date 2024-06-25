using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class ChatClient
{
    static void Main(string[] args)
    {
        Console.Write("Enter server IP: ");
        string serverIP = Console.ReadLine();

        TcpClient client = new TcpClient(serverIP, 8080);
        NetworkStream stream = client.GetStream();

        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start(client);

        Console.WriteLine("Connected to chat server...");
        while (true)
        {
            string message = Console.ReadLine();
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }
    }

    static void ReceiveMessages(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byteCount;

        while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
            Console.WriteLine("Received: " + message);
        }
    }
}
