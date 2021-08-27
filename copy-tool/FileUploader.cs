using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace CopyTool
{
    public class FileUploader
    {
        private string _sourceFilePath;

        public FileUploader(string sourceFilePath)
        {
            _sourceFilePath = sourceFilePath;
        }

        public async Task DoUpload(CancellationToken cancellationToken)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Options.ConnectionString);

            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(Options.ContainerName);
            if (! await blobContainerClient.ExistsAsync(cancellationToken))
            {
                blobContainerClient = await blobServiceClient.CreateBlobContainerAsync(Options.ContainerName);
            }
            
            FileInfo fi = new FileInfo(_sourceFilePath);
            long sourceFileLen = fi.Length;

            long offset = 0;
            int i = 0;
            List<Task> tasks = new List<Task>();
            SemaphoreSlim throttle = new SemaphoreSlim(Options.NumOfChannels, Options.NumOfChannels);
            while (true)
            {
                if (offset >= sourceFileLen)
                {
                    break;
                }

                await throttle.WaitAsync(cancellationToken);

                i++;
                string blobName = $"{Options.BlobNamePrefix}-{i}.blob";
                long startOffset = offset;

                Task t = Task.Run(() => this.UploadWorker(blobContainerClient, throttle, blobName, startOffset, Options.FileChunkSize, cancellationToken));
                tasks.Add(t);

                offset += Options.FileChunkSize;
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task UploadWorker(BlobContainerClient blobContainerClient,
            SemaphoreSlim throttle,
            string blobName, 
            long offset, 
            long length, 
            CancellationToken cancellationToken)
        {
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            Console.WriteLine("uploading {0}, from {1} len {2}", blobName, offset, length);
            FileStream fileStream = File.Open(_sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                ChunkedReadStream readStream = new ChunkedReadStream(fileStream, offset, length);

                await blobClient.UploadAsync(readStream, cancellationToken).ConfigureAwait(false);

                Console.WriteLine("upload {0} finished.", blobName);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                fileStream.Dispose();
                throttle.Release();
            }
        }
    }
}
