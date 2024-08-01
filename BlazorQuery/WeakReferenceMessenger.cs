namespace BlazorQuery
{
    public class WeakReferenceMessenger
    {
        private readonly Dictionary<string, List<WeakReference<Action<string, object>>>> _subscribersByKey = new();

        public void Subscribe(string key, Action<string, object> subscriber)
        {
            Console.WriteLine($"Subscribing {subscriber.Method.Name} to {key}");
            if (!_subscribersByKey.ContainsKey(key))
            {
                _subscribersByKey[key] = new List<WeakReference<Action<string, object>>>();
            }
            _subscribersByKey[key].Add(new WeakReference<Action<string, object>>(subscriber));
        }

        public void Unsubscribe(string key, Action<string, object> subscriber)
        {
            if (_subscribersByKey.ContainsKey(key))
            {
                Console.WriteLine($"Unsubscribing {_subscribersByKey[key].Count} subscribers");
                _subscribersByKey[key].RemoveAll(wr => wr.TryGetTarget(out var target) && target == subscriber);
                if (_subscribersByKey[key].Count == 0)
                {
                    _subscribersByKey.Remove(key);
                }
            }

            var derp = _subscribersByKey.Select(x => $"{x.Key}: {x.Value.Count}");
            var derp2 = string.Join(", ", derp);
            Console.WriteLine($"Keys subscribed to: {derp2}");
        }

        public void Send(string key, object value)
        {
            if (_subscribersByKey.ContainsKey(key))
            {
                var subscribers = _subscribersByKey[key];
                foreach (var weakReference in subscribers)
                {
                    if (weakReference.TryGetTarget(out var subscriber))
                    {
                        subscriber(key, value);
                    }
                }
                subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
                if (subscribers.Count == 0)
                {
                    _subscribersByKey.Remove(key);
                }
            }
        }

        public bool HasSubscribers(string key)
        {
            return _subscribersByKey.ContainsKey(key) && _subscribersByKey[key].Count > 0;
        }
    }
}
