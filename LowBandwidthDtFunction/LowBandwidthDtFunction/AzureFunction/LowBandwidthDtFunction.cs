// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LowBandwidthDtFunction.AzureFunction
{
    public class LowBandwidthDtFunction
    {
        private readonly ILogger<LowBandwidthDtFunction> _logger;

        public LowBandwidthDtFunction(ILogger<LowBandwidthDtFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(LowBandwidthDtFunction))]
        public void Run([EventGridTrigger] string cloudEvent)
        {
            _logger.LogInformation("Received a message from the EventGrid: {cloudEvent}", cloudEvent);
        }
    }
}
