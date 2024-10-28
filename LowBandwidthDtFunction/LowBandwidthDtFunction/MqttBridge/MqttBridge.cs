using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowBandwidthDtFunction.MqttBridge
{
    internal sealed class MqttBridge
    {
        private readonly ILogger _logger;

        private readonly string _iotHubDeviceConnectionString;
        
        private readonly string _mqttUsername;
        private readonly string _mqttPassword;
        private readonly string _mqttBrokerUri;
        private readonly string _mqttBrokerPort;
        private readonly string _mqttSubscriptionTopic;
        private readonly string _mqttQualityOfServiceLevel;

        internal MqttBridge(ILogger<MqttBridge> logger)
        {
            _logger = logger;

            _iotHubDeviceConnectionString = Environment.GetEnvironmentVariable("IotHubDeviceConnectionString") ??
                throw new ArgumentNullException("The IotHubDeviceConnectionString environment variable is null.");

            _mqttUsername = Environment.GetEnvironmentVariable("MqttUsername") ??
                throw new ArgumentNullException("The MqttUsername environment variable is null.");
            _mqttPassword = Environment.GetEnvironmentVariable("MqttPassword") ??
                throw new ArgumentNullException("The MqttPassword environment variable is null.");
            _mqttBrokerUri = Environment.GetEnvironmentVariable("MqttBrokerUri") ??
                throw new ArgumentNullException("The MqttBrokerUri environment variable is null.");
            _mqttBrokerPort = Environment.GetEnvironmentVariable("MqttBrokerPort") ??
                throw new ArgumentNullException("The MqttBrokerPort environment variable is null.");
            _mqttSubscriptionTopic = Environment.GetEnvironmentVariable("MqttSubscriptionTopic") ??
                throw new ArgumentNullException("The MqttSubscriptionTopic environment variable is null.");
            _mqttQualityOfServiceLevel = Environment.GetEnvironmentVariable("MqttQualityOfServiceLevel") ??
                throw new ArgumentNullException("The MqttQualityOfServiceLevel environment variable is null.");

            var mqttClientFactory = new MqttFactory();

            var clientOptions = mqttClientFactory.CreateClientOptionsBuilder()
                .WithTcpServer(_mqttBrokerUri, int.Parse(_mqttBrokerPort))
                .WithTlsOptions(options => { })
                .WithCredentials(_mqttUsername, _mqttPassword)
                .WithWillQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)int.Parse(_mqttQualityOfServiceLevel))
                .Build();

            var subscriptionOptions = mqttClientFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(filter =>
                    filter.WithTopic(_mqttSubscriptionTopic))
                .Build();

            var mqttClient = mqttClientFactory.CreateMqttClient();

            mqttClient.InspectPacketAsync += InspectPacketEventHandlerAsync;
            mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedEventHandlerAsync;
            mqttClient.ConnectedAsync += ConnectedEventHandlerAsync;
            mqttClient.DisconnectedAsync += DisconnectedEventHandlerAsync;

            _ = mqttClient.ConnectAsync(clientOptions).Result;
            _ = mqttClient.SubscribeAsync(subscriptionOptions).Result;
        }

        private Task InspectPacketEventHandlerAsync(InspectMqttPacketEventArgs e)
        {
            _logger.LogInformation("Detected a packet of type: {packetType}", e.Direction);

            return Task.CompletedTask;
        }

        private async Task ApplicationMessageReceivedEventHandlerAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage == null)
                return;

            _logger.LogInformation("Received application message with contents: {message}", e.ApplicationMessage.ConvertPayloadToString().Replace("\n", ""));

            await SendDataToIotHub(e.ApplicationMessage.PayloadSegment);
        }

        private Task ConnectedEventHandlerAsync(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("Client connected with result: {connectionResult}", e.ConnectResult);

            return Task.CompletedTask;
        }

        private Task DisconnectedEventHandlerAsync(MqttClientDisconnectedEventArgs e)
        {
            string disconnectReason;

            if (e.Exception != null)
                disconnectReason = e.Exception.Message;
            else
                disconnectReason = e.ReasonString;

            _logger.LogInformation("Client disconnected with reason: {disconnectReason}", disconnectReason);

            return Task.CompletedTask;
        }

        private async Task SendDataToIotHub(ArraySegment<byte> mqttMessage)
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(_iotHubDeviceConnectionString);

            var messageToSend = new Message(mqttMessage.Array);

            try
            {
                await deviceClient.SendEventAsync(messageToSend);

                _logger.LogInformation("Sent telemetry to Azure IoT Hub.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception while sending telemetry to Iot Hub: {exceptionMessage}", exception.Message);
            }
        }
    }
}
