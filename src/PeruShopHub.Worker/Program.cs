var builder = Host.CreateApplicationBuilder(args);

// Background services will be registered here in Phase 2

var host = builder.Build();
host.Run();
