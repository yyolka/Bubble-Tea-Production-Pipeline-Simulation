using System.Threading;

namespace BubbleTea.Core
{
    public class AtomicCounters
    {
        private int _processedCount = 0;
        private int _failedCount = 0;
        private int _reworkCount = 0;

        public int ProcessedCount => Interlocked.CompareExchange(ref _processedCount, 0, 0);
        public int FailedCount => Interlocked.CompareExchange(ref _failedCount, 0, 0);
        public int ReworkCount => Interlocked.CompareExchange(ref _reworkCount, 0, 0);

        public void IncrementProcessed() => Interlocked.Increment(ref _processedCount);
        public void IncrementFailed() => Interlocked.Increment(ref _failedCount);
        public void IncrementRework() => Interlocked.Increment(ref _reworkCount);
    }

    public class ConcurrentCookingManager
    {
        private int _currentCooking = 0;
        public const int MAX_CONCURRENT_COOKING = 3;
        public const int LOW_STOCK_THRESHOLD = 5;

        public int CurrentCooking => Interlocked.CompareExchange(ref _currentCooking, 0, 0);

        public bool TryStartCooking()
        {
            while (true)
            {
                int current = CurrentCooking;
                if (current >= MAX_CONCURRENT_COOKING)
                    return false;

                if (Interlocked.CompareExchange(ref _currentCooking, current + 1, current) == current)
                    return true;
            }
        }

        public void FinishCooking() => Interlocked.Decrement(ref _currentCooking);
    }
}