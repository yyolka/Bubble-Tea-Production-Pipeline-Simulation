using BubbleTea.Domain;
using BubbleTea.Core;
using System.Collections.Concurrent;

namespace BubbleTea.Simulation
{
    public class OrderTracker
    {
        private readonly ConcurrentDictionary<Guid, OrderTrackingInfo> _orders = new();
        private readonly ILogger _logger;
        private int _completedOrders = 0;
        private int _failedOrders = 0;
        private int _reworkCount = 0;
        private readonly List<double> _processingTimes = new();

        public OrderTracker(ILogger logger)
        {
            _logger = logger;
        }

        public void TrackOrderGenerated(Order order)
        {
            _orders.TryAdd(order.Id, new OrderTrackingInfo
            {
                OrderId = order.Id,
                Created = DateTime.Now,
                Status = OrderStatus.Created
            });
        }

        public void TrackOrderProcessing(Guid orderId, string stage)
        {
            if (_orders.TryGetValue(orderId, out var info))
            {
                info.CurrentStage = stage;
                info.LastUpdate = DateTime.Now;
            }
        }

        public void TrackOrderCompleted(Guid orderId)
        {
            if (_orders.TryGetValue(orderId, out var info) && info.Completed == null)
            {
                info.Completed = DateTime.Now;
                info.Status = OrderStatus.Completed;
                _completedOrders++;
                
                double processingTime = (info.Completed.Value - info.Created).TotalSeconds;
                _processingTimes.Add(processingTime);
                
                _logger.Log($"Order {orderId} completed in {processingTime:F2}s");
            }
        }

        public void TrackOrderFailed(Guid orderId)
        {
            if (_orders.TryGetValue(orderId, out var info))
            {
                info.Status = OrderStatus.Failed;
                info.Failed = true;
                _failedOrders++;
            }
        }

        public void TrackOrderRework(Guid orderId)
        {
            _reworkCount++;
            if (_orders.TryGetValue(orderId, out var info))
            {
                info.ReworkCount++;
            }
        }

        public SimulationStatistics GetStatistics()
        {
            int totalGenerated = _orders.Count;
            int totalInSystem = _orders.Values.Count(o => o.Completed == null && !o.Failed);
            int totalCompleted = _completedOrders;
            int totalFailed = _failedOrders;
            
            double avgProcessingTime = _processingTimes.Count > 0 ? _processingTimes.Average() : 0;
            double successRate = totalGenerated > 0 ? (double)totalCompleted / totalGenerated * 100 : 0;

            return new SimulationStatistics
            {
                TotalOrdersGenerated = totalGenerated,
                TotalOrdersProcessed = totalCompleted,
                TotalOrdersFailed = totalFailed,
                TotalOrdersReworked = _reworkCount,
                AverageOrderProcessingTime = avgProcessingTime,
            };
        }

        private class OrderTrackingInfo
        {
            public Guid OrderId { get; set; }
            public DateTime Created { get; set; }
            public DateTime? Completed { get; set; }
            public string CurrentStage { get; set; } = "";
            public DateTime LastUpdate { get; set; }
            public OrderStatus Status { get; set; }
            public bool Failed { get; set; }
            public int ReworkCount { get; set; }
        }

        private enum OrderStatus
        {
            Created,
            InProgress,
            Completed,
            Failed
        }
    }
}