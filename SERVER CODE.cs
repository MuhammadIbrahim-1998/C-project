using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class FileServer
{
    private TcpListener listener;
    private string sharedFolder;
    private const int BufferSize = 1024;

    public FileServer(int port, string sharedFolder)
    {
        listener = new TcpListener(IPAddress.Any, port);
        this.sharedFolder = sharedFolder;
    }

    public void Start()
    {
        try
        {
            listener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
            if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine("The port is already in use. Try using a different port.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error: {ex.Message}");
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
            string requestedFile = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

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

    public static void Main(string[] args)
    {
        string sharedFolder = @"C:\SharedFiles";

        // Ensure the shared folder exists
        if (!Directory.Exists(sharedFolder))
        {
            Directory.CreateDirectory(sharedFolder);
            Console.WriteLine("Created shared folder at: " + sharedFolder);
        }

        // Changed port to 9001 to avoid conflicts
        int serverPort = 9001;
        FileServer server = new FileServer(serverPort, sharedFolder);
        server.Start();
    }
}
