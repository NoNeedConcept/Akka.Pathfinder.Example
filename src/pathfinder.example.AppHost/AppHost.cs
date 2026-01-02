var builder = DistributedApplication.CreateBuilder(args);

const string admin = "admin";
var usernameResource = builder.AddParameter("username", admin);
var passwordResource = builder.AddParameter("password", admin);

var redis = builder
    .AddRedis("redis", 54000)
    .WithPassword(passwordResource);

var mongo = builder
    .AddMongoDB("Mongo", 55000, usernameResource, passwordResource)
    .AddDatabase("mongodb", "pathfinder");

const string pathfinderImage = "ghcr.io/noneedconcept/akka.pathfinder";
const string grpcImage = "ghcr.io/noneedconcept/akka.pathfinder.grpc";
const string aspnetcoreHttpsPorts = "ASPNETCORE_HTTPS_PORTS";
const string aspnetcoreHttpPorts = "ASPNETCORE_HTTP_PORTS";
const string health = "/health";
const string alive = $"{health}/alive";

var pathfinderOne = builder
    .AddContainer("PathfinderOne", pathfinderImage, "latest")
    .WithImagePullPolicy(ImagePullPolicy.Always)
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(targetPort: 6000, env: aspnetcoreHttpPorts)
    .WithHttpHealthCheck(health)
    .WithHttpHealthCheck(alive)
    .WithOtlpExporter();

var pathfinderTwo = builder
    .AddContainer("PathfinderTwo", pathfinderImage, "latest")
    .WithImagePullPolicy(ImagePullPolicy.Always)
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(targetPort: 6050, env: aspnetcoreHttpPorts)
    .WithHttpHealthCheck(health)
    .WithHttpHealthCheck(alive)
    .WithOtlpExporter();

var pathfinderThree = builder
    .AddContainer("PathfinderThree", pathfinderImage, "latest")
    .WithImagePullPolicy(ImagePullPolicy.Always)
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(targetPort: 6100, env: aspnetcoreHttpPorts)
    .WithHttpHealthCheck(health)
    .WithHttpHealthCheck(alive)
    .WithOtlpExporter();

var pathfinderFour = builder
    .AddContainer("PathfinderFour", pathfinderImage, "latest")
    .WithImagePullPolicy(ImagePullPolicy.Always)
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(targetPort: 6150, env: aspnetcoreHttpPorts)
    .WithHttpHealthCheck(health)
    .WithHttpHealthCheck(alive)
    .WithOtlpExporter();

var pathfinderFive = builder
    .AddContainer("PathfinderFive", pathfinderImage, "latest")
    .WithImagePullPolicy(ImagePullPolicy.Always)
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(targetPort: 6200, env: aspnetcoreHttpPorts)
    .WithHttpHealthCheck(health)
    .WithHttpHealthCheck(alive)
    .WithOtlpExporter();

var pathfinderGrpc = builder
    .AddContainer("PathfinderGrpc", grpcImage, "latest")
    .WithImagePullPolicy(ImagePullPolicy.Always)
    .WithReference(mongo)
    .WaitFor(mongo)
    .WithHttpEndpoint(targetPort: 6250, env: aspnetcoreHttpPorts)
    .WithHttpsEndpoint(targetPort: 5250, env: aspnetcoreHttpsPorts)
    .WithHttpHealthCheck(health)
    .WithHttpHealthCheck(alive)
    .WithBindMount($@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\.aspnet\https\aspnetapp.pfx",
        "/aspnet/https/aspnetapp.pfx")
    .WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Path", "/aspnet/https/aspnetapp.pfx")
    .WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Password", "TEST")
    .WithOtlpExporter();

builder.AddAkka("akka", "zeus", resourceBuilder =>
{
    resourceBuilder
        .WithLighthouse(3)
        .WithNode(targetPort: 8000, resource: pathfinderOne)
        .WithNode(targetPort: 8001, resource: pathfinderTwo)
        .WithNode(targetPort: 8002, resource: pathfinderThree)
        .WithNode(targetPort: 8003, resource: pathfinderFour)
        .WithNode(targetPort: 8004, resource: pathfinderFive)
        .WithNode(targetPort: 8005, resource: pathfinderGrpc);
});

var client = builder
    .AddProject<Projects.pathfinder_example_client>("PathfinderClient")
    .WithReference(pathfinderGrpc.GetEndpoint("https"))
    .WaitFor(pathfinderGrpc)
    .WaitFor(pathfinderOne)
    .WaitFor(pathfinderTwo)
    .WaitFor(pathfinderThree)
    .WaitFor(pathfinderFour)
    .WaitFor(pathfinderFive)
    .WithOtlpExporter();

await builder.Build().RunAsync();