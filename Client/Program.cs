using Client;
using Serilog.Events;
using Serilog;
using Domain;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

try
{
    var client = new GameServerClient("wss://localhost:7030/ws", Log.Logger);
    var deviceId = Guid.NewGuid();

    client.OnLoggedIn += (res) =>
    {
        Log.Logger.Information("Loggin successful! Player ID: " + res.PlayerId);
    };
    client.OnResourcesUpdated += (res) =>
    {
        var balance = string.Join(",", res.Balance.Select(b => $"{b.Type}:{b.Amount}"));
        Log.Logger.Information("Player Resources updated! New balance: " + balance);
    };

    client.OnGiftSent += (res) =>
    {
        Log.Logger.Information("Gift sent!");
    };

    client.OnGiftReceived += (res) =>
    {
        Log.Logger.Information($"New Gift received! From {res.FromFriendId}, resource type: {res.Resource.Type}, resource value: {res.Resource.Amount}");
    };

    client.OnError += (error) =>
    {
        Log.Logger.Error($"Server returned error response code: {error.Code}");
    };

    await client.Connect();


    var commandsText = @"Type command: 
    - login
    - update-resource (type) (value)
    - send-gift (playerId) (type) (value) ";

    while (true)
    {
        Console.Write(commandsText);
        Console.WriteLine();
        var input = Console.ReadLine();

        var inputParts = input.Split(" ");
        var cmd = inputParts[0];

        switch (cmd)
        {
            case "login":
                {
                    await client.Login(deviceId);
                    break;
                }
            case "update-resource":
                {
                    var resourceTypeStr = inputParts[1];
                    var resourceAmountStr = inputParts[2];

                    var resourceTypeParseResult = Enum.TryParse<ResourceType>(resourceTypeStr, true, out var resourceType);
                    if (!resourceTypeParseResult)
                    {
                        Console.WriteLine("Invalid resource type, value must be either: coins or rolls");
                    }

                    var resourceAmountParseResult = int.TryParse(resourceAmountStr, out var resourceAmount);

                    if (!resourceTypeParseResult)
                    {
                        Console.WriteLine("Invalid resource amount, should be integer number");
                    }

                    await client.UpdateResources(resourceType, resourceAmount);

                    break;
                }
            case "send-gift":
                {
                    var friendIdStr = inputParts[1];
                    var resourceTypeStr = inputParts[2];
                    var resourceAmountStr = inputParts[3];

                    var friendIdParseResult = int.TryParse(friendIdStr, out var friendId);

                    if (!friendIdParseResult)
                    {
                        Console.WriteLine("Invalid friend id, should be integer number");
                    }

                    var resourceTypeParseResult = Enum.TryParse<ResourceType>(resourceTypeStr, true, out var resourceType);
                    if (!resourceTypeParseResult)
                    {
                        Console.WriteLine("Invalid resource type, value must be either: coins or rolls");
                    }

                    var resourceAmountParseResult = int.TryParse(resourceAmountStr, out var resourceAmount);

                    if (!resourceTypeParseResult)
                    {
                        Console.WriteLine("Invalid resource amount, should be integer number");
                    }

                    await client.SendGift(friendId, resourceType, resourceAmount);

                    break;
                }
            default:
                {
                    Console.WriteLine("Unknown command");
                    break;
                }
        }

        await Task.Delay(500);
    }
}
catch (Exception ex)
{
    Log.Logger.Error(ex.ToString());
}