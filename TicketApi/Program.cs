using System;
using System.IO;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.MapPost("/data", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    string name = await reader.ReadToEndAsync();
    return $"Получены данные: {name}";
});

app.Run();