using Microsoft.Extensions.Hosting;
using WorkerService;
using SQLitePCL;

Batteries.Init();
IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppPathsOptions>(context.Configuration.GetSection("AppPaths"));
        services.Configure<AppSettingsOptions>(context.Configuration.GetSection("AppSettings"));
        services.AddSingleton<IConfiguration>(context.Configuration);
        services.AddHostedService<Worker>();
    })
    .Build();
LogHelper.LoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
host.Run();
