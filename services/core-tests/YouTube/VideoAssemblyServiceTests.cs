using DevPulse.Infrastructure.YouTube;
using FluentAssertions;

namespace DevPulse.Tests.YouTube;

public class VideoAssemblyServiceTests
{
    private static (byte[] Image, double DurationSeconds) Slide(double dur) =>
        (Array.Empty<byte>(), dur);

    [Fact]
    public void Single_slide_maps_directly_to_vout()
    {
        var result = VideoAssemblyService.BuildFilterComplex([Slide(4.0)]);
        result.Should().Be("[0:v]fps=30,scale=1080:1920[vout]");
    }

    [Fact]
    public void Two_slides_produce_one_xfade()
    {
        var result = VideoAssemblyService.BuildFilterComplex([Slide(4.0), Slide(5.0)]);
        result.Should().Contain("xfade=transition=fade")
              .And.Contain("[v0][v1]")
              .And.Contain("[vout]")
              .And.NotContain("[x");
    }

    [Fact]
    public void Three_slides_produce_chained_xfades()
    {
        var result = VideoAssemblyService.BuildFilterComplex([Slide(4.0), Slide(5.0), Slide(6.0)]);
        result.Should().Contain("[v0][v1]xfade")
              .And.Contain("[x1][v2]xfade")
              .And.Contain("[vout]");
    }

    [Fact]
    public void Xfade_offset_accounts_for_transition_overlap()
    {
        // With td=0.3, offset for first xfade = 4.0 - 0.3 = 3.70
        var result = VideoAssemblyService.BuildFilterComplex([Slide(4.0), Slide(5.0)]);
        result.Should().Contain("offset=3.70");
    }

    [Fact]
    public void Second_xfade_offset_subtracts_previous_transition()
    {
        // offset[1] = d0 + d1 - 2*td = 4.0 + 5.0 - 0.6 = 8.40
        var result = VideoAssemblyService.BuildFilterComplex([Slide(4.0), Slide(5.0), Slide(6.0)]);
        result.Should().Contain("offset=8.40");
    }
}
