namespace Engine.Core;

public sealed class TimeStepController
{
    private readonly double _fixedStep;
    private double _accumulator;
    public TimeStepController(double fixedStep = 1.0 / 60.0) => _fixedStep = fixedStep;
    public int Consume(double frameDeltaSeconds)
    {
        _accumulator += Math.Clamp(frameDeltaSeconds, 0.0, 0.25);
        var steps = 0;
        while (_accumulator >= _fixedStep)
        {
            steps++;
            _accumulator -= _fixedStep;
        }

        return steps;
    }

    public double FixedDelta => _fixedStep;
    public double Alpha => _accumulator / _fixedStep;
}
