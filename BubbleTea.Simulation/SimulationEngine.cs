using BubbleTea.Configuration;
using BubbleTea.Core;
using BubbleTea.Domain;
using System.Collections.Concurrent;

namespace BubbleTea.Simulation
{
    public class SimulationEngine
    {
        private readonly SimulationConfig _config;
        private readonly ILogger _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly BlockingQueue<Order> _orderQueue;
        private readonly BlockingQueue<Order> _preparationQueue;
        private readonly BlockingQueue<Order> _toppingsQueue;
        private readonly BlockingQueue<Order> _qualityQueue;
        private readonly BlockingQueue<int> _tapiocaQueue;
        private readonly List<Emitter> _emitters = new();
        private readonly List<Handler> _handlers = new();
        private readonly List<Task> _tasks = new();
        private readonly ConcurrentDictionary<Guid, Order> _activeOrders = new();
        private readonly ConcurrentBag<Order> _completedOrders = new();
        private readonly ConcurrentBag<Order> _failedOrders = new();
        private int _reworkCount = 0;
        private int _ordersGeneratedCount = 0;

        public SimulationEngine(SimulationConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
            _orderQueue = new BlockingQueue<Order>(_config.Queues.OrderQueueCapacity);
            _preparationQueue = new BlockingQueue<Order>(_config.Queues.OrderQueueCapacity);
            _toppingsQueue = new BlockingQueue<Order>(_config.Queues.OrderQueueCapacity);
            _qualityQueue = new BlockingQueue<Order>(_config.Queues.ReadyQueueCapacity);
            _tapiocaQueue = new BlockingQueue<int>(_config.Queues.TapiocaQueueCapacity);

            for (int i = 0; i < 10; i++)
            {
                _tapiocaQueue.TryEnqueue(1, out _);
            }
        }

        public async Task<SimulationStatistics> RunAsync()
        {
            _logger.Log("=== Starting Bubble Tea Production Simulation ===");
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            
            try
            {
                CreateEmitters();
                CreateHandlers();
                SetupEventHandlers();
                StartComponents(cancellationToken);
                
                _logger.Log($"Simulation running for {_config.SimulationDurationSeconds} seconds...");
                _logger.Log($"Initial tapioca: {_tapiocaQueue.Count}");

                var monitoringTask = StartMonitoringAsync(cancellationToken);
                
                using var stopTimer = new Timer(_ => 
                {
                    _logger.Log("Time's up! Stopping simulation...");
                    StopSimulation();
                }, null, TimeSpan.FromSeconds(_config.SimulationDurationSeconds), Timeout.InfiniteTimeSpan);

                await Task.WhenAll(_tasks);
                await monitoringTask;
                
                _logger.Log("=== Simulation Completed ===");

                await Task.Delay(2000);

                return CollectRealStatistics();
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Simulation was cancelled");
                return CollectRealStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Simulation error: {ex.Message}");
                throw;
            }
            finally
            {
                ForceStop();
            }
        }

        private void CreateEmitters()
        {
            int emitterId = 1;
            for (int i = 0; i < _config.RegularEmitterCount; i++)
            {
                var emitter = new RegularEmitter(emitterId++, _config, _orderQueue, _logger);
                emitter.OrderGenerated += order => 
                {
                    Interlocked.Increment(ref _ordersGeneratedCount);
                    _activeOrders.TryAdd(order.Id, order);
                    order.StartTime = DateTime.Now;
                    _logger.Log($"Order {order.Id} generated (Total: {_ordersGeneratedCount})");
                };
                
                _emitters.Add(emitter);
            }
            for (int i = 0; i < _config.GroupEmitterCount; i++)
            {
                var emitter = new GroupEmitter(emitterId++, _config, _orderQueue, _logger);
                
                emitter.OrderGenerated += order => 
                {
                    Interlocked.Increment(ref _ordersGeneratedCount);
                    _activeOrders.TryAdd(order.Id, order);
                    order.StartTime = DateTime.Now;
                    _logger.Log($"Group order {order.Id} generated (Qty: {order.Quantity}, Total: {_ordersGeneratedCount})");
                };
                
                _emitters.Add(emitter);
            }
            
            _logger.Log($"Created {_emitters.Count} emitters");
        }

