using BubbleTea.Domain;
using BubbleTea.Configuration;

namespace BubbleTea.Core
{
    public abstract class Emitter
    {
        protected readonly Random _random = new();
        protected readonly int _id;
        protected readonly SimulationConfig _config;
        protected readonly IQueue<Order> _orderQueue;
        protected readonly ILogger _logger;
        protected bool _isRunning = false;

        public Emitter(int id, SimulationConfig config, IQueue<Order> orderQueue, ILogger logger)
        {
            _id = id;
            _config = config;
            _orderQueue = orderQueue;
            _logger = logger;
        }

        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract void Stop();

        public event Action<Order>? OrderGenerated;

        protected Order GenerateOrder(bool isGroupOrder = false)
        {
            var random = new Random();
            var complexities = Enum.GetValues<DrinkComplexity>();
            var sizes = Enum.GetValues<DrinkSize>();
            
            var order = new Order
            {
                Complexity = complexities[random.Next(complexities.Length)],
                ToppingsCount = random.Next(0, 4),
                Size = sizes[random.Next(sizes.Length)],
                IsGroupOrder = isGroupOrder,
                HasStudentDiscount = isGroupOrder && random.NextDouble() > 0.5,
                Quantity = isGroupOrder ? random.Next(2, 6) : 1
            };
            
            OrderGenerated?.Invoke(order); 
            return order;
        }

        protected double GetNormalRandom(double mean, double deviation)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + deviation * randStdNormal;
        }
    }

    public class RegularEmitter : Emitter
    {
        public RegularEmitter(int id, SimulationConfig config, IQueue<Order> orderQueue, ILogger logger) 
            : base(id, config, orderQueue, logger) { }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"Emitter {_id} (Regular) started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    double interval = Math.Max(0.5, GetNormalRandom(
                        _config.Emitter.RegularOrderIntervalMean,
                        _config.Emitter.RegularOrderIntervalDeviation
                    ));
                    
                    await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
                    
                    var order = GenerateOrder();
                    if (_orderQueue.TryEnqueue(order, out string error))
                    {
                        var complexityName = order.Complexity.GetDisplayName();
                        _logger.Log($"Emitter {_id} generated {complexityName} order {order.Id}");
                    }
                    else
                    {
                        _logger.Log($"Emitter {_id} failed to enqueue order: {error}");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Emitter {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"Emitter {_id} (Regular) stopped");
        }

        public override void Stop() => _isRunning = false;
    }

    public class GroupEmitter : Emitter
    {
        public GroupEmitter(int id, SimulationConfig config, IQueue<Order> orderQueue, ILogger logger) 
            : base(id, config, orderQueue, logger) { }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"Emitter {_id} (Group) started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    double interval = Math.Max(1.0, GetNormalRandom(
                        _config.Emitter.GroupOrderIntervalMean,
                        _config.Emitter.GroupOrderIntervalDeviation
                    ));
                    
                    await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
                    
                    var order = GenerateOrder(isGroupOrder: true);
                    if (_orderQueue.TryEnqueue(order, out string error))
                    {
                        _logger.Log($"Emitter {_id} generated group order {order.Id} (Qty: {order.Quantity}, Complexity: {order.ComplexityScore:F2})");
                    }
                    else
                    {
                        _logger.Log($"Emitter {_id} failed to enqueue group order: {error}");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Emitter {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"Emitter {_id} (Group) stopped");
        }

        public override void Stop() => _isRunning = false;
    }
}