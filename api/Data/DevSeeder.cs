using api.Features.Users;
using api.Features.Videos;

namespace api.Data;

public static class DevSeeder
{
    private static readonly (string Title, string Description, string[] Tags)[] JojoVideoMetadata =
    [
        ("Water", "A video of water", ["low fps", "water", "vibe"]),
        ("Stars", "Lookng at the stars", ["low fps", "stars", "sky", "vibe"]),
        ("Traffic", "Walking through the city center", ["low fps", "traffic", "city", "vibe"]),
    ];

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

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

        var jojo = await db.Users.FirstOrDefaultAsync(u => u.Username == "jojo");

        if (jojo is null)
        {
            jojo = new User("jojo", BCrypt.Net.BCrypt.HashPassword("password"));
            db.Users.Add(jojo);
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeeder] Created user 'jojo'");
        }

        var profilePicture = ImageExtensions
            .SelectMany(ext => Directory.GetFiles(devAssetsPath, $"*{ext}"))
            .OrderBy(f => f)
            .FirstOrDefault();

        if (profilePicture is not null)
        {
            Directory.CreateDirectory(Path.Combine("uploads", "profile-pictures"));
            var ext = Path.GetExtension(profilePicture);
            var destPath = Path.Combine("uploads", "profile-pictures", $"{jojo.Id}{ext}");
            File.Copy(profilePicture, destPath, overwrite: true);
            jojo.SetProfilePicture(destPath);
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeeder] Set profile picture for 'jojo'");
        }
        else
        {
            logger.LogWarning(
                "[DevSeeder] No image file found in {Path}, skipping profile picture",
                devAssetsPath
            );
        }

        var videoFiles = Directory
            .GetFiles(devAssetsPath, "*.mp4")
            .OrderBy(f => f)
            .Take(3)
            .ToArray();

        if (videoFiles.Length == 0)
        {
            logger.LogWarning("[DevSeeder] No .mp4 files found in {Path}", devAssetsPath);
            return;
        }

        Directory.CreateDirectory("uploads");

        for (var i = 0; i < videoFiles.Length; i++)
        {
            var meta = JojoVideoMetadata[i];
            var destFileName = $"jojo_{i + 1}{Path.GetExtension(videoFiles[i])}";
            var destPath = Path.Combine("uploads", destFileName);

            await using (
                var src = new FileStream(
                    videoFiles[i],
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true
                )
            )
            await using (
                var dst = new FileStream(
                    destPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true
                )
            )
                await src.CopyToAsync(dst);

            var tagNames = meta.Tags.Select(t => t.ToLowerInvariant()).Distinct().ToList();
            var existingTags = await db.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
            var newTags = tagNames
                .Where(name => !existingTagNames.Contains(name))
                .Select(name => new Tag(name))
                .ToList();

            var video = new Video(meta.Title, meta.Description, jojo.Id);
            video.SetReady(destPath);
            video.Tags.AddRange(existingTags.Concat(newTags));
            db.Videos.Add(video);
            await db.SaveChangesAsync();
        }

        logger.LogInformation("[DevSeeder] Seeded {Count} videos for 'jojo'", videoFiles.Length);
    }
}
