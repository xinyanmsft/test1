using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CopyTool
{
    public class FileDownloader
    {
        private string _targetFilePath;

        public FileDownloader(string targetFilePath)
        {
            _targetFilePath = targetFilePath;
        }

        public async Task DoDownload(CancellationToken cancellationToken)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Options.ConnectionString);

            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(Options.ContainerName);
            if (!await blobContainerClient.ExistsAsync(cancellationToken))
            {
                throw new Exception($"Container {Options.ContainerName} doesn't exist!");
            }

            var blobs = blobContainerClient.GetBlobsAsync(traits: BlobTraits.Metadata, prefix: Options.BlobNamePrefix, cancellationToken: cancellationToken);
            List<BlobDownloadWorkItem> items = new List<BlobDownloadWorkItem>();
            await foreach(BlobItem b in blobs)
            {
                items.Add(new BlobDownloadWorkItem()
                { 
                    Id = b.Name,
                    Size = b.Properties.ContentLength.Value
                });
            }

            var orderedItems = items.OrderBy(b => b.Id);
            var fileSize = items.Sum(b => b.Size);
            using (FileStream fileStream = File.Open(_targetFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
            {
                fileStream.SetLength(fileSize);
            }

            List<Task> tasks = new List<Task>();
            SemaphoreSlim throttle = new SemaphoreSlim(Options.NumOfChannels, Options.NumOfChannels);

            long startOffset = 0;
            foreach (var blobItem in orderedItems)
            {
                await throttle.WaitAsync(cancellationToken);
                long offset = startOffset;
                Task t = Task.Run(() => this.DownloadWorker(blobContainerClient, throttle, blobItem.Id, offset, blobItem.Size, cancellationToken));
                tasks.Add(t);
                startOffset += blobItem.Size;
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task DownloadWorker(BlobContainerClient blobContainerClient,
            SemaphoreSlim throttle,
            string blobName,
            long offset,
            long length,
            CancellationToken cancellationToken)
        {
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            Console.WriteLine("downloading {0}, from {1} len {2}", blobName, offset, length);
            FileStream fileStream = File.Open(_targetFilePath, FileMode.Open, FileAccess.Write, FileShare.Write);
            try
            {
                ChunkedWriteStream writeStream = new ChunkedWriteStream(fileStream, offset, length);

                await blobClient.DownloadToAsync(writeStream, cancellationToken).ConfigureAwait(false);

                Console.WriteLine("download {0} finished.", blobName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                fileStream.Dispose();
                throttle.Release();
            }
        }

        private class BlobDownloadWorkItem
        {
            public string Id { get; set; }
            public long Size { get; set; }
        }
    }
}
