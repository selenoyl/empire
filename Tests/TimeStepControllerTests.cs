using Engine.Core;

namespace Tests;

public class TimeStepControllerTests
{
    [Fact]
    public void AccumulatorProducesDeterministicSteps()
    {
        var t = new TimeStepController(1.0 / 60.0);
        var steps = t.Consume(1.0 / 30.0);
        Assert.Equal(2, steps);
        steps = t.Consume(1.0 / 120.0);
        Assert.Equal(0, steps);
        steps = t.Consume(1.0 / 120.0);
        Assert.Equal(1, steps);
    }
}
