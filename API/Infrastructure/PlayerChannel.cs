using Contracts;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Reflection;

namespace API.Infrastructure
{
    public class PlayerChannel
    {
        private readonly WebSocket webSocket;

        public event Action<Message?>? OnMessage;
        public event Action? OnClose;

        public PlayerChannel(WebSocket webSocket)
        {
            this.webSocket = webSocket;
        }

        public async Task Listen(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];

            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), cancellationToken);

            while (!receiveResult.CloseStatus.HasValue)
            {
                var messageString = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var message = JsonSerializer.Deserialize<Message>(messageString);

                OnMessage?.Invoke(message);

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);
            }
            OnClose?.Invoke();

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }

        public async Task SendMessage<T>(T payload)
        {
            var sendBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Message
            {
                Payload = JsonSerializer.Serialize(payload),
                Type = payload.GetType().GetCustomAttribute<MessageTypeAttribute>().Name
            }));

            await webSocket.SendAsync(
                new ArraySegment<byte>(sendBuffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}
