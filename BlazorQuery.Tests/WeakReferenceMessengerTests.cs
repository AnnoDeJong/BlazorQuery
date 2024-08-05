using Xunit;
using FluentAssertions;

namespace BlazorQuery.Tests
{
    public class WeakReferenceMessengerTests
    {
        [Fact]
        public void Subscribe_ShouldAddSubscriber()
        {
            // Arrange
            var messenger = new WeakReferenceMessenger();
            var key = "testKey";
            Action<string, object> subscriber = (k, v) => { };

            // Act
            messenger.Subscribe(key, subscriber);

            // Assert
            messenger.HasSubscribers(key).Should().BeTrue();
        }

        [Fact]
        public void Unsubscribe_ShouldRemoveSubscriber()
        {
            // Arrange
            var messenger = new WeakReferenceMessenger();
            var key = "testKey";
            Action<string, object> subscriber = (k, v) => { };

            // Act
            messenger.Subscribe(key, subscriber);
            messenger.Unsubscribe(key, subscriber);

            // Assert
            messenger.HasSubscribers(key).Should().BeFalse();
        }

        [Fact]
        public void Send_ShouldInvokeSubscriber()
        {
            // Arrange
            var messenger = new WeakReferenceMessenger();
            var key = "testKey";
            var received = false;
            Action<string, object> subscriber = (k, v) => { received = true; };

            // Act
            messenger.Subscribe(key, subscriber);
            messenger.Send(key, new object());

            // Assert
            received.Should().BeTrue();
        }

        [Fact]
        public void Send_ShouldNotInvokeUnsubscribedSubscriber()
        {
            // Arrange
            var messenger = new WeakReferenceMessenger();
            var key = "testKey";
            var received = false;
            Action<string, object> subscriber = (k, v) => { received = true; };

            // Act
            messenger.Subscribe(key, subscriber);
            messenger.Unsubscribe(key, subscriber);
            messenger.Send(key, new object());

            // Assert
            received.Should().BeFalse();
        }
    }
}