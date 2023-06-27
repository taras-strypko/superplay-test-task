using Contracts;
using Domain;
using Serilog;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Client
{
    public class GameServerClient
    {
        private readonly string serverUrl;
        private readonly ILogger logger;
        private ClientWebSocket webSocketClient;
        private Task? listenTask;

        public event Action<LoginResponse>? OnLoggedIn;
        public event Action<UpdateResourceResponse>? OnResourcesUpdated;
        public event Action<SendGiftResponse>? OnGiftSent;
        public event Action<GiftReceived>? OnGiftReceived;
        public event Action<ErrorResponse>? OnError;
        public long? PlayerId { get; private set; }

        public GameServerClient(string serverUrl, ILogger logger)
        {
            this.serverUrl = !string.IsNullOrEmpty(serverUrl) ? serverUrl : throw new ArgumentNullException(nameof(serverUrl));
            webSocketClient = new ClientWebSocket();
            this.logger = logger;
        }

        public async Task Connect()
        {
            var serverUri = new Uri(serverUrl);

            await webSocketClient.ConnectAsync(serverUri, CancellationToken.None);
            listenTask = Task.Factory.StartNew(ListenForMessages);
        }

        public async Task Login(Guid deviceId)
        {
            var firstUserLoginRequest = new LoginRequest
            {
                DeviceId = deviceId
            };

            await Send(firstUserLoginRequest, CancellationToken.None);
        }

        public async Task UpdateResources(ResourceType type, int amount)
        {
            var updateResourceRequest = new UpdateResourceRequest
            {
                ResourceType = type,
                ResourceValue = amount
            };

            await Send(updateResourceRequest, CancellationToken.None);
        }

        public async Task SendGift(long friendId, ResourceType type, int amount)
        {
            var sendGiftRequest = new SendGiftRequest
            {
                FriendPlayerId = friendId,
                Type = type,
                Amount = amount
            };

            await Send(sendGiftRequest, CancellationToken.None);
        }

        private async Task Send<T>(T req, CancellationToken cancellationToken)
        {
            var messageType = typeof(T)?.GetCustomAttribute<MessageTypeAttribute>()?.Name;

            if (messageType == null)
            {
                throw new ArgumentException($"{nameof(messageType)} has no defined message type attribute");
            }

            var message = new Message
            {
                Type = messageType,
                Payload = JsonSerializer.Serialize(req),
                PlayerId = PlayerId
            };
            var sendBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await webSocketClient.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task ListenForMessages()
        {
            var buffer = new byte[4 * 1024];

            try
            {
                while (true)
                {
                    WebSocketReceiveResult result = await webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // The server closed the connection
                        await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed connection", CancellationToken.None);
                        break;
                    }

                    var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = Parse<Message>(messageString);

                    switch (message.Type)
                    {
                        case LoginResponse.MESSAGE_TYPE:
                            {
                                var payload = Parse<LoginResponse>(message.Payload);
                                PlayerId = payload.PlayerId;
                                OnLoggedIn?.Invoke(payload);
                                break;
                            }
                        case UpdateResourceResponse.MESSAGE_TYPE:
                            {
                                var payload = Parse<UpdateResourceResponse>(message.Payload);
                                OnResourcesUpdated?.Invoke(payload);
                                break;
                            }

                        case SendGiftResponse.MESSAGE_TYPE:
                            {
                                var payload = Parse<SendGiftResponse>(message.Payload);
                                OnGiftSent?.Invoke(payload);
                                break;
                            }
                        case GiftReceived.MESSAGE_TYPE:
                            {
                                var payload = Parse<GiftReceived>(message.Payload);
                                
                                OnGiftReceived?.Invoke(payload);
                                break;
                            }
                        case ErrorResponse.MESSAGE_TYPE:
                            {
                                var payload = Parse<ErrorResponse>(message.Payload);

                                OnError?.Invoke(payload);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                logger.Error($"Exception while listening: {ex}");
                throw;
            }
        }

        private T Parse<T>(string data)
        {
            var res = JsonSerializer.Deserialize<T>(data);
            return res == null ? throw new Exception("Response cannot be parsed") : res;
        }
    }
}
