using Microsoft.Extensions.Hosting;

// Host preparado sin consumidores o trabajos simulados.
var builder = Host.CreateApplicationBuilder(args);
using var host = builder.Build();
await host.RunAsync();
