using analytics_service.Consumers;
using MassTransit;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Analytics");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString!));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<VideoWatchedConsumer>();
    x.AddConsumer<VideoLikedConsumer>();
    x.AddConsumer<VideoUnlikedConsumer>();

    x.UsingRabbitMq(
        (ctx, cfg) =>
        {
            cfg.Host(
                builder.Configuration["RabbitMq:Host"],
                "/",
                h =>
                {
                    h.Username(builder.Configuration["RabbitMq:Username"]!);
                    h.Password(builder.Configuration["RabbitMq:Password"]!);
                }
            );
            cfg.ConfigureEndpoints(ctx);
        }
    );
});

var host = builder.Build();
host.Run();
