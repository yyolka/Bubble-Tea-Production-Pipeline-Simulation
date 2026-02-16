namespace BubbleTea.Domain
{
    public enum DrinkComplexity
    {
        MilkTea = 1,
        CoffeeWithTapioca = 2,
        SpecialMenu = 3
    }

    public static class DrinkComplexityExtensions
    {
        public static double GetComplexityMultiplier(this DrinkComplexity complexity)
        {
            return complexity switch
            {
                DrinkComplexity.MilkTea => 1.0,
                DrinkComplexity.CoffeeWithTapioca => 1.5,
                DrinkComplexity.SpecialMenu => 2.0,
                _ => 1.0
            };
        }

        public static string GetDisplayName(this DrinkComplexity complexity)
        {
            return complexity switch
            {
                DrinkComplexity.MilkTea => "Milk tea",
                DrinkComplexity.CoffeeWithTapioca => "Coffee with tapioca",
                DrinkComplexity.SpecialMenu => "Special menu",
                _ => "Unknown"
            };
        }

    }
}
