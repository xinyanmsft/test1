using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyTool
{
    public static class Options
    {
        public static string ConnectionString { get; set; } = ""; // TODO: Add

        public static string ContainerName { get; set; } = "test-container";

        public static int NumOfChannels { get; set; } = 8;

        public static long FileChunkSize { get; set; } = 1024 * 1024 * 1024;

        public static string BlobNamePrefix { get; set; } = "blob";
    }
}
