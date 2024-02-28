﻿using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Text;

namespace ModuleA;

internal class ModuleBackgroundService : BackgroundService
{
    private int _counter;
    private ModuleClient? _moduleClient;
    private CancellationToken _cancellationToken;
    private readonly ILogger<ModuleBackgroundService> _logger;

    public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        MqttTransportSettings mqttSetting = new(TransportType.Mqtt_Tcp_Only);
        ITransportSettings[] settings = { mqttSetting };

        // Open a connection to the Edge runtime
        _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

        // Reconnect is not implented because we'll let docker restart the process when the connection is lost
        _moduleClient.SetConnectionStatusChangesHandler((status, reason) => 
            _logger.LogWarning("Connection changed: Status: {status} Reason: {reason}", status, reason));

        await _moduleClient.OpenAsync(cancellationToken);

        _logger.LogInformation("IoT Hub module client initialized.");

        await SendEvents(cancellationToken);
    }

    async Task SendEvents(CancellationToken cancellationToken)
    {
        int messageDelay = 10 * 1000;
        int count = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            string messageString = "helloworld";
            using Message message = new(Encoding.UTF8.GetBytes(messageString));
            message.ContentEncoding = "utf-8";
            message.ContentType = "text/plain";
            message.Properties.Add("sequenceNumber", count.ToString());
            _logger.LogInformation($"Send message: {count}, Body: [{messageString}]");
            await _moduleClient!.SendEventAsync("output1", message, cancellationToken);
            count++;
            await Task.Delay(messageDelay, cancellationToken);
        }
    }
}
