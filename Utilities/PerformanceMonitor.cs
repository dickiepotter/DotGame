using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DotGame.Utilities;

public class PerformanceMonitor
{
    private readonly Queue<double> _frameTimes;
    private readonly int _maxSamples;
    private readonly Stopwatch _frameTimer;

    public double FPS { get; private set; }
    public double AverageFrameTimeMs { get; private set; }
    public int TotalFrames { get; private set; }

    public PerformanceMonitor(int maxSamples = 60)
    {
        _maxSamples = maxSamples;
        _frameTimes = new Queue<double>(maxSamples);
        _frameTimer = new Stopwatch();
        FPS = 0;
        AverageFrameTimeMs = 0;
        TotalFrames = 0;
    }

    public void StartFrame()
    {
        _frameTimer.Restart();
    }

    public void EndFrame()
    {
        _frameTimer.Stop();
        double frameTimeMs = _frameTimer.Elapsed.TotalMilliseconds;

        // Add to queue
        _frameTimes.Enqueue(frameTimeMs);

        // Remove oldest if we exceed max samples
        if (_frameTimes.Count > _maxSamples)
        {
            _frameTimes.Dequeue();
        }

        // Calculate average
        if (_frameTimes.Count > 0)
        {
            AverageFrameTimeMs = _frameTimes.Average();
            FPS = _frameTimes.Count > 0 ? 1000.0 / AverageFrameTimeMs : 0;
        }

        TotalFrames++;
    }

    public void Reset()
    {
        _frameTimes.Clear();
        FPS = 0;
        AverageFrameTimeMs = 0;
        TotalFrames = 0;
    }

    public string GetSummary()
    {
        return $"FPS: {FPS:F1} | Frame Time: {AverageFrameTimeMs:F2}ms | Total Frames: {TotalFrames}";
    }
}
