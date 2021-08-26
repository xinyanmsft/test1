using Microsoft.Extensions.Configuration;

namespace IOTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var options = new IOTestOptions();
            configuration.Bind(options);

            if (!options.Validate())
            {
                Logger.Info("Invalid arguments.");
                return;
            }

            FileIOTestRunner runner = new FileIOTestRunner();
            runner.RunScenario();
        }
    }
}