using Orleans;

namespace Shared;

public interface ISubscription : IGrainWithStringKey
{
    Task Publish<T>(T message);
    Task TriggerSending();
}


public interface IMessage : IGrainWithGuidKey
{
    Task<string> GetMessage();
    Task Clear();
    Task Initialize(string message);
}

public interface ITopic : IGrainWithStringKey
{
    Task Subscribe(SubscriptionDetails details);
    Task Unsubscribe(SubscriptionDetails details);
}

public record SubscriptionDetails(string Queue, string Subscriber);

