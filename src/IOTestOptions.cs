using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTest
{
    public class IOTestOptions
    {
        public static IOTestOptions Instance { get; private set; }

        public int Count { get; set; }

        public bool Verbose { get; set; }

        public int FileSizeInKB { get; set; }

        public int ConcurrentFiles { get; set; }

        public string TestDirectory {  get; set; }

        public bool Verify { get; set; }

        public IOTestOptions()
        {
            this.Count = 100;
            this.Verbose = false;
            this.FileSizeInKB = 100;
            this.ConcurrentFiles = 8;
            this.TestDirectory = Directory.GetCurrentDirectory();
            this.Verify = false;

            IOTestOptions.Instance = this;
        }

        public bool Validate()
        {
            if (this.Count <= 0)
            {
                Logger.Error($"Invalid Count value: {0}", this.Count);
                return false;
            }

            if (!Directory.Exists(this.TestDirectory))
            {
                Logger.Error($"Test directory {0} does not exist.", this.TestDirectory);
                return false;
            }

            return true;
        }
    }
}
