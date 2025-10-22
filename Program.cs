using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Добавляем логирование
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Используем CORS
app.UseCors("AllowAll");

// Раздаём статические файлы из wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// Настраиваем SignalR Hub
app.MapHub<DrawingHub>("/drawingHub");

// Простой endpoint для проверки
app.MapGet("/api/health", () => new { status = "OK", time = DateTime.UtcNow });

// Перенаправляем с корня на нашу страницу
app.MapGet("/", () => "Drawing App is running! Go to /index.html");

// Запускаем приложение
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

// Hub для рисования
public class DrawingHub : Hub
{
    private static int _userCount = 0;
    private readonly ILogger<DrawingHub> _logger;

    public DrawingHub(ILogger<DrawingHub> logger)
    {
        _logger = logger;
    }
    
    public async Task SendDrawing(int startX, int startY, int endX, int endY, string color, int brushSize)
    {
        _logger.LogInformation($"📤 Отправка рисунка: {startX},{startY} -> {endX},{endY} цвет: {color} размер: {brushSize}");
        
        try
        {
            await Clients.Others.SendAsync("ReceiveDrawing", startX, startY, endX, endY, color, brushSize);
            _logger.LogInformation("✅ Рисунок отправлен другим клиентам");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Ошибка отправки рисунка: {ex.Message}");
        }
    }
    
    public async Task ClearCanvas()
    {
        _logger.LogInformation("🧹 Очистка холста");
        await Clients.All.SendAsync("CanvasCleared");
    }
    
    // При подключении пользователя
    public override async Task OnConnectedAsync()
    {
        _userCount++;
        _logger.LogInformation($"🔗 Пользователь подключился. Всего: {_userCount}, ConnectionId: {Context.ConnectionId}");
        
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnConnectedAsync();
    }
    
    // При отключении пользователя
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userCount--;
        _logger.LogInformation($"🔌 Пользователь отключился. Всего: {_userCount}, ConnectionId: {Context.ConnectionId}");
        
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnDisconnectedAsync(exception);
    }
}