        private void CreateHandlers()
        {
            int handlerId = 1;

            for (int i = 0; i < _config.BasePreparationHandlers; i++)
            {
                _handlers.Add(new BasePreparationHandler(
                    handlerId++, _config, _orderQueue, _preparationQueue, _logger));
            }

            for (int i = 0; i < _config.TapiocaCookingHandlers; i++)
            {
                _handlers.Add(new TapiocaCookingHandler(
                    handlerId++, _config, _tapiocaQueue, _logger));
            }

            for (int i = 0; i < _config.ToppingsHandlers; i++)
            {
                _handlers.Add(new ToppingsHandler(
                    handlerId++, _config, _preparationQueue, _toppingsQueue, _tapiocaQueue, _logger));
            }

            for (int i = 0; i < _config.QualityControlHandlers; i++)
            {
                _handlers.Add(new QualityControlHandler(
                    handlerId++, _config, _toppingsQueue, _qualityQueue, _logger));
            }

            for (int i = 0; i < _config.PackagingHandlers; i++)
            {
                _handlers.Add(new PackagingHandler(
                    handlerId++, _config, _qualityQueue, _logger));
            }
            
            _logger.Log($"Created {_handlers.Count} handlers");
        }

        private void SetupEventHandlers()
        {
            foreach (var handler in _handlers)
            {
                if (handler is PackagingHandler packagingHandler)
                {
                    packagingHandler.OrderCompleted += orderId => 
                    {
                        if (_activeOrders.TryRemove(orderId, out var order))
                        {
                            order.CompletionTime = DateTime.Now;
                            _completedOrders.Add(order);
                            _logger.Log($"Order {orderId} completed in {order.ProcessingTime?.TotalSeconds:F1}s");
                        }
                    };
                }
                
                handler.OrderFailed += orderId => 
                {
                    if (_activeOrders.TryRemove(orderId, out var order))
                    {
                        _failedOrders.Add(order);
                    }
                };
                
                handler.OrderReworked += orderId => 
                {
                    Interlocked.Increment(ref _reworkCount);
                };
            }
        }

        private void StartComponents(CancellationToken cancellationToken)
        {
            foreach (var emitter in _emitters)
            {
                _tasks.Add(Task.Run(async () => 
                {
                    try
                    {
                        await emitter.StartAsync(cancellationToken);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Emitter error: {ex.Message}");
                    }
                }, cancellationToken));
            }

            foreach (var handler in _handlers)
            {
                _tasks.Add(Task.Run(async () => 
                {
                    try
                    {
                        await handler.StartAsync(cancellationToken);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Handler error: {ex.Message}");
                    }
                }, cancellationToken));
            }
            
            _logger.Log($"Started {_tasks.Count} components");
        }

        private async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            try
            {
                int checkInterval = 10; 
                int checks = 0;
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(checkInterval), cancellationToken);
                    checks++;

