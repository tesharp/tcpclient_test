using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var listener = new TcpListener(ip, 8080);

            Console.WriteLine("Listening for connections...!");
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Task.Run(() => Process(client));
            }

            Console.WriteLine("Press a key to continue...!");
            Console.ReadKey();
        }

        static async Task Process(TcpClient client)
        {
            // Console.WriteLine("Client connected!");

            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            string line = default;

            line = await reader.ReadLineAsync();
            var number = int.Parse(line);
            line = await reader.ReadLineAsync();
            var bufferSize = int.Parse(line);

            var buffer = new byte[bufferSize];
            using (var bufReader = new BufferedStream(stream))
            {
                long bytesRead = 0;

                var startTime = DateTime.Now;

                while (true)
                {
                    var read = await bufReader.ReadAsync(buffer, 0, bufferSize);
                    if (read == 0)
                        break;

                    bytesRead += read;
                }

                var timeSpent = (DateTime.Now - startTime).TotalMilliseconds;
                // sum += (decimal)timeSpent;

                Console.WriteLine($"{Header(number)} Bytes read: {bytesRead}, time spent: {timeSpent}, bufferSize: {bufferSize}");
            }

            // Console.WriteLine("Client disconnected!");
            client.Dispose();
        }

        static string Header(int number)
        {
            return $"{number} :: ";
        }
    }
}
