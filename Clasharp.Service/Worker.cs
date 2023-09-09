using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Clasharp.Common;

namespace Clasharp.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly ConcurrentQueue<string> _logsQueue = new();
    private ClashWrapper? _clashWrapper;
    private readonly HttpListenerWrapper _httpListenerWrapper;

    public Worker(ILogger<Worker> logger, HttpListenerWrapper httpListenerWrapper)
    {
        _logger = logger;
        _httpListenerWrapper = httpListenerWrapper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _httpListenerWrapper.AddRoute("/start_clash", HandleStart);
        _httpListenerWrapper.AddRoute("/stop_clash", HandleStop);
        _httpListenerWrapper.AddRoute("/is_running", HandleIsRunning);
        _httpListenerWrapper.AddRoute("/logs", HandleLogs);
        _httpListenerWrapper.AddRoute("/hello", HandleHello);
        var httpLocalhost = $"http://localhost:{GlobalConfigs.ClashServicePort}/";
        _logger.LogInformation($"Listening at {httpLocalhost}");
        stoppingToken.Register(() => _clashWrapper?.Stop());
        await _httpListenerWrapper.Listen(httpLocalhost, stoppingToken);
    }

    private Task HandleIsRunning(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var running = _clashWrapper?.IsRunning() ?? false;
        context.Return(running.ToString());
        return Task.CompletedTask;
    }

    private Task HandleHello(HttpListenerContext context, CancellationToken cancellationToken)
    {
        context.Return("OK");
        return Task.CompletedTask;
    }

    private async Task HandleStart(HttpListenerContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleStart");
        var launchInfo = await context.GetRequestBody<ClashLaunchInfo>();
        if (launchInfo == null)
        {
            _logger.LogWarning("Invalid ClashLaunchInfo");
            context.Return(400);
            return;
        }

        _clashWrapper?.Stop();
        _clashWrapper = new ClashWrapper(launchInfo)
        {
            OnNewLog = s =>
            {
                _logger.LogDebug("new clash log: {log}", s);
                _logsQueue.Enqueue(s);
            }
        };
        _logger.LogInformation("Start clash {Clash} at {Path}", launchInfo.ExecutablePath, launchInfo.WorkDir);
        _clashWrapper.Start();
        context.Return();
    }

    private Task HandleStop(HttpListenerContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleStop");
        _clashWrapper?.Stop();
        context.Return();
        return Task.CompletedTask;
    }

    private async Task HandleLogs(HttpListenerContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleLogs");
        if (!context.Request.IsWebSocketRequest)
        {
            _logger.LogWarning("Not websocket");
            context.Return(400);
            return;
        }

        var webSocketContext = await context.AcceptWebSocketAsync(null);
        while (webSocketContext.WebSocket.State == WebSocketState.Open)
        {
            if (_logsQueue.TryDequeue(out var log))
            {
                var logArraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(log));
                await webSocketContext.WebSocket.SendAsync(logArraySegment, WebSocketMessageType.Text, true,
                    cancellationToken);
                _logger.LogDebug("Sent log {log}", log);
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        _logger.LogWarning("Websocket closed");
    }
}