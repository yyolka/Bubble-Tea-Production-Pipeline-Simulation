using System;
using System.Collections.Generic;

namespace BubbleTea.Core
{
    public class SimulationStatistics
    {
        public int TotalOrdersGenerated { get; set; }
        public int TotalOrdersProcessed { get; set; }
        public int TotalOrdersFailed { get; set; }
        public int TotalOrdersReworked { get; set; }
        public double AverageOrderProcessingTime { get; set; }
        public double OrdersPerMinute { get; set; }
        public double Throughput { get; set; }
        
        public Dictionary<string, int> QueueLengths { get; set; } = new();
        public Dictionary<string, HandlerStatistics> HandlerStats { get; set; } = new();
        public List<string> BottleneckAnalysis { get; set; } = new();

        public void AddQueueLength(string queueName, int length)
        {
            QueueLengths[queueName] = length;
        }

        public void AddHandlerStatistics(string handlerName, HandlerStatistics stats)
        {
            HandlerStats[handlerName] = stats;
        }

        public void AddBottleneck(string bottleneck)
        {
            BottleneckAnalysis.Add(bottleneck);
        }

        public void CalculateDerivedMetrics(int simulationDurationSeconds)
        {
            if (simulationDurationSeconds > 0)
            {
                OrdersPerMinute = (TotalOrdersProcessed * 60.0) / simulationDurationSeconds;
                Throughput = (double)TotalOrdersProcessed / simulationDurationSeconds;
            }
        }

        public string GetSummary()
        {
            var summary = new System.Text.StringBuilder();
            
            summary.AppendLine("=== SIMULATION STATISTICS ===");
            summary.AppendLine($"Total Orders Generated: {TotalOrdersGenerated}");
            summary.AppendLine($"Total Orders Processed: {TotalOrdersProcessed}");
            summary.AppendLine($"Total Orders Failed: {TotalOrdersFailed}");
            summary.AppendLine($"Total Orders Reworked: {TotalOrdersReworked}");
            
            if (TotalOrdersGenerated > 0)
            {
                double successRate = (TotalOrdersProcessed * 100.0) / TotalOrdersGenerated;
                summary.AppendLine($"Success Rate: {successRate:F1}%");
            }
            
            summary.AppendLine($"Average Processing Time: {AverageOrderProcessingTime:F2} sec");
            summary.AppendLine($"Orders Per Minute: {OrdersPerMinute:F1}");
            summary.AppendLine($"Throughput: {Throughput:F2} orders/sec");
            
            return summary.ToString();
        }

        public string GetQueueReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== QUEUE STATUS ===");
            foreach (var kvp in QueueLengths)
            {
                report.AppendLine($"{kvp.Key.PadRight(25)}: {kvp.Value} items");
            }
            
            return report.ToString();
        }

        public string GetHandlerReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== HANDLER STATISTICS ===");
            foreach (var kvp in HandlerStats)
            {
                var stats = kvp.Value;
                report.AppendLine($"{kvp.Key}:");
                report.AppendLine($"  Processed: {stats.Processed}");
                report.AppendLine($"  Failed: {stats.Failed}");
                report.AppendLine($"  Reworked: {stats.Reworked}");
                report.AppendLine($"  Utilization: {stats.Utilization:P1}");
            }
            
            return report.ToString();
        }

        public string GetBottleneckReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== BOTTLENECK ANALYSIS ===");
            if (BottleneckAnalysis.Count == 0)
            {
                report.AppendLine("No significant bottlenecks detected.");
            }
            else
            {
                foreach (var bottleneck in BottleneckAnalysis)
                {
                    report.AppendLine($"• {bottleneck}");
                }
            }
            
            return report.ToString();
        }

        public string GetFullReport()
        {
            return GetSummary() + "\n" + 
                   GetQueueReport() + "\n" + 
                   GetHandlerReport() + "\n" + 
                   GetBottleneckReport();
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["TotalOrdersGenerated"] = TotalOrdersGenerated,
                ["TotalOrdersProcessed"] = TotalOrdersProcessed,
                ["TotalOrdersFailed"] = TotalOrdersFailed,
                ["TotalOrdersReworked"] = TotalOrdersReworked,
                ["AverageOrderProcessingTime"] = AverageOrderProcessingTime,
                ["OrdersPerMinute"] = OrdersPerMinute,
                ["Throughput"] = Throughput,
                ["QueueLengths"] = QueueLengths,
                ["HandlerStats"] = HandlerStats,
                ["BottleneckAnalysis"] = BottleneckAnalysis
            };
        }
    }

    public class HandlerStatistics
    {
        public int Processed { get; set; }
        public int Failed { get; set; }
        public int Reworked { get; set; }
        public double Utilization { get; set; }

        public void CalculateUtilization(int totalOrders, int simulationTime)
        {
            if (simulationTime > 0 && totalOrders > 0)
            {
                Utilization = (double)Processed / totalOrders;
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["Processed"] = Processed,
                ["Failed"] = Failed,
                ["Reworked"] = Reworked,
                ["Utilization"] = Utilization
            };
        }
    }

    public class OrderStatistics
    {
        public Guid OrderId { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public TimeSpan? ProcessingTime => CompletionTime - CreationTime;
        public bool IsCompleted { get; set; }
        public bool Failed { get; set; }
        public bool Reworked { get; set; }
        public int HandlerCount { get; set; }

        public void MarkCompleted()
        {
            CompletionTime = DateTime.Now;
            IsCompleted = true;
        }

        public void MarkFailed()
        {
            Failed = true;
        }

        public void MarkReworked()
        {
            Reworked = true;
        }
    }
}