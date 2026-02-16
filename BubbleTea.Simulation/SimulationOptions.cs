namespace BubbleTea.Simulation
{
    public class SimulationOptions
    {
        public string ConfigPath { get; } = "config.json";
        public string LogFilePath { get; } = "simulation.log";
        public bool EnableConsoleOutput { get; } = true;

        public static SimulationOptions Create()
        {
            return new SimulationOptions();
        }

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (!File.Exists(ConfigPath))
            {
                errors.Add($"Configuration file not found: {ConfigPath}");
            }

            return errors;
        }
    }
}