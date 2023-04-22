using Orleans.Runtime;
using Shared;

namespace Server;

public class Subscription : Grain, ISubscription
{
    private readonly HttpClient _client;
    private readonly IPersistentState<MessageQueue> _messageQueue;

    public Subscription(HttpClient client,
        [PersistentState("messageQueue", "store")] IPersistentState<MessageQueue> messageQueue)
    {
        _client = client;
        _messageQueue = messageQueue;
    }

    /// <inheritdoc />
    public async Task Publish<T>(T message)
    {
        // add to queue
        if (message != null)
        {
            var messageGrain = GrainFactory.GetGrain<IMessage>(Guid.NewGuid());
            await messageGrain.Initialize(message.ToString());
            _messageQueue.State.Messages.Enqueue(messageGrain);
            await _messageQueue.WriteStateAsync();
        }

        var asReference = this.AsReference<ISubscription>();
        _ = asReference.TriggerSending();
    }

    /// <inheritdoc />
    public async Task TriggerSending()
    {
        if (_messageQueue.State.Messages.TryDequeue(out var message))
        {
            var data = message.GetMessage();
            // _client.Send(data);
            await _messageQueue.WriteStateAsync();
            message.Clear();
            _ = TriggerSending(); // recursivly trigger sending again
        }
        
    }
}

public class MessageQueue
{
    public Queue<IMessage> Messages { get; set; } = new();
}

public class Message : Grain, IMessage
{
    private readonly IPersistentState<string> _state;

    public Message([PersistentState("message", "store")] IPersistentState<string> state)
    {
        _state = state;
    }
    
    /// <inheritdoc />
    public async Task<string> GetMessage()
    {
        return _state.State;
    }

    /// <inheritdoc />
    public async Task Clear()
    {
        await _state.ClearStateAsync();
    }

    /// <inheritdoc />
    public async Task Initialize(string message)
    {
        _state.State = message;
        await _state.WriteStateAsync();
    }
}


public class Topic : Grain, ITopic
{
    private readonly IPersistentState<TopicState> _state;

    public Topic([PersistentState("topic", "store")] IPersistentState<TopicState> state)
    {
        _state = state;
    }
    
    /// <inheritdoc />
    public async Task Subscribe(SubscriptionDetails details)
    {
        var subscription = GrainFactory.GetGrain<ISubscription>(details.Subscriber);
        _state.State.Subscriptions.TryAdd(details.Subscriber, subscription);
        await _state.WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task Unsubscribe(SubscriptionDetails details)
    {
        _state.State.Subscriptions.Remove(details.Subscriber);
        await _state.WriteStateAsync();
    }

    public async Task Publish<T>(T message)
    {
        var _ = _state.State.Subscriptions.Values.Select(x => x.Publish(message)).ToArray();
    }
}

public class TopicState
{
    public Dictionary<string, ISubscription> Subscriptions { get; set; } = new();
}