using api.Features.Users;
using api.Features.Videos;

namespace api.Data;

public static class DevSeeder
{
    private static readonly (string Title, string Description, string[] Tags)[] VideoMetadata =
    [
        ("Morning Workout", "A quick morning workout routine", ["fitness", "workout", "morning"]),
        ("Cooking Pasta", "How to cook the perfect pasta", ["cooking", "food", "italian"]),
        ("City Walk", "Walking through the city center", ["travel", "city", "walk"]),
        ("Guitar Practice", "Fingerpicking patterns for beginners", ["music", "guitar", "tutorial"]),
        ("Dog Park", "Playful dogs at the park", ["animals", "dogs", "outdoor"]),
        ("Sunset Timelapse", "Beautiful sunset timelapse", ["nature", "timelapse", "sunset"]),
        ("Yoga Session", "20 minute yoga for flexibility", ["yoga", "fitness", "wellness"]),
        ("Street Food", "Trying street food around town", ["food", "travel", "street"]),
        ("Rain on Window", "Relaxing rain sounds and visuals", ["relaxing", "rain", "ambient"]),
        ("Skateboarding", "Tricks at the local skatepark", ["sports", "skateboarding", "outdoor"]),
    ];

    public static async Task SeedAsync(IServiceProvider services, string devAssetsPath)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (await db.Videos.AnyAsync())
        {
            logger.LogInformation("[DevSeeder] Videos already seeded, skipping");
            return;
        }

        const string seedUsername = "seed_user";
        var seedUser = await db.Users.FirstOrDefaultAsync(u => u.Username == seedUsername);

        if (seedUser is null)
        {
            seedUser = new User(seedUsername, BCrypt.Net.BCrypt.HashPassword("seed_password"));
            db.Users.Add(seedUser);
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeeder] Created seed user '{Username}'", seedUsername);
        }

        var videoFiles = Directory
            .GetFiles(devAssetsPath, "*.mp4")
            .OrderBy(f => f)
            .ToArray();

        if (videoFiles.Length == 0)
        {
            logger.LogWarning("[DevSeeder] No .mp4 files found in {Path}", devAssetsPath);
            return;
        }

        Directory.CreateDirectory("uploads");

        for (var i = 0; i < videoFiles.Length; i++)
        {
            var meta = VideoMetadata[i % VideoMetadata.Length];
            var destFileName = $"seed_{i + 1}{Path.GetExtension(videoFiles[i])}";
            var destPath = Path.Combine("uploads", destFileName);

            File.Copy(videoFiles[i], destPath, overwrite: true);

            var tagNames = meta.Tags.Select(t => t.ToLowerInvariant()).Distinct().ToList();
            var existingTags = await db.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
            var newTags = tagNames
                .Where(name => !existingTagNames.Contains(name))
                .Select(name => new Tag(name))
                .ToList();

            var video = new Video(meta.Title, meta.Description, destPath, seedUser.Id);
            video.Tags.AddRange(existingTags.Concat(newTags));
            db.Videos.Add(video);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[DevSeeder] Seeded {Count} videos", videoFiles.Length);
    }
}
