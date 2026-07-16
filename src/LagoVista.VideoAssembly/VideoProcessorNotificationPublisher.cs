using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.VideoAssembly
{
    public sealed class VideoProcessorNotificationSettings
    {
        public bool Enabled { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Exchange { get; set; } = "notif.publish";
        public string RoutingKey { get; set; } = "notif-publish";
        public string ClientName { get; set; } = "VideoProcessorNotificationPublisher";
    }

    public sealed class VideoProcessorNotificationPublisher : IAsyncDisposable
    {
        private readonly VideoProcessorNotificationSettings _settings;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private readonly JsonSerializerOptions _jsonOptions;
        private IConnection _connection;
        private IChannel _channel;

        public VideoProcessorNotificationPublisher(VideoProcessorNotificationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task TryPublishAsync<TPayload>(string channelId, string text, TPayload payload, CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled) return;

            try
            {
                await PublishAsync(channelId, text, payload, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Rabbit notification publish failed: {ex.Message}");
            }
        }

        public async Task PublishAsync<TPayload>(string channelId, string text, TPayload payload, CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled) return;
            if (String.IsNullOrWhiteSpace(channelId)) throw new ArgumentNullException(nameof(channelId));

            var channel = await GetChannelAsync(cancellationToken);
            var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
            var notification = new VideoProcessorNotification
            {
                MessageId = Guid.NewGuid().ToString("N"),
                DateStamp = DateTime.UtcNow.ToString("O"),
                Channel = VideoProcessorNotificationHeader.Create("org", "Org"),
                Verbosity = VideoProcessorNotificationHeader.Create("normal", "Normal"),
                ChannelId = channelId,
                Text = text,
                PayloadType = typeof(TPayload).Name,
                Payload = payloadJson
            };

            var json = JsonSerializer.Serialize(notification, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);
            await channel.BasicPublishAsync(_settings.Exchange, _settings.RoutingKey, body: body, cancellationToken: cancellationToken);
        }

        private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen) return _channel;

            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen) return _channel;
                await DisposeConnectionAsync();

                Exception lastException = null;
                var delay = TimeSpan.FromSeconds(1);

                for (var attempt = 1; attempt <= 5; attempt++)
                {
                    try
                    {
                        var factory = new ConnectionFactory
                        {
                            ClientProvidedName = _settings.ClientName,
                            HostName = _settings.HostName,
                            Port = _settings.Port,
                            VirtualHost = String.IsNullOrWhiteSpace(_settings.VirtualHost) ? "/" : _settings.VirtualHost,
                            UserName = _settings.UserName,
                            Password = _settings.Password,
                            AutomaticRecoveryEnabled = true,
                            TopologyRecoveryEnabled = true
                        };

                        _connection = await factory.CreateConnectionAsync(cancellationToken);
                        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                        return _channel;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        await DisposeConnectionAsync();
                        if (attempt < 5) await Task.Delay(delay, cancellationToken);
                        delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                    }
                }

                throw new InvalidOperationException($"Could not connect to RabbitMQ host '{_settings.HostName}'.", lastException);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task DisposeConnectionAsync()
        {
            if (_channel != null)
            {
                try
                {
                    await _channel.DisposeAsync();
                }
                catch
                {
                }

                _channel = null;
            }

            if (_connection != null)
            {
                try
                {
                    await _connection.DisposeAsync();
                }
                catch
                {
                }

                _connection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                await DisposeConnectionAsync();
            }
            finally
            {
                _connectionLock.Release();
                _connectionLock.Dispose();
            }
        }
    }

    internal sealed class VideoProcessorNotification
    {
        [JsonPropertyName("messageId")]
        public string MessageId { get; set; }

        [JsonPropertyName("dateStamp")]
        public string DateStamp { get; set; }

        [JsonPropertyName("channel")]
        public VideoProcessorNotificationHeader Channel { get; set; }

        [JsonPropertyName("verbosity")]
        public VideoProcessorNotificationHeader Verbosity { get; set; }

        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("payloadType")]
        public string PayloadType { get; set; }

        [JsonPropertyName("payloadJSON")]
        public string Payload { get; set; }
    }

    internal sealed class VideoProcessorNotificationHeader
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("_t")]
        public string Indexer { get; set; } = "eh";

        public static VideoProcessorNotificationHeader Create(string id, string value)
        {
            return new VideoProcessorNotificationHeader { Id = id, Key = value, Text = value };
        }
    }
}
