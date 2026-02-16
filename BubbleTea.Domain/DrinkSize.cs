namespace BubbleTea.Domain
{
    public enum DrinkSize
    {
        Small = 500,
        Medium = 700,
        Large = 1000
    }

    public static class DrinkSizeExtensions
    {
        public static double GetSizeMultiplier(this DrinkSize size)
        {
            return size switch
            {
                DrinkSize.Small => 1.0,
                DrinkSize.Medium => 1.2,
                DrinkSize.Large => 1.5,
                _ => 1.0
            };
        }

        public static string GetDisplayName(this DrinkSize size)
        {
            return size switch
            {
                DrinkSize.Small => "500ml (Small)",
                DrinkSize.Medium => "700ml (Medium)",
                DrinkSize.Large => "1000ml (Large)",
                _ => "Unknown"
            };
        }

        public static int GetVolumeMl(this DrinkSize size)
        {
            return (int)size;
        }

        public static double GetPackagingTimeMultiplier(this DrinkSize size)
        {
            return size switch
            {
                DrinkSize.Small => 1.0,
                DrinkSize.Medium => 1.3,
                DrinkSize.Large => 1.7,
                _ => 1.0
            };
        }
        public static int GetRecommendedTapiocaPortions(this DrinkSize size)
        {
            return size switch
            {
                DrinkSize.Small => 1,
                DrinkSize.Medium => 2,
                DrinkSize.Large => 3,
                _ => 1
            };
        }
    }
}