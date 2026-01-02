using Microsoft.AspNetCore.Builder;
using pathfinder.example.client.Startup;
using Servus.Core.Application.Startup;

var appRunner = AppBuilder
    .Create(WebApplication.CreateBuilder(args), b => b.Build())
    .WithSetup<ServiceSetupContainer>();

await appRunner.Build().RunAsync();