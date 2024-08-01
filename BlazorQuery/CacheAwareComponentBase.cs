using Microsoft.AspNetCore.Components;

namespace BlazorQuery
{
    public abstract class CacheAwareComponentBase : ComponentBase, IDisposable
    {
        [Inject]
        protected CacheProvider CacheProvider { get; set; }

        private readonly Dictionary<string, Delegate> _subscriptions = new();
        private readonly Dictionary<string, Delegate> _valueSetters = new();

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected async Task<T> UseQuery<T>(string key, Func<Task<T>> fetchFunction, Action<T> valueSetter)
        {
            // Store the reference to the value setter
            _valueSetters[key] = valueSetter;

            // Always subscribe to HandleCacheUpdated
            Action<string, object> handler = (k, v) => HandleCacheUpdated<T>(k, v);
            _subscriptions[key] = handler;
            CacheProvider.Subscribe(key, handler);

            var result = await CacheProvider.UseQuery(key, fetchFunction);
            valueSetter(result);
            return result;
        }

        private void HandleCacheUpdated<T>(string key, object value)
        {
            if (_valueSetters.TryGetValue(key, out var valueSetter))
            {
                var typedSetter = (Action<T>)valueSetter;
                typedSetter((T)value);
            }
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                CacheProvider.Unsubscribe(subscription.Key, (Action<string, object>)subscription.Value);
            }
            _subscriptions.Clear();
            _valueSetters.Clear();
        }
    }
}
