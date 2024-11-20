using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Peer
{
    private TcpListener listener;
    private string sharedFolder;
    private const int BufferSize = 1024;

    public Peer(int port, string sharedFolder)
    {
        listener = new TcpListener(IPAddress.Any, port);
        this.sharedFolder = sharedFolder;
    }

    public void Start()
    {
        try
        {
            listener.Start();
            Console.WriteLine("Peer started. Waiting for connections...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting peer: {ex.Message}");
        }
    }

    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();

        try
        {
            byte[] buffer = new byte[BufferSize];
            int bytesRead = stream.Read(buffer, 0, BufferSize);
            string requestedFile = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"Requested file: {requestedFile}");

            string filePath = Path.Combine(sharedFolder, requestedFile);
            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                stream.Write(fileData, 0, fileData.Length);
                Console.WriteLine($"Sent file: {requestedFile}");
            }
            else
            {
                byte[] notFoundMessage = Encoding.UTF8.GetBytes("File not found");
                stream.Write(notFoundMessage, 0, notFoundMessage.Length);
                Console.WriteLine($"File not found: {requestedFile}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    public void RequestFile(string ipAddress, int port, string fileName, string savePath)
    {
        try
        {
            Console.WriteLine($"Attempting to connect to {ipAddress}:{port}");
            TcpClient client = new TcpClient(ipAddress, port);
            NetworkStream stream = client.GetStream();

            byte[] requestData = Encoding.UTF8.GetBytes(fileName);
            stream.Write(requestData, 0, requestData.Length);
            Console.WriteLine($"Requested file {fileName} from {ipAddress}:{port}");

            byte[] buffer = new byte[BufferSize];
            MemoryStream ms = new MemoryStream();
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, BufferSize)) > 0)
            {
                ms.Write(buffer, 0, bytesRead);
            }

            string fullSavePath = Path.Combine(savePath, fileName);
            File.WriteAllBytes(fullSavePath, ms.ToArray());
            Console.WriteLine($"File {fileName} saved to {fullSavePath}");

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting file: {ex.Message}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        string sharedFolder = @"C:\SharedFiles";

        // Ensure the shared folder exists
        if (!Directory.Exists(sharedFolder))
        {
            Directory.CreateDirectory(sharedFolder);
            Console.WriteLine("Created shared folder at: " + sharedFolder);
        }

        int peerPort = 9000;

        Peer peer = new Peer(peerPort, sharedFolder);
        Thread peerThread = new Thread(peer.Start);
        peerThread.Start();

        Console.WriteLine("Peer is running. Press 'r' to request a file or 'q' to quit.");
        while (true)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Q)
            {
                break;
            }
            else if (key == ConsoleKey.R)
            {
                Console.Write("Enter IP Address: ");
                string ipAddress = Console.ReadLine();
                Console.Write("Enter Port: ");
                int port = int.Parse(Console.ReadLine());
                Console.Write("Enter File Name: ");
                string fileName = Console.ReadLine();
                Console.Write("Enter Save Path: ");
                string savePath = Console.ReadLine();

                peer.RequestFile(ipAddress, port, fileName, savePath);
            }
        }
    }
}
