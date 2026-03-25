using api.Features.Auth;
using api.Features.Users;
using api.Features.Videos;

namespace api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<VideoLike> VideoLikes => Set<VideoLike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();

        modelBuilder.Entity<UserFollow>().HasKey(f => new { f.FollowerId, f.FollowedId });

        modelBuilder
            .Entity<UserFollow>()
            .HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<UserFollow>()
            .HasOne(f => f.Followed)
            .WithMany()
            .HasForeignKey(f => f.FollowedId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VideoLike>().HasKey(l => new { l.UserId, l.VideoId });

        modelBuilder
            .Entity<VideoLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<VideoLike>()
            .HasOne(l => l.Video)
            .WithMany()
            .HasForeignKey(l => l.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
