var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapHub<DrawingHub>("/drawingHub");
app.MapGet("/", () => "Drawing App is running! Go to /index.html");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

public class DrawingHub : Microsoft.AspNetCore.SignalR.Hub
{
    private static int _userCount = 0;
    
    public async Task SendDrawing(int startX, int startY, int endX, int endY, string color, int brushSize)
    {
        await Clients.Others.SendAsync("ReceiveDrawing", startX, startY, endX, endY, color, brushSize);
    }
    
    public async Task ClearCanvas() => await Clients.All.SendAsync("CanvasCleared");
    
    public override async Task OnConnectedAsync()
    {
        _userCount++;
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userCount--;
        await Clients.All.SendAsync("UserCountUpdated", _userCount);
        await base.OnDisconnectedAsync(exception);
    }
}