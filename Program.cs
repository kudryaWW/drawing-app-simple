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
    message = "Server is running",
    port = Environment.GetEnvironmentVariable("PORT")
});

// Перенаправляем с корня на нашу страницу
app.MapGet("/", () => Results.Redirect("/index.html"));

// ЗАПУСК НА ПРАВИЛЬНОМ ПОРТУ
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

// Максимально простой Hub
public class DrawingHub : Hub
{
    private static int _userCount = 0;
    
    public async Task SendDrawing(int startX, int startY, int endX, int endY, string color, int brushSize)
    {
        Console.WriteLine($"📱 Received from {Context.ConnectionId}: {startX},{startY} -> {endX},{endY}");
        await Clients.Others.SendAsync("ReceiveDrawing", startX, startY, endX, endY, color, brushSize);
    }
    
    public async Task ClearCanvas()
    {
        Console.WriteLine("🧹 Clear canvas");
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
