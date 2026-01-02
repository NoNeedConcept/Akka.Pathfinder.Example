using System.Diagnostics.CodeAnalysis;
using Akka.Pathfinder.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Servus.Core.Application.Startup;

namespace pathfinder.example.client.Startup;

public class ServiceSetupContainer : IServiceSetupContainer, IHostApplicationBuilderSetupContainer
{
    [Experimental("EXTEXP0001")]
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        var pathfinderOneUrl = Environment.GetEnvironmentVariable("services__PathfinderGrpc__https__0")!;
        services
            .AddGrpcClient<MapManager.MapManagerClient>(o => o.Address = new Uri(pathfinderOneUrl))
            .RemoveAllResilienceHandlers();
        services
            .AddGrpcClient<Pathfinder.PathfinderClient>(o => o.Address = new Uri(pathfinderOneUrl))
            .RemoveAllResilienceHandlers();
        services
            .AddGrpcClient<PointService.PointServiceClient>(o => o.Address = new Uri(pathfinderOneUrl))
            .RemoveAllResilienceHandlers();
        services.AddHostedService<Delay>();
    }

    public void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
    }
}