using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static Dictionary<int, decimal> Averages = new Dictionary<int, decimal>();

        static async Task Main(string[] args)
        {
            var bufferSize = 500000;
            var iterations = 10;
            var maxDiff = 250000;
            var level = 1;
            var currentBufferSize = bufferSize;

            while (true)
            {
                var lowerBufferSize = FindNextLower(bufferSize, level);
                var upperBufferSize = FindNextUpper(bufferSize, level);

                await Loop(bufferSize, iterations, maxDiff);
                await Loop(lowerBufferSize, iterations, maxDiff);
                await Loop(upperBufferSize, iterations, maxDiff);

                if (bufferSize - lowerBufferSize + 1 < maxDiff || upperBufferSize - bufferSize + 1 < maxDiff)
                {
                    Averages.Clear();
                    maxDiff /= 2;
                    iterations *= 2;
                    continue;
                }

                if (Averages[upperBufferSize] < Averages[bufferSize] && Averages[upperBufferSize] < Averages[lowerBufferSize] && bufferSize == Averages.Keys.Max())
                    bufferSize = bufferSize * 2;
                else
                {
                    bufferSize = Averages.OrderBy(kvp => kvp.Value).First().Key;
                    // bufferSize = (int)lowest.Average(kvp => kvp.Key);
                    level += 1;
                }

                currentBufferSize = bufferSize;
            }

            Console.WriteLine("Press a key to continue...");
            Console.ReadKey();
        }

        static async Task Loop(int bufferSize, int iterations, int maxDiff)
        {
            if (Averages.ContainsKey(bufferSize))
                return;

            decimal sum = 0;

            for (int ii = 0; ii < iterations; ii++)
                sum += (decimal)await Connect(ii, bufferSize, iterations, maxDiff);

            var average = sum / iterations;
            Averages.Add(bufferSize, average);

            Console.WriteLine();
        }

        static int FindNextUpper(int bufferSize, int level)
        {
            if (Averages.ContainsKey(bufferSize) && Averages.ContainsKey(bufferSize + 1) && Averages.ContainsKey(bufferSize - 1))
                return bufferSize;

            var upper = bufferSize + (bufferSize / level);

            if (Averages.ContainsKey(upper))
                return FindNextUpper(bufferSize, level + 1);

            return upper;
        }

        static int FindNextLower(int bufferSize, int level)
        {
            if (Averages.ContainsKey(bufferSize) && Averages.ContainsKey(bufferSize + 1) && Averages.ContainsKey(bufferSize - 1))
                return bufferSize;

            var lower = bufferSize - (bufferSize / (level * 2));

            if (Averages.ContainsKey(lower))
                return FindNextLower(bufferSize, level + 1);

            return lower;
        }

        static async Task<double> Connect(int number, int bufferSize, int iterations, int maxDiff)
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var client = new TcpClient();

            try
            {
                await client.ConnectAsync(ip, 8080);

                var stream = client.GetStream();

                var writer = new StreamWriter(stream);
                long bytesSent = 0;
                var buffer = new byte[bufferSize];

                await writer.WriteLineAsync(number.ToString());
                await writer.WriteLineAsync(bufferSize.ToString());
                await writer.FlushAsync();

                using (var bufWriter = new BufferedStream(stream, bufferSize))
                {
                    var startTime = DateTime.Now;

                    using (var fileStream = File.OpenRead("fake.jpg"))
                    {
                        long numBytesToRead = fileStream.Length;

                        while (numBytesToRead > 0)
                        {
                            var read = await fileStream.ReadAsync(buffer, 0, bufferSize);
                            if (read == 0)
                                break;

                            await bufWriter.WriteAsync(buffer, 0, read);

                            numBytesToRead -= read;
                            bytesSent += read;
                        }
                    }

                    await bufWriter.FlushAsync();
                    var timeSpent = (DateTime.Now - startTime).TotalMilliseconds;

                    Console.Write($"\rBuffer: {bufferSize}, Iterations: {iterations}, Max diff: {maxDiff},  Bytes sent: {bytesSent}, time spent: {timeSpent}");

                    client.Dispose();

                    return timeSpent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Header(number) + ex.Message);
            }

            return 0;
        }

        static string Header(int number)
        {
            return $"{number} :: ";
        }
    }
}
