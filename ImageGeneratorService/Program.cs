var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("TextRenderService", (c) =>
{
    c.BaseAddress = new Uri(builder.Configuration["TextRenderServiceUrl"]!);
});

var app = builder.Build();
app.MapControllers();
app.Run();