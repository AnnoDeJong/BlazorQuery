using FluentAssertions;
using Xunit;

namespace BlazorQuery.Tests
{
    public class CacheProviderTests
    {
        [Fact]
        public async Task UseQuery_ShouldReturnCachedValue_WhenCalledWithinCacheLifetime()
        {
            // Arrange
            var cacheProvider = new CacheProvider(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
            var key = "testKey";
            var initialValue = "initialValue";
            var newValue = "newValue";
            var stringListIterator = new StringListIterator(new List<string> { initialValue, newValue });

            var result3 = string.Empty;
            cacheProvider.Subscribe(key, (k, v) => result3 = (string)v);

            // Act
            var result1 = await cacheProvider.UseQuery(key, FetchData);
            await Task.Delay(TimeSpan.FromSeconds(5));
            var result2 = await cacheProvider.UseQuery(key, FetchData);
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for async fetch to complete
            // Assert
            result1.Should().Be(initialValue);
            result2.Should().Be(initialValue);
            result3.Should().Be(initialValue);

            async Task<string> FetchData()
            {
                await Task.Delay(10); // Simulate a data fetch
                //return DateTime.Now.ToString();

                return await stringListIterator.GetNextString();
            }
        }

        [Fact]
        public async Task UseQuery_ShouldRefetchValue_WhenCalledAfterCacheLifetime()
        {
            // Arrange
            var cacheProvider = new CacheProvider(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
            var key = "testKey";
            var initialValue = "initialValue";
            var newValue = "newValue";

            var stringListIterator = new StringListIterator(new List<string> { initialValue, newValue });

            var result3 = string.Empty;
            cacheProvider.Subscribe(key, (k, v) => result3 = (string)v);

            // Act
            var result1 = await cacheProvider.UseQuery(key, FetchData);
            await Task.Delay(TimeSpan.FromSeconds(2)); // Wait for cache to become stale
            var result2 = await cacheProvider.UseQuery(key, FetchData);

            await Task.Delay(TimeSpan.FromSeconds(2)); // Wait for async fetch to complete

            // Assert
            result1.Should().Be(initialValue);
            result2.Should().Be(initialValue);
            result3.Should().Be(newValue);

            async Task<string> FetchData()
            {
                await Task.Delay(10); // Simulate a data fetch
                //return DateTime.Now.ToString();

                return await stringListIterator.GetNextString();
            }
        }

        [Fact]
        public async Task UseQuery_ShouldReturnNewValue_WhenCacheIsInvalidated()
        {
            // Arrange
            var cacheProvider = new CacheProvider(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
            var key = "testKey";
            var initialValue = "initialValue";
            var newValue = "newValue";

            var stringListIterator = new StringListIterator(new List<string> { initialValue, newValue });

            var result2 = string.Empty;
            cacheProvider.Subscribe(key, (k, v) => result2 = (string)v);

            // Act
            var result1 = await cacheProvider.UseQuery(key, FetchData);
            await cacheProvider.InvalidateQuery(new[] { key });
           

            // Assert
            result1.Should().Be(initialValue);
            result2.Should().Be(newValue);

            async Task<string> FetchData()
            {
                await Task.Delay(1); // Simulate a data fetch
                //return DateTime.Now.ToString();

                return await stringListIterator.GetNextString();
            }
        }


    }
    public class StringListIterator
    {
        private List<string> _strings;
        private int _currentIndex;

        public StringListIterator(List<string> strings)
        {
            _strings = strings;
            _currentIndex = 0;
        }

        public async Task<string> GetNextString()
        {
            if (_currentIndex >= _strings.Count)
            {
                _currentIndex = 0;
            }

            string nextString = _strings[_currentIndex];
            _currentIndex++;

            return await Task.FromResult(nextString);
        }
    }
}