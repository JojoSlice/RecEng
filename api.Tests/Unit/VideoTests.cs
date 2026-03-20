using api.Features.Videos;

namespace api.Tests.Unit;

public class VideoTests
{
    [Fact]
    public void NewVideo_HasProcessingStatus()
    {
        var video = new Video("title", "desc", Guid.NewGuid());

        Assert.Equal(VideoStatus.Processing, video.Status);
    }

    [Fact]
    public void SetReady_SetsStatusAndFilePath()
    {
        var video = new Video("title", "desc", Guid.NewGuid());

        video.SetReady("/uploads/abc.mp4");

        Assert.Equal(VideoStatus.Ready, video.Status);
        Assert.Equal("/uploads/abc.mp4", video.FilePath);
    }

    [Fact]
    public void SetFailed_SetsFailedStatus()
    {
        var video = new Video("title", "desc", Guid.NewGuid());

        video.SetFailed();

        Assert.Equal(VideoStatus.Failed, video.Status);
    }

    [Fact]
    public void Update_ChangesTitleAndDescription()
    {
        var video = new Video("old title", "old desc", Guid.NewGuid());

        video.Update("new title", "new desc", []);

        Assert.Equal("new title", video.Title);
        Assert.Equal("new desc", video.Description);
    }

    [Fact]
    public void Update_ReplacesTags()
    {
        var video = new Video("title", "desc", Guid.NewGuid());
        video.Tags.AddRange([new Tag("old1"), new Tag("old2")]);

        video.Update("title", "desc", [new Tag("new1")]);

        Assert.Single(video.Tags);
        Assert.Equal("new1", video.Tags[0].Name);
    }

    [Fact]
    public void Update_WithEmptyTags_ClearsTags()
    {
        var video = new Video("title", "desc", Guid.NewGuid());
        video.Tags.Add(new Tag("sometag"));

        video.Update("title", "desc", []);

        Assert.Empty(video.Tags);
    }
}
