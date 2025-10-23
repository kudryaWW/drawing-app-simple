using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddSignalR();
builder.Services.AddCors();

var app = builder.Build();

// Используем CORS
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Раздаём статические файлы из wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// Настраиваем SignalR Hub
app.MapHub<DrawingHub>("/drawingHub");

// Простой endpoint для проверки
app.MapGet("/api/health", () => new { 
    status = "OK", 
    time = DateTime.UtcNow,
    message = "Server is running"
});

// Перенаправляем с корня на нашу страницу
app.MapGet("/", () => Results.Redirect("/index.html"));

// Запускаем приложение
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

// УЛУЧШЕННЫЙ Hub с поддержкой float
public class DrawingHub : Hub
{
    private static int _userCount = 0;
    
    // ИЗМЕНИЛ: принимаем double вместо int
    public async Task SendDrawing(double startX, double startY, double endX, double endY, string color, int brushSize)
    {
        try
        {
            // ВАЛИДАЦИЯ
            if (color == null) color = "#000000";
            if (brushSize < 1) brushSize = 5;
            if (brushSize > 50) brushSize = 50;
            
            // Преобразуем double в int (округляем)
            int startXInt = (int)Math.Round(startX);
            int startYInt = (int)Math.Round(startY);
            int endXInt = (int)Math.Round(endX);
            int endYInt = (int)Math.Round(endY);
            
            // Ограничиваем координаты
            startXInt = Math.Clamp(startXInt, 0, 800);
            startYInt = Math.Clamp(startYInt, 0, 500);
            endXInt = Math.Clamp(endXInt, 0, 800);
            endYInt = Math.Clamp(endYInt, 0, 500);
            
            Console.WriteLine($"🎨 RECEIVED: {startX:F2},{startY:F2} -> {endX:F2},{endY:F2} (converted to: {startXInt},{startYInt} -> {endXInt},{endYInt})");
            
            // Отправляем всем кроме отправителя
            await Clients.Others.SendAsync("ReceiveDrawing", startXInt, startYInt, endXInt, endYInt, color, brushSize);
            
            Console.WriteLine($"✅ SENT to others: {startXInt},{startYInt} -> {endXInt},{endYInt}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in SendDrawing: {ex.Message}");
            Console.WriteLine($"❌ Data: {startX},{startY} -> {endX},{endY} color:{color} size:{brushSize}");
        }
    }
    
    public async Task ClearCanvas()
    {
        Console.WriteLine("🧹 Clear canvas requested");
        await Clients.All.SendAsync("CanvasCleared");
    }
    
    public override async Task OnConnectedAsync()
    {
        _userCount++;
        Console.WriteLine($"🔗 CONNECTED: {Context.ConnectionId}. Total: {_userCount}");
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userCount--;
        Console.WriteLine($"🔌 DISCONNECTED: {Context.ConnectionId}. Total: {_userCount}");
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnDisconnectedAsync(exception);
    }
}
