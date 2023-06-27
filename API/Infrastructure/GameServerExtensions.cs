using Contracts;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using System.Text.Json;

namespace API.Infrastructure
{
    public static class GameServerExtensions
    {
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

        private static async Task<object> ExecuteOperationHandler(PlayerChannel channel, Message message, IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(GameServerExtensions));
            if (message == null)
            {
                return new ErrorResponse
                {
                    Code = Errors.INVALID_REQUEST_MESSAGE.ToString()
                };
            }

            var operationContext = new OperationContext() { PlayerId = message.PlayerId, Channel = channel };

            Type openGenericType = typeof(IMessageHandler<,>);
            var allMessageHandlers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericType))
                .ToArray();

            foreach (var messageHandler in allMessageHandlers)
            {
                var messageHandlerInterfaceDeclaration = messageHandler.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericType);
                var requestType = messageHandlerInterfaceDeclaration.GetGenericArguments().First();
                var responseType = messageHandlerInterfaceDeclaration.GetGenericArguments().Last();
                var operationNameAttribute = requestType.GetCustomAttribute<MessageTypeAttribute>();

                if (operationNameAttribute.Name == message.Type)
                {
                    Type genericType = typeof(IMessageHandler<,>).MakeGenericType(requestType, responseType);
                    var handler = serviceProvider.GetService(genericType);
                    if (handler != null)
                    {
                        if (messageHandler.GetCustomAttribute<AuthorizeAttribute>() != null && !message.PlayerId.HasValue)
                        {
                            return new ErrorResponse
                            {
                                Code = Errors.PLAYER_NOT_LOGGED_IN.ToString()
                            };
                        }

                        MethodInfo handleMethod = handler.GetType().GetMethod("Handle")!;

                        try
                        {
                            object task = handleMethod.Invoke(handler, new object[] { JsonSerializer.Deserialize(message.Payload, requestType), operationContext })!;
                            await (Task)task;
                            PropertyInfo property = task.GetType().GetProperty("Result");
                            var result = property.GetValue(task);

                            return result;
                        }
                        catch (OperationException ex)
                        {
                            logger.LogError(ex, "Error executing operation");
                            return new ErrorResponse
                            {
                                Code = ex.Code
                            };
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error executing operation");
                            while (ex.InnerException != null)
                            {
                                if (ex.InnerException is OperationException)
                                {
                                    return new ErrorResponse
                                    {
                                        Code = ((OperationException)ex.InnerException).Code
                                    };
                                }
                                ex = ex.InnerException;
                            }

                            return new ErrorResponse
                            {
                                Code = Errors.INTERNAL_SERVER_ERROR.ToString()
                            };
                        }
                    }
                    break;
                }
            }
            logger.LogError($"Execution error operation - handler for message type {message.Type} not found");
            return new ErrorResponse { Code = Errors.OPERATION_NOT_IMPLEMENTED.ToString() };
        }
    }
}