                    if (checks % 3 == 0)
                    {
                        int inSystem = _orderQueue.Count + _preparationQueue.Count + 
                                     _toppingsQueue.Count + _qualityQueue.Count;
                        int completed = _completedOrders.Count;
                        double completionRate = _ordersGeneratedCount > 0 
                            ? (double)completed / _ordersGeneratedCount * 100 
                            : 0;
                        
                        _logger.Log($"=== Progress Report ===");
                        _logger.Log($"Generated: {_ordersGeneratedCount}, Completed: {completed} ({completionRate:F1}%)");
                        _logger.Log($"In system: {inSystem}, Failed: {_failedOrders.Count}, Rework: {_reworkCount}");
                        _logger.Log($"Queues: Order({_orderQueue.Count}), Prep({_preparationQueue.Count}), Top({_toppingsQueue.Count}), QC({_qualityQueue.Count})");
                        _logger.Log($"Tapioca: {_tapiocaQueue.Count}/{_tapiocaQueue.Capacity}");

                        if (_tapiocaQueue.Count < 5)
                            _logger.Log("WARNING: Low tapioca stock!");
                        if (_toppingsQueue.Count > 20)
                            _logger.Log("WARNING: Toppings queue too long!");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void StopSimulation()
        {
            _logger.Log("Stopping all components...");
            
            foreach (var emitter in _emitters)
            {
                emitter.Stop();
            }
            
            foreach (var handler in _handlers)
            {
                handler.Stop();
            }
            
            _cancellationTokenSource?.Cancel();
        }

        private void ForceStop()
        {
            _logger.Log("Force stopping...");
            StopSimulation();
            
            try
            {
                Task.WaitAll(_tasks.ToArray(), TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        private SimulationStatistics CollectRealStatistics()
        {
            var completedOrders = _completedOrders.ToList();
            var failedOrders = _failedOrders.ToList();
            
            double totalProcessingTime = 0;
            foreach (var order in completedOrders)
            {
                if (order.ProcessingTime.HasValue)
                {
                    totalProcessingTime += order.ProcessingTime.Value.TotalSeconds;
                }
            }
            
            double avgProcessingTime = completedOrders.Count > 0 
                ? totalProcessingTime / completedOrders.Count 
                : 0;
            
            double successRate = _ordersGeneratedCount > 0 
                ? (double)completedOrders.Count / _ordersGeneratedCount * 100 
                : 0;
            
            double simulationMinutes = _config.SimulationDurationSeconds / 60.0;
            
            return new SimulationStatistics
            {
                TotalOrdersGenerated = _ordersGeneratedCount,
                TotalOrdersProcessed = completedOrders.Count,
                TotalOrdersFailed = failedOrders.Count,
                TotalOrdersReworked = _reworkCount,
                AverageOrderProcessingTime = avgProcessingTime,
                OrdersPerMinute = _ordersGeneratedCount / simulationMinutes,
                Throughput = completedOrders.Count / simulationMinutes,
                
                QueueLengths = new Dictionary<string, int>
                {
                    ["OrderQueue"] = _orderQueue.Count,
                    ["PreparationQueue"] = _preparationQueue.Count,
                    ["TapiocaQueue"] = _tapiocaQueue.Count,
                    ["ToppingsQueue"] = _toppingsQueue.Count,
                    ["QualityQueue"] = _qualityQueue.Count
                },
                
                HandlerStats = CollectHandlerStatistics(),
                
                BottleneckAnalysis = CalculateRealBottlenecks(completedOrders.Count)
            };
        }

        private Dictionary<string, HandlerStatistics> CollectHandlerStatistics()
        {
            var stats = new Dictionary<string, HandlerStatistics>();
            
            foreach (var handler in _handlers)
            {
                var typeName = handler.GetType().Name;
                if (!stats.ContainsKey(typeName))
                {
                    stats[typeName] = new HandlerStatistics();
                }
                
                stats[typeName].Processed += handler.ProcessedCount;
                stats[typeName].Failed += handler.FailedCount;
                stats[typeName].Reworked += handler.ReworkCount;

                double utilization = (double)handler.ProcessedCount / (_config.SimulationDurationSeconds / 60.0);
                stats[typeName].Utilization = Math.Min(100, utilization * 10);
            }
            
            return stats;
        }

        private List<string> CalculateRealBottlenecks(int completedOrders)
        {
            var bottlenecks = new List<string>();

            if (_toppingsQueue.Count > 20)
            {
                bottlenecks.Add($"CRITICAL: Toppings queue has {_toppingsQueue.Count} orders (capacity: {_config.Queues.OrderQueueCapacity})");
                bottlenecks.Add($"-> Add more ToppingsHandlers (currently {_config.ToppingsHandlers})");
                bottlenecks.Add($"-> Increase TapiocaCookingHandlers (currently {_config.TapiocaCookingHandlers})");
            }

            if (_tapiocaQueue.Count < 3)
            {
                bottlenecks.Add($"Tapioca stock critically low: {_tapiocaQueue.Count} portions");
                bottlenecks.Add($"-> Tapioca cooking can't keep up with demand");
                bottlenecks.Add($"-> Consider: 1) More TapiocaCookingHandlers, 2) Larger TapiocaQueueCapacity");
            }

            if (completedOrders < _ordersGeneratedCount / 4)
            {
                bottlenecks.Add($"Very low completion rate: only {completedOrders}/{_ordersGeneratedCount} orders completed");
                bottlenecks.Add($"-> System is overwhelmed, orders are stuck in queues");
            }

            if (_preparationQueue.Count > 15)
                bottlenecks.Add($"Preparation queue long ({_preparationQueue.Count}) - base preparation is bottleneck");
            
            if (_qualityQueue.Count > 10)
                bottlenecks.Add($"Quality control queue long ({_qualityQueue.Count}) - QC is bottleneck");
            
            return bottlenecks;
        }

        public void Stop()
        {
            StopSimulation();
        }
    }
}