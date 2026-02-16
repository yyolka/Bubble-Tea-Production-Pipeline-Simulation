using System;

namespace BubbleTea.Domain
{
    public class Order
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreationTime { get; } = DateTime.Now;
        public DateTime? StartTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public DrinkComplexity Complexity { get; set; }
        public int ToppingsCount { get; set; }
        public DrinkSize Size { get; set; }
        public bool IsGroupOrder { get; set; }
        public bool HasStudentDiscount { get; set; }
        public int Quantity { get; set; } = 1;
        public double ComplexityScore => (int)Complexity * Size.GetSizeMultiplier();
        
        public TimeSpan? ProcessingTime => CompletionTime.HasValue && StartTime.HasValue 
            ? CompletionTime.Value - StartTime.Value 
            : null;
    }
}