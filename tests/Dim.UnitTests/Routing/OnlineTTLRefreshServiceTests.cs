using Dim.Application.Routing;
using FluentAssertions;

namespace Dim.UnitTests.Routing;

public sealed class OnlineTTLRefreshServiceTests
{
    [Fact]
    public void CalculateRefreshScheduleShouldRefreshSmallConnectionSetInSingleTick()
    {
        var schedule = OnlineTTLRefreshService.CalculateRefreshSchedule(10, TimeSpan.FromSeconds(30));

        schedule.BatchSize.Should().Be(10);
        schedule.Delay.Should().BeLessThanOrEqualTo(schedule.RefreshWindow);
        schedule.Delay.Ticks.Should().BeLessThanOrEqualTo(schedule.RefreshWindow.Ticks);
    }

    [Fact]
    public void CalculateRefreshScheduleShouldLimitLargeBatchAndFinishWithinRefreshWindow()
    {
        const int connectionCount = 2000;

        var schedule = OnlineTTLRefreshService.CalculateRefreshSchedule(connectionCount, TimeSpan.FromMinutes(1));

        schedule.BatchSize.Should().BeLessThanOrEqualTo(512);
        (schedule.BatchSize * schedule.TickCount).Should().BeGreaterThanOrEqualTo(connectionCount);
        (schedule.Delay.Ticks * schedule.TickCount).Should().BeLessThanOrEqualTo(schedule.RefreshWindow.Ticks);
    }

    [Fact]
    public void CalculateRefreshScheduleShouldUseDefaultTtlWhenRouteTtlIsNotPositive()
    {
        var schedule = OnlineTTLRefreshService.CalculateRefreshSchedule(1, TimeSpan.Zero);
        var expectedRefreshWindow = TimeSpan.FromTicks((long)Math.Ceiling(TimeSpan.FromDays(7).Ticks * 0.8));

        schedule.RefreshWindow.Should().Be(expectedRefreshWindow);
        schedule.BatchSize.Should().Be(1);
        schedule.Delay.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CalculateRefreshScheduleShouldUseBoundedDelayWhenNoConnectionExists()
    {
        var schedule = OnlineTTLRefreshService.CalculateRefreshSchedule(0, TimeSpan.FromMinutes(1));

        schedule.BatchSize.Should().Be(0);
        schedule.Delay.Should().Be(TimeSpan.FromSeconds(30));
    }
}
