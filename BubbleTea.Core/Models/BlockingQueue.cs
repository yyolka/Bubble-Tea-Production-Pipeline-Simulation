using System.Collections.Concurrent;

namespace BubbleTea.Core
{
    public class BlockingQueue<T> : IQueue<T>
    {
        private readonly BlockingCollection<T> _queue;
        private readonly int _capacity;

        public BlockingQueue(int capacity)
        {
            _capacity = capacity;
            _queue = new BlockingCollection<T>(new ConcurrentQueue<T>(), capacity);
        }

        public bool TryEnqueue(T item, out string error)
        {
            error = ""; 
            if (_queue.Count >= _capacity)
            {
                error = "Queue is full";
                return false;
            }
            
            _queue.TryAdd(item);
            return true;
        }

        public bool TryDequeue(out T item)
        {
            return _queue.TryTake(out item!, 100); 
        }

        public int Count => _queue.Count;
        public int Capacity => _capacity;
        public bool IsFull => _queue.Count >= _capacity;
        public bool IsEmpty => _queue.Count == 0;
    }
}