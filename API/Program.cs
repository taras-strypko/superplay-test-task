using API.Features;
using API.Infrastructure;
using Contracts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddSingleton<PlayerRepository>();
builder.Services.AddSingleton<PlayersPool>();

builder.Services.RegisterMessageHandler<LoginRequest, LoginResponse, LoginHandler>();
builder.Services.RegisterMessageHandler<UpdateResourceRequest, UpdateResourceResponse, UpdateResourceHandler>();
builder.Services.RegisterMessageHandler<SendGiftRequest, SendGiftResponse, SendGiftHandler>();

var app = builder.Build();

app.MapGet("/", () => "Game server is running");

app.UseGameServer();

logger.Information("Game server is running");

app.Run();