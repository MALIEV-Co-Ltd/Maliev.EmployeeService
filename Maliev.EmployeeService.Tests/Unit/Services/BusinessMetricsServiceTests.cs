using System.Diagnostics.Metrics;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Services;

public class BusinessMetricsServiceTests
{
    [Fact]
    public void MeterCreation_ShouldSucceed()
    {
        using var meter = new Meter("test-meter");

        Assert.NotNull(meter);
    }

    [Fact]
    public void Meter_ShouldHaveCorrectName()
    {
        using var meter = new Meter("my-custom-meter");

        Assert.Equal("my-custom-meter", meter.Name);
    }

    [Fact]
    public void ObservableGauge_ShouldTrackValue()
    {
        using var meter = new Meter("test-meter");

        var gauge = meter.CreateObservableGauge("test-gauge", () => 42);

        Assert.NotNull(gauge);
    }

    [Fact]
    public void Counter_ShouldIncrement()
    {
        using var meter = new Meter("test-meter");

        var counter = meter.CreateCounter<int>("test-counter");

        counter.Add(1);
        counter.Add(5);

        Assert.NotNull(counter);
    }

    [Fact]
    public void Histogram_ShouldRecordValue()
    {
        using var meter = new Meter("test-meter");

        var histogram = meter.CreateHistogram<double>("test-histogram");

        histogram.Record(1.5);
        histogram.Record(2.5);

        Assert.NotNull(histogram);
    }
}
