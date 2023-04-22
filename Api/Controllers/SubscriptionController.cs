using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly IClusterClient _client;

    public SubscriptionController(IClusterClient client)
    {
        _client = client;
    }
    
    [HttpPost("subscribe")]
    public ActionResult Subscribe(SubscriptionDetails details)
    {
        var eventSink = _client.GetGrain<ITopic>(details.Queue);
        eventSink.Subscribe(details);
        return Ok();
    }
    
    [HttpPost("unsubscribe")]
    public ActionResult Unsubscribe(SubscriptionDetails details)
    {
        var eventSink = _client.GetGrain<ITopic>(details.Queue);
        eventSink.Unsubscribe(details);
        return Ok();
    }
}
