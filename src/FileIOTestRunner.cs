using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTest
{
    public class FileIOTestRunner
    {
        private readonly ManualResetEvent _completeEvent;
        private readonly IOTestOptions _option;
        private readonly byte[] _buffer;
        private int _testCount = 0;
        private long _bytesRead = 0;
        private int _readCount = 0;
        private long _bytesWritten = 0;
        private int _writeCount = 0;

        public FileIOTestRunner()
        {
            _completeEvent = new ManualResetEvent(false);
            _option = IOTestOptions.Instance;
            _buffer = new byte[4096];
        }

        public void RunScenario()
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = (byte)(i % 256);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Task.Run(() => this.RunScenarioInternal());
            _completeEvent.WaitOne();

            stopwatch.Stop();
            double readPerSecond = _readCount / stopwatch.Elapsed.TotalSeconds;
            double writePerSecond = _writeCount / stopwatch.Elapsed.TotalSeconds;
            double readMBPerSecond = _bytesRead / stopwatch.Elapsed.TotalSeconds / 1000000;
            double writeMBPerSecond = _bytesWritten / stopwatch.Elapsed.TotalSeconds / 1000000;
            Logger.Info("Test completed in {0:N2}ms.", stopwatch.ElapsedMilliseconds);
            Logger.Info("{0:N2} read operation per seconds, {1:N2}MB read bytes per seconds.", readPerSecond, readMBPerSecond);
            Logger.Info("{0:N2} write operation per seconds, {1:N2}MB write bytes per seconds.", writePerSecond, writeMBPerSecond);
        }

        private async Task RunScenarioInternal()
        {
            List<Task> tasks = new List<Task>();
            for(int i = 0; i < _option.ConcurrentFiles; i++)
            {
                tasks.Add(Task.Run(() => this.RunFileTestAsync(i)));
            }
            Logger.Verbose("Waiting for {0} threads to complete.", _option.ConcurrentFiles);
            await Task.WhenAll(tasks);

            _completeEvent.Set();
        }

        private async Task RunFileTestAsync(int runnerId)
        {
            var rand = new Random(runnerId);
            List<string> testFiles = new List<string>();
            byte[] readBuffer = new byte[_buffer.Length];

            int i = 0;
            while (true)
            {
                i++;
                if (i % 10 == 0)
                {
                    // read / delete files every 10 iterations
                    await ReadAndCleanupFiles(testFiles, runnerId);
                    testFiles.Clear();
                }

                int c = Interlocked.Increment(ref _testCount);
                if (c > _option.Count)
                {
                    Logger.Verbose("Runner {0} completed, test count {1}", runnerId, c);
                    break;
                }

                long fileSize = (long)(1000 * _option.FileSizeInKB * (rand.NextDouble() + 0.5));
                string testFile = Path.Combine(_option.TestDirectory, Guid.NewGuid().ToString());
                testFiles.Add(testFile);

                try
                {
                    Logger.Verbose("Runner {0} create file {1} size {2}.", runnerId, testFile, fileSize);

                    // Write the file.
                    using (var fs = File.Create(testFile))
                    {
                        while (fileSize > 0)
                        {
                            int bytesToWrite = (int)Math.Min(fileSize, _buffer.Length);
                            await fs.WriteAsync(_buffer, 0, bytesToWrite);
                            fileSize -= bytesToWrite;
                            Interlocked.Add(ref _bytesWritten, bytesToWrite);
                            Interlocked.Increment(ref _writeCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to test {0}, size {1}. Exception: {2}.", testFile, fileSize, ex);
                    break;
                }
            }

            await ReadAndCleanupFiles(testFiles, runnerId);
            testFiles.Clear();
        }

        private async Task ReadAndCleanupFiles(IEnumerable<string> testFiles, int runnerId)
        {
            byte[] readBuffer = new byte[_buffer.Length];
            foreach (var testFile in testFiles)
            {
                using (var fs = File.OpenRead(testFile))
                {
                    int bytesRead = 0;
                    do
                    {
                        long c = 0;
                        bytesRead = await fs.ReadAsync(readBuffer, 0, readBuffer.Length);
                        if (_option.Verify)
                        {
                            for (int i = 0; i < bytesRead; i++)
                            {
                                if (readBuffer[i] != _buffer[i])
                                {
                                    Logger.Error("File content does not match, name {0}, offset {1}.", testFile, c + i);
                                    return;
                                }
                            }
                        }
                        c += bytesRead;
                        Interlocked.Add(ref _bytesRead, bytesRead);
                        Interlocked.Increment(ref _readCount);
                    } while (bytesRead > 0);

                    Logger.Verbose("Runner {0} read file {1}.", runnerId, testFile);
                }

                try
                {
                    File.Delete(testFile);
                }
                catch(Exception ex)
                {
                    Logger.Error("Failed to delete {0}. Exception: {1}.", testFile, ex);
                }
            }
        }
    }
}
