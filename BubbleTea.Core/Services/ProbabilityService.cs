using System;
using System.Collections.Generic;
using System.Linq;

namespace BubbleTea.Core.Services
{
    public static class ProbabilityService
    {
        private static readonly Random _globalRandom = new();
        [ThreadStatic]
        private static Random? _localRandom;

        private static Random Random
        {
            get
            {
                if (_localRandom == null)
                {
                    int seed;
                    lock (_globalRandom)
                    {
                        seed = _globalRandom.Next();
                    }
                    _localRandom = new Random(seed);
                }
                return _localRandom;
            }
        }

        public static bool IsEventHappened(double probability)
        {
            if (probability <= 0) return false;
            if (probability >= 1) return true;
            
            return Random.NextDouble() <= probability;
        }

        public static double GetNormalRandom(double mean, double stdDev, double? min = null, double? max = null)
        {
            double u1 = 1.0 - Random.NextDouble();
            double u2 = 1.0 - Random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double result = mean + stdDev * randStdNormal;

            if (min.HasValue && result < min.Value)
                result = min.Value;
            if (max.HasValue && result > max.Value)
                result = max.Value;

            return Math.Round(result, 2);
        }

        public static double GetUniformRandom(double min, double max)
        {
            if (min > max)
                (min, max) = (max, min);

            return min + (Random.NextDouble() * (max - min));
        }

        public static int GetRandomInt(int min, int max)
        {
            return Random.Next(min, max + 1);
        }

        public static T GetRandomItem<T>(IList<T> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("List cannot be empty");

            int index = Random.Next(items.Count);
            return items[index];
        }

        public static T GetWeightedRandom<T>(Dictionary<T, double> itemsWithWeights) where T : notnull
        {
            if (itemsWithWeights == null || itemsWithWeights.Count == 0)
                throw new ArgumentException("Dictionary cannot be empty");

            double totalWeight = itemsWithWeights.Values.Sum();
            double randomValue = Random.NextDouble() * totalWeight;
            
            double cumulative = 0;
            foreach (var kvp in itemsWithWeights)
            {
                cumulative += kvp.Value;
                if (randomValue <= cumulative)
                    return kvp.Key;
            }

            return itemsWithWeights.Last().Key;
        }

        public static TimeSpan GetRandomTime(double minSeconds, double maxSeconds)
        {
            double seconds = GetUniformRandom(minSeconds, maxSeconds);
            return TimeSpan.FromSeconds(seconds);
        }

        public static bool GetRandomBool(double probabilityTrue = 0.5)
        {
            return IsEventHappened(probabilityTrue);
        }

        public static int GenerateToppingsCount(int maxToppings = 3)
        {
            return Random.Next(0, maxToppings + 1);
        }
    }
}