using System.Text.Json;

namespace BubbleTea.Configuration
{
    public class EmitterConfig
    {
        public double RegularOrderIntervalMean { get; set; } = 3.0;
        public double RegularOrderIntervalDeviation { get; set; } = 1.5;
        public double GroupOrderIntervalMean { get; set; } = 10.0;
        public double GroupOrderIntervalDeviation { get; set; } = 3.0;
    }

    public class QueueConfig
    {
        public int OrderQueueCapacity { get; set; } = 35;
        public int TapiocaQueueCapacity { get; set; } = 15;
        public int ReadyQueueCapacity { get; set; } = 12;
    }

    public class HandlerConfig
    {
        public double BasePreparationMinTime { get; set; } = 0.5;
        public double BasePreparationMaxTime { get; set; } = 1.0;
        public double TapiocaCookingMinTime { get; set; } = 8.0;
        public double TapiocaCookingMaxTime { get; set; } = 12.0;
        public double ToppingsMinTime { get; set; } = 0.5;
        public double ToppingsMaxTime { get; set; } = 1.0;
        public double QualityControlMinTime { get; set; } = 0.8;
        public double QualityControlMaxTime { get; set; } = 1.2;
        public double PackagingMinTime { get; set; } = 2.0;
        public double PackagingMaxTime { get; set; } = 8.0;
 
        public double BasePreparationSuccessRate { get; set; } = 0.8;
        public double BasePreparationRecalibrationRate { get; set; } = 0.15;
        public double TapiocaCookingSuccessRate { get; set; } = 0.9;
        public double ToppingsSuccessRate { get; set; } = 0.85;
        public double ToppingsReworkRate { get; set; } = 0.1;
        public double QualityControlSuccessRate { get; set; } = 0.75;
        public double QualityControlMinorDefectRate { get; set; } = 0.2;
    }

    public class SimulationConfig
    {
        public int SimulationDurationSeconds { get; set; } = 300;
        public int RegularEmitterCount { get; set; } = 2;
        public int GroupEmitterCount { get; set; } = 1;
        public int BasePreparationHandlers { get; set; } = 2;
        public int TapiocaCookingHandlers { get; set; } = 2;
        public int ToppingsHandlers { get; set; } = 2;
        public int QualityControlHandlers { get; set; } = 1;
        public int PackagingHandlers { get; set; } = 1;
        
        public EmitterConfig Emitter { get; set; } = new();
        public QueueConfig Queues { get; set; } = new();
        public HandlerConfig Handlers { get; set; } = new();

        public static void Save(SimulationConfig config, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
}