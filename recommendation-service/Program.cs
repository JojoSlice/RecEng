using recommendation_service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddKeyedSingleton(
    "postgres",
    NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Postgres")!)
);

builder.Services.AddKeyedSingleton(
    "timescale",
    NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Timescale")!)
);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!)
);

var host = builder.Build();
host.Run();
