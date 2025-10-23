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

// УЛУЧШЕННЫЙ Hub с обработкой ошибок
public class DrawingHub : Hub
{
    private static int _userCount = 0;
    
    public async Task SendDrawing(int startX, int startY, int endX, int endY, string color, int brushSize)
    {
        try
        {
            // ВАЛИДАЦИЯ и ЗАЩИТА от плохих данных
            if (color == null) color = "#000000";
            if (brushSize < 1) brushSize = 5;
            if (brushSize > 50) brushSize = 50;
            
            // Логируем ВСЕ входящие данные
            Console.WriteLine($"🎨 RECEIVED: {startX},{startY} -> {endX},{endY} color:{color} size:{brushSize} from:{Context.ConnectionId}");
            
            // Проверяем что координаты валидны
            if (Math.Abs(startX) > 10000 || Math.Abs(startY) > 10000 || 
                Math.Abs(endX) > 10000 || Math.Abs(endY) > 10000)
            {
                Console.WriteLine($"⚠️  Invalid coordinates: {startX},{startY} -> {endX},{endY}");
                return;
            }
            
            // Отправляем всем кроме отправителя
            await Clients.Others.SendAsync("ReceiveDrawing", startX, startY, endX, endY, color, brushSize);
            
            Console.WriteLine($"✅ SENT to others: {startX},{startY} -> {endX},{endY}");
        }
        catch (Exception ex)
        {
            // Логируем ошибку но НЕ падаем
            Console.WriteLine($"❌ ERROR in SendDrawing: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
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
