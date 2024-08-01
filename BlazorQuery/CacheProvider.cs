namespace BlazorQuery
{
    public class CacheProvider
    {
        private readonly Dictionary<string, (object Value, DateTime Timestamp)> _cache = new();
        private readonly Dictionary<string, Task<object>> _fetchTasks = new();
        private readonly Dictionary<string, Func<Task<object>>> _fetchFunctions = new();
        private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _staleTime = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);
        private readonly Task _cleanupTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly WeakReferenceMessenger _messenger = new();

        public CacheProvider()
        {
            _cleanupTask = Task.Run(CleanupExpiredCacheEntries);
        }

        public CacheProvider(TimeSpan cacheLifetime, TimeSpan staleTime, TimeSpan cleanupInterval)
        {
            _cacheLifetime = cacheLifetime;
            _staleTime = staleTime;
            _cleanupInterval = cleanupInterval;
            _cleanupTask = Task.Run(CleanupExpiredCacheEntries);
        }

        public void Subscribe(string key, Action<string, object> subscriber)
        {
            _messenger.Subscribe(key, subscriber);
        }

        public void Unsubscribe(string key, Action<string, object> subscriber)
        {
            _messenger.Unsubscribe(key, subscriber);
        }

        public async Task<T> UseQuery<T>(string[] keys, Func<Task<T>> fetchFunction)
        {
            var key = string.Join("|", keys);
            return await UseQuery(key, fetchFunction);
        }

        public async Task<T> UseQuery<T>(string key, Func<Task<T>> fetchFunction)
        {
            if (_cache.TryGetValue(key, out var cachedEntry))
            {
                var age = DateTime.UtcNow - cachedEntry.Timestamp;
                if (age < _cacheLifetime)
                {
                    if (age >= _staleTime)
                    {
                        _ = FetchAndCacheAsync(key, fetchFunction);
                    }
                    NotifySubscribers(key, cachedEntry.Value);
                    return (T)cachedEntry.Value;
                }
                else
                {
                    _cache.Remove(key);
                }
            }

            if (_fetchTasks.TryGetValue(key, out var fetchTask))
            {
                return (T)await fetchTask;
            }

            var task = FetchAndCacheAsync(key, fetchFunction);
            _fetchTasks[key] = task;
            _fetchFunctions[key] = async () => await fetchFunction();
            return (T)await task;
        }

        public async Task InvalidateQuery(string[] keys)
        {
            await InvalidateQuery(string.Join("|", keys));
        }

        private async Task InvalidateQuery(string key)
        {
            await Refetch(key);
        }

        private async Task<object> FetchAndCacheAsync<T>(string key, Func<Task<T>> fetchFunction)
        {
            Console.WriteLine($"Fetching {key}");
            if (_fetchTasks.TryGetValue(key, out var existingTask))
            {
                return await existingTask;
            }

            var fetchTask = FetchAndCacheInternalAsync(key, fetchFunction);
            _fetchTasks[key] = fetchTask;
            var result = await fetchTask;
            _fetchTasks.Remove(key);
            return result;
        }

        private async Task<object> FetchAndCacheInternalAsync<T>(string key, Func<Task<T>> fetchFunction)
        {
            var value = await fetchFunction();
            _cache[key] = (value, DateTime.UtcNow);
            NotifySubscribers(key, value);
            return value;
        }

        private void NotifySubscribers(string key, object value)
        {
            _messenger.Send(key, value);
        }

        private async Task Refetch(string? key)
        {
            Console.WriteLine($"Refetching {key}");
            if (_fetchFunctions.TryGetValue(key, out var fetchFunction))
            {
                _ = await FetchAndCacheAsync(key, fetchFunction);
            }
        }

        private async Task CleanupExpiredCacheEntries()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine("Cache cleanup started");
                await Task.Delay(_cleanupInterval, _cancellationTokenSource.Token);

                var keysToRemove = _cache
                    .Where(x => DateTime.UtcNow - x.Value.Timestamp >= _cacheLifetime)
                    .Select(y => y.Key)
                    .ToList();
                foreach (var key in keysToRemove)
                {
                    if (_messenger.HasSubscribers(key))
                    {
                        await Refetch(key);
                    }
                    else
                    {
                        Console.WriteLine($"Removing {key}");
                        _cache.Remove(key);
                        _fetchFunctions.Remove(key);
                    }
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cleanupTask.Wait();
            _cancellationTokenSource.Dispose();
        }
    }
}
