using BubbleTea.Domain;
using BubbleTea.Configuration;
using BubbleTea.Core.Services;

namespace BubbleTea.Core
{
    public abstract class Handler
    {
        protected readonly Random _random = new();
        protected readonly int _id;
        protected readonly SimulationConfig _config;
        protected readonly ILogger _logger;
        protected readonly AtomicCounters _counters;
        protected volatile bool _isRunning = false;
        
        public event Action<Guid>? OrderCompleted;
        public event Action<Guid>? OrderFailed;
        public event Action<Guid>? OrderReworked;

        public int ProcessedCount => _counters.ProcessedCount;
        public int FailedCount => _counters.FailedCount;
        public int ReworkCount => _counters.ReworkCount;

        protected Handler(int id, SimulationConfig config, ILogger logger)
        {
            _id = id;
            _config = config;
            _logger = logger;
            _counters = new AtomicCounters();
        }

        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract void Stop();

        protected double GetRandomTime(double min, double max)
        {
            return min + (_random.NextDouble() * (max - min));
        }

        protected void OnOrderCompleted(Guid orderId) => OrderCompleted?.Invoke(orderId);
        protected void OnOrderFailed(Guid orderId) => OrderFailed?.Invoke(orderId);
        protected void OnOrderReworked(Guid orderId) => OrderReworked?.Invoke(orderId);
    }

    public class BasePreparationHandler : Handler
    {
        private readonly IQueue<Order> _inputQueue;
        private readonly IQueue<Order> _outputQueue;

        public BasePreparationHandler(int id, SimulationConfig config, IQueue<Order> inputQueue, 
                                     IQueue<Order> outputQueue, ILogger logger)
            : base(id, config, logger)
        {
            _inputQueue = inputQueue;
            _outputQueue = outputQueue;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"BasePreparationHandler {_id} started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_inputQueue.TryDequeue(out Order order))
                    {
                        _logger.Log($"BasePreparationHandler {_id} processing order {order.Id}");
                        
                        double processTime = GetRandomTime(
                            _config.Handlers.BasePreparationMinTime,
                            _config.Handlers.BasePreparationMaxTime
                        );
                        
                        await Task.Delay(TimeSpan.FromSeconds(processTime), cancellationToken);
                        
                        double rand = _random.NextDouble();
                        if (rand <= _config.Handlers.BasePreparationSuccessRate)
                        {
                            if (_outputQueue.TryEnqueue(order, out string error))
                            {
                                _logger.Log($"BasePreparationHandler {_id} completed order {order.Id}");
                                _counters.IncrementProcessed();
                                OnOrderCompleted(order.Id);
                            }
                            else
                            {
                                _logger.Log($"BasePreparationHandler {_id} failed to enqueue order {order.Id}: {error}");
                                _counters.IncrementFailed();
                                OnOrderFailed(order.Id);
                            }
                        }
                        else if (rand <= _config.Handlers.BasePreparationSuccessRate + 
                                 _config.Handlers.BasePreparationRecalibrationRate)
                        {
                            _logger.Log($"BasePreparationHandler {_id} needs recalibration for order {order.Id}");
                            _inputQueue.TryEnqueue(order, out _);
                            _counters.IncrementRework();
                            OnOrderReworked(order.Id);
                        }
                        else
                        {
                            _logger.Log($"BasePreparationHandler {_id} recipe error for order {order.Id}");
                            _counters.IncrementFailed();
                            OnOrderFailed(order.Id);
                        }
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"BasePreparationHandler {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"BasePreparationHandler {_id} stopped");
        }

        public override void Stop() => _isRunning = false;
    }

    public class TapiocaCookingHandler : Handler
    {
        private readonly BlockingQueue<int> _tapiocaQueue;
        private int _concurrentCooking = 0;
        private const int MAX_CONCURRENT_COOKING = 3;
        private const int LOW_STOCK_THRESHOLD = 5;

