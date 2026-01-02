using Akka.Pathfinder.Grpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Akka.Pathfinder.DemoLayout;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Direction = Akka.Pathfinder.Grpc.Direction;
using DirectionConfig = Akka.Pathfinder.Grpc.DirectionConfig;
using PointConfig = Akka.Pathfinder.Grpc.PointConfig;

namespace pathfinder.example.client;

public static class Extensions
{
    public static Direction To(this Directions value)
        => value switch
        {
            Directions.None => Direction.None,
            Directions.Top => Direction.Top,
            Directions.Bottom => Direction.Bottom,
            Directions.Left => Direction.Left,
            Directions.Right => Direction.Right,
            Directions.Front => Direction.Front,
            Directions.Back => Direction.Back,
            _ => Direction.None
        };
}

public class Delay : BackgroundService
{
    private readonly MapManager.MapManagerClient _mapManagerClient;
    private readonly Pathfinder.PathfinderClient _pathfinderClient;
    private readonly PointService.PointServiceClient _pointServiceClient;
    private readonly ILogger<Delay> _logger;

    public Delay(IServiceProvider provider)
    {
        _mapManagerClient = provider.GetRequiredService<MapManager.MapManagerClient>();
        _pathfinderClient = provider.GetRequiredService<Pathfinder.PathfinderClient>();
        _pointServiceClient = provider.GetRequiredService<PointService.PointServiceClient>();
        _logger = provider.GetRequiredService<ILogger<Delay>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //var map = MapProvider.MapConfigs[6];

        var map = MapFactoryProvider
            .Instance
            .CreateFactory()
            .Create(new IntergalaticDummyMapSettings(5, 2, new MapSize(30, 20, 20), [], 42, true));
        var points = map.Configs.Values.SelectMany(x => x).ToArray();
        var mapId = map.Id;
        var mapStateResponse = await _mapManagerClient.GetMapStateAsync(new MapRequest { MapId = mapId.ToString() },
            cancellationToken: stoppingToken);
        if (!mapStateResponse.IsReady)
        {
            await CreateMapAsync(map, stoppingToken);
        }

        _logger.LogCritical("MAP ID IS {MapId}", mapId);
        _logger.LogWarning("MAP IS READY");
        await Task.Delay(TimeSpan.FromSeconds(180), stoppingToken);

        //return;

        var requests = new[]
        {
            GenerateAFindPathRequest(points, "Westerland (Sylt)", "Garmisch-Partenkirchen"),
            GenerateAFindPathRequest(points, "Görlitz", "Aachen"),
            GenerateAFindPathRequest(points, "Stop-MetroTrack-Berlin (underground)-5",
                "Stop-MetroTrack-Munich (underground)-5"),
            GenerateAFindPathRequest(points, "Maschen", "Kornwestheim"),
            GenerateAFindPathRequest(points, "Dortmund", "Cologne")
        };
        var stream = _pathfinderClient.FindPath(cancellationToken: stoppingToken);
        foreach (var findRequest in requests)
        {
            _logger.LogInformation("Pathfinder request: {@Request}", findRequest);
            await stream.RequestStream.WriteAsync(findRequest, stoppingToken);
        }

        await stream.RequestStream.CompleteAsync();

        await foreach (var response in stream.ResponseStream.ReadAllAsync(cancellationToken: stoppingToken))
        {
            _logger.LogInformation("Pathfinder response: {@Response}", response);
            if (response.Success)
            {
                var path = await _pathfinderClient.GetPathAsync(new GetPathRequest
                {
                    PathfinderId = response.PathfinderId,
                    PathId = response.PathId
                }, cancellationToken: stoppingToken);
                _logger.LogInformation("Pathfinder response: {@Response} - cost: {@PathCost}", response,
                    path.Path.Sum(x => x.Cost));
            }
        }

        await stream.RequestStream.CompleteAsync();
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }


    private static FindPathRequest GenerateAFindPathRequest(Akka.Pathfinder.DemoLayout.PointConfig[] values,
        string sourcePoint, string targetPoint)
    {
        var random = Random.Shared;
        var randomIds = random.GetItems(values.Select(x => x.Id).ToArray(), 2);
        var sourceId = FindStationId(values, sourcePoint) ?? values[randomIds[0]].Id;
        var targetId = FindStationId(values, targetPoint) ?? values[randomIds[1]].Id;

        Directions[] directions =
            [Directions.Top, Directions.Bottom, Directions.Left, Directions.Right, Directions.Front, Directions.Back];
        var startDirection = Random.Shared.GetItems(directions, 1).Single();

        return new FindPathRequest
        {
            PathfinderId = Guid.NewGuid().ToString(),
            Direction = startDirection.To(),
            Duration = Duration.FromTimeSpan(TimeSpan.FromSeconds(40)),
            SourcePointId = sourceId,
            TargetPointId = targetId,
        };
    }

    private static int? FindStationId(Akka.Pathfinder.DemoLayout.PointConfig[] values, string stationName)
    {
        return values.FirstOrDefault(p => p.Name?.Equals(stationName) == true)?.Id;
    }

    private async Task CreateMapAsync(MapConfigWithPoints map, CancellationToken stoppingToken)
    {
        var createMapRequest = CreateMapRequest(map);

        var createMapResponse =
            await _mapManagerClient.CreateMapAsync(createMapRequest, cancellationToken: stoppingToken);
        _logger.LogInformation("Map created: {@Response}", createMapResponse);
        var loadMapResponse = await _mapManagerClient.LoadAsync(new MapRequest { MapId = createMapResponse.MapId },
            new CallOptions().WithCancellationToken(stoppingToken).WithDeadline(DateTime.UtcNow.AddHours(2)));
        _logger.LogInformation("Map loaded: {@Response}", loadMapResponse);
    }

    private CreateMapRequest CreateMapRequest(MapConfigWithPoints map)
    {
        var createMap = new CreateMapRequest
        {
            MapId = map.Id.ToString()
        };
        createMap.Points.Add(map.Configs.Values.SelectMany(x => x).Select(pointConfig =>
        {
            var result = new PointConfig
            {
                Cost = pointConfig.Cost,
                Id = pointConfig.Id
            };
            result.DirectionConfigs.Add(pointConfig.DirectionConfigs
                .ToDictionary(
                    x => (int)x.Key.To(),
                    x =>
                        new DirectionConfig
                        {
                            Cost = x.Value.Cost,
                            TargetPointId = x.Value.TargetPointId
                        }));
            return result;
        }).ToList());
        return createMap;
    }
}