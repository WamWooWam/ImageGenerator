using WinTextRenderer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService();

builder.Services.AddHostedService<WPFService>();
builder.Services.AddSingleton<DispatcherAccessor>();
builder.Services.AddTransient<ITextRenderService, WPFTextRenderService>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