        public TapiocaCookingHandler(int id, SimulationConfig config, BlockingQueue<int> tapiocaQueue, ILogger logger)
            : base(id, config, logger)
        {
            _tapiocaQueue = tapiocaQueue;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"TapiocaCookingHandler {_id} started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_tapiocaQueue.Count <= LOW_STOCK_THRESHOLD && _concurrentCooking < MAX_CONCURRENT_COOKING)
                    {
                        Interlocked.Increment(ref _concurrentCooking);
                        _ = CookTapiocaBatch(cancellationToken);
                    }
                    
                    await Task.Delay(1000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"TapiocaCookingHandler {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"TapiocaCookingHandler {_id} stopped");
        }

        private async Task CookTapiocaBatch(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Log($"TapiocaCookingHandler {_id} started cooking batch");
                
                double cookTime = GetRandomTime(
                    _config.Handlers.TapiocaCookingMinTime,
                    _config.Handlers.TapiocaCookingMaxTime
                );
                
                await Task.Delay(TimeSpan.FromSeconds(cookTime), cancellationToken);
                
                if (ProbabilityService.IsEventHappened(_config.Handlers.TapiocaCookingSuccessRate))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (_tapiocaQueue.TryEnqueue(1, out string error))
                        {
                            _logger.Log($"TapiocaCookingHandler {_id} added tapioca portion");
                            _counters.IncrementProcessed();
                        }
                    }
                }
                else
                {
                    _logger.Log($"TapiocaCookingHandler {_id} batch failed (over/undercooked)");
                    _counters.IncrementFailed();
                }
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentCooking);
            }
        }

        public override void Stop() => _isRunning = false;
    }

    public class ToppingsHandler : Handler
    {
        private readonly IQueue<Order> _inputQueue;
        private readonly IQueue<Order> _outputQueue;
        private readonly BlockingQueue<int> _tapiocaQueue;

        public ToppingsHandler(int id, SimulationConfig config, IQueue<Order> inputQueue, 
                              IQueue<Order> outputQueue, BlockingQueue<int> tapiocaQueue, ILogger logger)
            : base(id, config, logger)
        {
            _inputQueue = inputQueue;
            _outputQueue = outputQueue;
            _tapiocaQueue = tapiocaQueue;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"ToppingsHandler {_id} started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_inputQueue.TryDequeue(out Order order))
                    {
                        _logger.Log($"ToppingsHandler {_id} processing order {order.Id}");
                        
                        if (order.ToppingsCount > 0 && _tapiocaQueue.Count < order.ToppingsCount)
                        {
                            _logger.Log($"ToppingsHandler {_id} waiting for tapioca for order {order.Id}");
                            _inputQueue.TryEnqueue(order, out _);
                            await Task.Delay(1000, cancellationToken);
                            continue;
                        }
                        
                        for (int i = 0; i < order.ToppingsCount; i++)
                        {
                            _tapiocaQueue.TryDequeue(out _);
                        }
                        
                        double processTime = GetRandomTime(
                            _config.Handlers.ToppingsMinTime,
                            _config.Handlers.ToppingsMaxTime
                        ) * (order.Complexity == DrinkComplexity.SpecialMenu ? 1.5 : 1.0);
                        
                        await Task.Delay(TimeSpan.FromSeconds(processTime), cancellationToken);
                        
                        if (ProbabilityService.IsEventHappened(_config.Handlers.ToppingsSuccessRate))
                        {
                            if (_outputQueue.TryEnqueue(order, out string error))
                            {
                                _logger.Log($"ToppingsHandler {_id} completed order {order.Id}");
                                _counters.IncrementProcessed();
                                OnOrderCompleted(order.Id);
                            }
                            else
                            {
                                _logger.Log($"ToppingsHandler {_id} failed to enqueue order {order.Id}: {error}");
                                _counters.IncrementFailed();
                                OnOrderFailed(order.Id);
                            }
                        }
                        else if (ProbabilityService.IsEventHappened(_config.Handlers.ToppingsReworkRate))
                        {
                            _logger.Log($"ToppingsHandler {_id} needs rework for order {order.Id}");
                            _inputQueue.TryEnqueue(order, out _);
                            _counters.IncrementRework();
                            OnOrderReworked(order.Id);
                        }
                        else
                        {
                            _logger.Log($"ToppingsHandler {_id} missing ingredients for order {order.Id}");
                            _inputQueue.TryEnqueue(order, out _);
                            await Task.Delay(2000, cancellationToken);
                        }
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"ToppingsHandler {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"ToppingsHandler {_id} stopped");
        }

        public override void Stop() => _isRunning = false;
    }

    public class QualityControlHandler : Handler
    {
        private readonly IQueue<Order> _inputQueue;
        private readonly IQueue<Order> _outputQueue;

        public QualityControlHandler(int id, SimulationConfig config, IQueue<Order> inputQueue, 
                                    IQueue<Order> outputQueue, ILogger logger)
            : base(id, config, logger)
        {
            _inputQueue = inputQueue;
            _outputQueue = outputQueue;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"QualityControlHandler {_id} started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_inputQueue.TryDequeue(out Order order))
                    {
                        _logger.Log($"QualityControlHandler {_id} checking order {order.Id}");
                        
                        double processTime = GetRandomTime(
                            _config.Handlers.QualityControlMinTime,
                            _config.Handlers.QualityControlMaxTime
                        );
                        
                        await Task.Delay(TimeSpan.FromSeconds(processTime), cancellationToken);
                        
                        double rand = _random.NextDouble();
                        if (rand <= _config.Handlers.QualityControlSuccessRate)
                        {
                            if (_outputQueue.TryEnqueue(order, out string error))
                            {
                                _logger.Log($"QualityControlHandler {_id} approved order {order.Id}");
                                _counters.IncrementProcessed();
                                OnOrderCompleted(order.Id);
                            }
                            else
                            {
                                _logger.Log($"QualityControlHandler {_id} failed to enqueue order {order.Id}: {error}");
                                _counters.IncrementFailed();
                                OnOrderFailed(order.Id);
                            }
                        }
                        else if (rand <= _config.Handlers.QualityControlSuccessRate + 
                                 _config.Handlers.QualityControlMinorDefectRate)
                        {
                            _logger.Log($"QualityControlHandler {_id} minor defect in order {order.Id}");
                            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                            _inputQueue.TryEnqueue(order, out _);
                            _counters.IncrementRework();
                            OnOrderReworked(order.Id);
                        }
                        else
                        {
                            _logger.Log($"QualityControlHandler {_id} critical defect in order {order.Id}");
                            _counters.IncrementFailed();
                            OnOrderFailed(order.Id);
                        }
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"QualityControlHandler {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"QualityControlHandler {_id} stopped");
        }

        public override void Stop() => _isRunning = false;
    }

    public class PackagingHandler : Handler
    {
        private readonly IQueue<Order> _inputQueue;

        public PackagingHandler(int id, SimulationConfig config, IQueue<Order> inputQueue, ILogger logger)
            : base(id, config, logger)
        {
            _inputQueue = inputQueue;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.Log($"PackagingHandler {_id} started");
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_inputQueue.TryDequeue(out Order order))
                    {
                        _logger.Log($"PackagingHandler {_id} packaging order {order.Id}");
                        
                        double processTime = GetRandomTime(
                            _config.Handlers.PackagingMinTime,
                            _config.Handlers.PackagingMaxTime
                        );
                        
                        await Task.Delay(TimeSpan.FromSeconds(processTime), cancellationToken);
                        
                        _logger.Log($"PackagingHandler {_id} completed order {order.Id}");
                        _counters.IncrementProcessed();
                        OnOrderCompleted(order.Id);
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"PackagingHandler {_id} error: {ex.Message}");
                }
            }
            
            _logger.Log($"PackagingHandler {_id} stopped");
        }

        public override void Stop() => _isRunning = false;
    }
}