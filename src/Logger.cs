using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTest
{
    internal static class Logger
    {
        public static void Info(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public static void Error(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public static void Verbose(string format, params object[] args)
        {
            if (IOTestOptions.Instance.Verbose)
            {
                Console.WriteLine(format, args);
            }
        }
    }
}
