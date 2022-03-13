using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;



/**
 * Connects to a selected ParallelReplicator-server and creates files and directories as reported by the server
 * Usage: ParallelReplicatorClient.exe <server-url> <replicate-directory>
 */



public class ParallelReplicatorClient {

    public static int buffer_size = 16 * 1024 * 1024;
    public static string base_directory;

    public static void Main(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: ParallelReplicatorClient.exe <server-url> <replicate-directory>\n");
            return;
        }

        var url = args[0];
        base_directory = args[1];
        Console.Write($"replicating files to: {base_directory}\n");

        _ = ConnectAsync(url);

        Console.WriteLine("ParallelReplicator client connecting...\n");
        Console.WriteLine("Press any key to exit.\n");
        Console.ReadKey();
    }

    public static CancellationTokenSource cts = new CancellationTokenSource();
    public static ClientWebSocket ws;

    public static async Task ConnectAsync(string url) {
        ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(url), cts.Token);
        await Task.Factory.StartNew(ReceiveLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private static async Task ReceiveLoop() {
        var loopToken = cts.Token;
        MemoryStream outputStream = null;
        WebSocketReceiveResult receiveResult = null;
        var buffer = new byte[buffer_size];
        try {
            while (!loopToken.IsCancellationRequested) {
                outputStream = new MemoryStream(buffer_size);
                do {
                    receiveResult = await ws.ReceiveAsync(buffer, cts.Token);
                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                        outputStream.Write(buffer, 0, receiveResult.Count);
                }
                while (!receiveResult.EndOfMessage);
                if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                outputStream.Position = 0;
                ResponseReceived(outputStream);
            }
        }
        catch (TaskCanceledException) { }
        finally {
            outputStream?.Dispose();
        }
    }

    private static void ResponseReceived(Stream stream) {
        StreamReader reader = new StreamReader(stream);
        string message = reader.ReadToEnd();

        var instructions = message.Split(",", 3);
        var cmd = instructions[0];
        var path = Encoding.UTF8.GetString(Convert.FromBase64String(instructions[1]));
        Console.Write($"cmd: {cmd} -> {path}\n");

        if (cmd == "update") {
            byte[] filedata = Convert.FromBase64String(instructions[2]);
            File.WriteAllBytes(base_directory + "/" + path, filedata);
        } else if (cmd == "updatedirectory") {
            if (!Directory.Exists(base_directory + "/" + path))
                Directory.CreateDirectory(base_directory + "/" + path);
        } else if (cmd == "delete") {
            try { File.Delete(base_directory + "/" + path); } catch (Exception) {}
            try { Directory.Delete(base_directory + "/" + path, true); } catch (Exception) {}
        }

        stream.Dispose();
    }
}

