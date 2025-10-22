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

var app = builder.Build();

// Используем CORS
app.UseCors("AllowAll");

// Раздаём статические файлы из wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// Настраиваем SignalR Hub
app.MapHub<DrawingHub>("/drawingHub");

// Перенаправляем с корня на нашу страницу
app.MapGet("/", () => "Drawing App is running! Go to /index.html");

// Запускаем приложение
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

// Hub для рисования
public class DrawingHub : Hub
{
    private static int _userCount = 0;
    
    public async Task SendDrawing(int startX, int startY, int endX, int endY, string color, int brushSize)
    {
        await Clients.Others.SendAsync("ReceiveDrawing", startX, startY, endX, endY, color, brushSize);
    }
    
    public async Task ClearCanvas()
    {
        await Clients.All.SendAsync("CanvasCleared");
    }
    
    // При подключении пользователя
    public override async Task OnConnectedAsync()
    {
        _userCount++;
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnConnectedAsync();
    }
    
    // При отключении пользователя
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userCount--;
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnDisconnectedAsync(exception);
    }
}
