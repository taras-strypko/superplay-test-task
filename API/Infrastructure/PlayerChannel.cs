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
            var readResult = await ReadMessageByChunks(webSocket, cancellationToken);

            while (!readResult.ReceiveResult.CloseStatus.HasValue)
            {
                var messageString = Encoding.UTF8.GetString(readResult.Data, 0, readResult.Data.Length);
                var message = JsonSerializer.Deserialize<Message>(messageString);

                OnMessage?.Invoke(message);

                readResult = await ReadMessageByChunks(webSocket, cancellationToken);
            }

            OnClose?.Invoke();

            await webSocket.CloseAsync(
                readResult.ReceiveResult.CloseStatus!.Value,
                readResult.ReceiveResult.CloseStatusDescription,
                cancellationToken);
        }

        private async Task<(WebSocketReceiveResult ReceiveResult, byte[] Data)> ReadMessageByChunks(WebSocket webSocket, CancellationToken cancellationToken)
        {
            const int MESSAGE_MAX_SIZE = 1024 * 16; // let's define some upper limit for message size, as with large messages the game might become slow.
            const int CHUNK_SIZE = 1024; 
            var framePayload = new ArraySegment<byte>(new byte[MESSAGE_MAX_SIZE]);
            var buffer = new ArraySegment<byte>(new byte[CHUNK_SIZE]);

            WebSocketReceiveResult receiveResult;
            int framePayloadSize = 0;
            do
            {
                receiveResult = await webSocket.ReceiveAsync(buffer, cancellationToken);

                var data = buffer.Slice(0, receiveResult.Count);
                data.CopyTo(framePayload.Slice(framePayloadSize, receiveResult.Count));
                framePayloadSize += receiveResult.Count;

            } while (!receiveResult.EndOfMessage);

            return (receiveResult, framePayload[0..framePayloadSize].ToArray());
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
