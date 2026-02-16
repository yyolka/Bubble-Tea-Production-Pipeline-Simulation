using System.Text.Json;
using System.Text.Json.Serialization;

namespace BubbleTea.Configuration
{
    public static class ConfigLoader
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        public static SimulationConfig Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Configuration file not found: {filePath}");
                    Console.WriteLine("Creating default configuration...");
                    var defaultConfig = CreateDefaultConfig();
                    Save(defaultConfig, filePath);
                    return defaultConfig;
                }

                Console.WriteLine($"Loading configuration from: {filePath}");
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<SimulationConfig>(json, _jsonOptions);

                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration");
                }

                ValidateConfig(config);

                Console.WriteLine($"Configuration loaded successfully");
                return config;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON format error: {ex.Message}");
                Console.WriteLine("Creating default configuration...");
                return CreateDefaultConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration loading error: {ex.Message}");
                Console.WriteLine("Using default configuration...");
                return CreateDefaultConfig();
            }
        }

        public static void Save(SimulationConfig config, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"Configuration saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration saving error: {ex.Message}");
            }
        }

        private static SimulationConfig CreateDefaultConfig()
        {
            return new SimulationConfig
            {
                SimulationDurationSeconds = 300,
                RegularEmitterCount = 2,
                GroupEmitterCount = 1,
                BasePreparationHandlers = 2,
                TapiocaCookingHandlers = 2,
                ToppingsHandlers = 2,
                QualityControlHandlers = 1,
                PackagingHandlers = 1,
                
                Emitter = new EmitterConfig
                {
                    RegularOrderIntervalMean = 3.0,
                    RegularOrderIntervalDeviation = 1.5,
                    GroupOrderIntervalMean = 10.0,
                    GroupOrderIntervalDeviation = 3.0
                },
                
                Queues = new QueueConfig
                {
                    OrderQueueCapacity = 35,
                    TapiocaQueueCapacity = 15,
                    ReadyQueueCapacity = 12
                },
                
                Handlers = new HandlerConfig
                {
                    BasePreparationMinTime = 0.5,
                    BasePreparationMaxTime = 1.0,
                    BasePreparationSuccessRate = 0.8,
                    BasePreparationRecalibrationRate = 0.15,
                    
                    TapiocaCookingMinTime = 8.0,
                    TapiocaCookingMaxTime = 12.0,
                    TapiocaCookingSuccessRate = 0.9,
                    
                    ToppingsMinTime = 0.5,
                    ToppingsMaxTime = 1.0,
                    ToppingsSuccessRate = 0.85,
                    ToppingsReworkRate = 0.1,
                    
                    QualityControlMinTime = 0.8,
                    QualityControlMaxTime = 1.2,
                    QualityControlSuccessRate = 0.75,
                    QualityControlMinorDefectRate = 0.2,
                    
                    PackagingMinTime = 2.0,
                    PackagingMaxTime = 8.0
                }
            };
        }

        private static void ValidateConfig(SimulationConfig config)
        {
            var errors = new List<string>();

            if (config.RegularEmitterCount < 1) errors.Add("Regular emitter count must be at least 1");
            if (config.GroupEmitterCount < 0) errors.Add("Group emitter count cannot be negative");
            if (config.BasePreparationHandlers < 1) errors.Add("Base preparation handlers count must be at least 1");
            if (config.TapiocaCookingHandlers < 1) errors.Add("Tapioca cooking handlers count must be at least 1");
            if (config.ToppingsHandlers < 1) errors.Add("Toppings handlers count must be at least 1");
            if (config.QualityControlHandlers < 1) errors.Add("Quality control handlers count must be at least 1");
            if (config.PackagingHandlers < 1) errors.Add("Packaging handlers count must be at least 1");
            if (config.Queues.OrderQueueCapacity < 5) errors.Add("Order queue capacity must be at least 5");
            if (config.Queues.TapiocaQueueCapacity < 3) errors.Add("Tapioca queue capacity must be at least 3");
            if (config.Queues.ReadyQueueCapacity < 3) errors.Add("Ready queue capacity must be at least 3");
            if (config.Emitter.RegularOrderIntervalMean <= 0) errors.Add("Regular order interval mean must be positive");
            if (config.Emitter.GroupOrderIntervalMean <= 0) errors.Add("Group order interval mean must be positive");

            ValidateProbability("Base preparation success rate", config.Handlers.BasePreparationSuccessRate, errors);
            ValidateProbability("Base preparation recalibration rate", config.Handlers.BasePreparationRecalibrationRate, errors);
            ValidateProbability("Tapioca cooking success rate", config.Handlers.TapiocaCookingSuccessRate, errors);
            ValidateProbability("Toppings success rate", config.Handlers.ToppingsSuccessRate, errors);
            ValidateProbability("Toppings rework rate", config.Handlers.ToppingsReworkRate, errors);
            ValidateProbability("Quality control success rate", config.Handlers.QualityControlSuccessRate, errors);
            ValidateProbability("Quality control minor defect rate", config.Handlers.QualityControlMinorDefectRate, errors);

            if (config.Handlers.BasePreparationSuccessRate + config.Handlers.BasePreparationRecalibrationRate > 1.0)
                errors.Add("Sum of base preparation success and recalibration rates cannot exceed 1.0");
            
            if (config.Handlers.ToppingsSuccessRate + config.Handlers.ToppingsReworkRate > 1.0)
                errors.Add("Sum of toppings success and rework rates cannot exceed 1.0");
            
            if (config.Handlers.QualityControlSuccessRate + config.Handlers.QualityControlMinorDefectRate > 1.0)
                errors.Add("Sum of quality control success and minor defect rates cannot exceed 1.0");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Configuration errors:\n{string.Join("\n", errors)}");
            }
        }

        private static void ValidateProbability(string name, double probability, List<string> errors)
        {
            if (probability < 0 || probability > 1)
            {
                errors.Add($"{name} must be in range 0 to 1 (current: {probability})");
            }
        }
    }
}