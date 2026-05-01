using Dim.AspNetCore;
using Dim.AspNetCore.Authentication;
using Dim.SampleHost.Authentication;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
});

builder.Services.AddSingleton<IDimTokenValidator, TokenVerify>();
builder.Services.AddDimChat(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/dim/health"));
app.MapDimChat();

app.Run();


