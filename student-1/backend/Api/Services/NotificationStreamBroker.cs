using System.Collections.Concurrent;
using System.Threading.Channels;
using Api.DTOs;

namespace Api.Services;

public interface INotificationStreamBroker
{
    ChannelReader<NotificationDto> Subscribe(Guid studentId, CancellationToken cancellationToken);
    void Unsubscribe(Guid studentId, Channel<NotificationDto> channel);
    ValueTask PublishAsync(NotificationDto notification, CancellationToken cancellationToken = default);
}

public sealed class NotificationStreamBroker : INotificationStreamBroker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<Channel<NotificationDto>>> _subscriptions = new();

    public ChannelReader<NotificationDto> Subscribe(Guid studentId, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<NotificationDto>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });

        _subscriptions.AddOrUpdate(
            studentId,
            _ => [channel],
            (_, bag) =>
            {
                bag.Add(channel);
                return bag;
            });

        cancellationToken.Register(() => Unsubscribe(studentId, channel));

        return channel.Reader;
    }

    public void Unsubscribe(Guid studentId, Channel<NotificationDto> channel)
    {
        channel.Writer.TryComplete();
        if (_subscriptions.TryGetValue(studentId, out var bag))
        {
            var remaining = new ConcurrentBag<Channel<NotificationDto>>(bag.Where(c => c != channel));
            _subscriptions[studentId] = remaining;
        }
    }

    public async ValueTask PublishAsync(NotificationDto notification, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(notification.StudentId, out var channels))
        {
            foreach (var channel in channels)
            {
                await channel.Writer.WriteAsync(notification, cancellationToken);
            }
        }
    }
}
