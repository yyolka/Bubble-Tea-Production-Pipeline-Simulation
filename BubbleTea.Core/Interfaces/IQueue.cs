namespace BubbleTea.Core
{
    public interface IQueue<T>
    {
        bool TryEnqueue(T item, out string error);
        bool TryDequeue(out T item);
        int Count { get; }
        int Capacity { get; }
        bool IsFull { get; }
        bool IsEmpty { get; }
    }
}