

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CopyTool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("copy-tool upload|download <file name>");
                return;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "upload"))
            {
                Upload(args[1]);
            }
            else
            {
                Download(args[1]);
            }            
        }

        private static void Download(string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {filePath} already exist.");
                return;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => cts.Cancel();

            FileDownloader fileDownloader = new FileDownloader(filePath);

            Stopwatch s = Stopwatch.StartNew();
            fileDownloader.DoDownload(cts.Token).Wait();
            s.Stop();

            Console.WriteLine("Download completed in {0}ms.", s.Elapsed.TotalMilliseconds);
        }

        private static void Upload(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File {filePath} does not exist.");
                return;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => cts.Cancel();

            FileUploader fileUploader = new FileUploader(filePath);

            Stopwatch s = Stopwatch.StartNew();
            fileUploader.DoUpload(cts.Token).Wait();
            s.Stop();

            Console.WriteLine($"Upload completed in {0}ms.", s.Elapsed.TotalMilliseconds);
        }
    }
}