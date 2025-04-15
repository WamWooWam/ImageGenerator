using WinTextRenderer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseIIS();

builder.Services.AddWindowsService();

builder.Services.AddHostedService<WPFHostedService>();
builder.Services.AddSingleton<IDispatcher, WPFDispatcher>();
builder.Services.AddTransient<ITextRenderService, WPFTextRenderService>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
