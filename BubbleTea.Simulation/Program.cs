using BubbleTea.Configuration;
using BubbleTea.Core;

namespace BubbleTea.Simulation
{
    class Program
    {
        static async Task Main()
        {
            var options = SimulationOptions.Create();

            if (!File.Exists(options.ConfigPath))
            {
                Console.WriteLine($"ERROR: Configuration file '{options.ConfigPath}' not found.");
                Console.WriteLine("Create config.json file with simulation settings.");
                Environment.Exit(1);
            }

            Console.WriteLine("=== Bubble Tea Production Simulation ===");
            Console.WriteLine($"Using config: {options.ConfigPath}");
            Console.WriteLine("=========================================\n");

            var config = ConfigLoader.Load(options.ConfigPath);

            using var logger = new FileLogger(options.LogFilePath, options.EnableConsoleOutput);
            
            try
            {
                await RunSimulation(config, logger);
            }
            catch (Exception ex)
            {
                logger.LogError($"Simulation error: {ex.Message}");
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static async Task RunSimulation(SimulationConfig config, ILogger logger)
        {
            logger.Log("=== Starting Bubble Tea Production Simulation ===");
            var engine = new SimulationEngine(config, logger);
            var statistics = await engine.RunAsync();
            DisplayResults(statistics, logger);
        }

        static void DisplayResults(SimulationStatistics stats, ILogger logger)
        {
            logger.Log("\n=== SIMULATION RESULTS ===");
            
            logger.Log($"Total orders generated: {stats.TotalOrdersGenerated}");
            logger.Log($"Total orders processed: {stats.TotalOrdersProcessed}");
            logger.Log($"Total orders failed: {stats.TotalOrdersFailed}");
            logger.Log($"Total orders reworked: {stats.TotalOrdersReworked}");
            
            logger.Log("\nQueue Status at End:");
            foreach (var kvp in stats.QueueLengths)
            {
                logger.Log($"  {kvp.Key.PadRight(25)}: {kvp.Value} items");
            }
            
            double successRate = stats.TotalOrdersGenerated > 0 
                ? stats.TotalOrdersProcessed * 100.0 / stats.TotalOrdersGenerated 
                : 0;
            
            logger.Log($"\nAverage order processing time: {stats.AverageOrderProcessingTime:F2} seconds");
            logger.Log($"Processing success rate: {successRate:F1}%");
        }
    }
}