using Contracts;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using System.Text.Json;

namespace API.Infrastructure
{
    public static class GameServerExtensions
    {
        record HandlerMetadata(Type RequestType, Type HandlerType);

        private static Dictionary<string, HandlerMetadata> handlers = new Dictionary<string, HandlerMetadata>();
        public static IServiceCollection RegisterMessageHandler<TReq, TRes, THandlerImplementation>(this IServiceCollection serviceCollection)
            where THandlerImplementation: class, IMessageHandler<TReq, TRes>
        {
            var messageType = typeof(TReq).GetCustomAttribute<MessageTypeAttribute>()?.Name;
             
            if (messageType == null)
            {
                throw new Exception("Message must have MessageTypeAttribute specified");  
            }

            handlers.Add(messageType, new HandlerMetadata(typeof(TReq), typeof(IMessageHandler<TReq, TRes>)));
            serviceCollection.AddScoped<IMessageHandler<TReq, TRes>, THandlerImplementation>();

            return serviceCollection;
        }

        public static IApplicationBuilder UseGameServer(this IApplicationBuilder app)
        {
            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var channel = new PlayerChannel(webSocket);

                        channel.OnMessage += async (msg) =>
                        {
                            var res = await ExecuteOperationHandler(channel, msg, context.RequestServices);
                            await channel.SendMessage(res);
                        };

                        await channel.Listen(CancellationToken.None);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await next(context);
                }
            });

            return app;
        }

        private static async Task<object?> ExecuteOperationHandler(PlayerChannel channel, Message message, IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(GameServerExtensions));
            if (message == null)
            {
                return new ErrorResponse(Errors.INVALID_REQUEST_MESSAGE.ToString());
            }

            var operationContext = new OperationContext() { PlayerId = message.PlayerId, Channel = channel };

            var handlerFound = handlers.TryGetValue(message.Type, out var handlerMetadata);

            if (!handlerFound)
            {
                logger.LogError($"Execution error operation - handler for message type {message.Type} not found");
                return new ErrorResponse(Errors.OPERATION_NOT_IMPLEMENTED.ToString());
            }
            var handlerType = handlerMetadata!.HandlerType;
            var handler = serviceProvider.GetService(handlerType) as IMessageHandler;

            if (handler == null)
            {
                logger.LogError($"Execution error operation - handler for message type {message.Type} not registered");
                return new ErrorResponse(Errors.OPERATION_NOT_IMPLEMENTED.ToString());
            }
           
            if (handlerType.GetCustomAttribute<AuthorizeAttribute>() != null && !message.PlayerId.HasValue)
            {
                return new ErrorResponse(Errors.PLAYER_NOT_LOGGED_IN.ToString());
            }

            try
            {
                var deserializedRequest = JsonSerializer.Deserialize(message.Payload, handlerMetadata!.RequestType);
                if (deserializedRequest == null)
                {
                    return new ErrorResponse(Errors.INVALID_REQUEST_MESSAGE.ToString());
                }

                return await handler.Handle(deserializedRequest, operationContext);
            }
            catch (OperationException ex)
            {
                logger.LogError(ex, "Error executing operation");
                return new ErrorResponse(ex.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing operation");
                while (ex.InnerException != null)
                {
                    if (ex.InnerException is OperationException)
                    {
                        return new ErrorResponse(((OperationException)ex.InnerException).Code);
                    }
                    ex = ex.InnerException;
                }

                return new ErrorResponse(Errors.INTERNAL_SERVER_ERROR.ToString());
            }
        }
    }
}
