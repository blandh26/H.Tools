using HTools.Core.Models;

namespace HTools.Windows.Effects;

public sealed class MouseShakeDetector
{
    private static readonly TimeSpan DefaultSampleInterval = TimeSpan.FromMilliseconds(8);
    private static readonly TimeSpan DetectionWindow = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan DefaultTrailWindow = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(1100);

    private readonly Queue<TurnPoint> _turns = new();
    private readonly Queue<SamplePoint> _samples = new();

    private ScreenPoint? _lastPoint;
    private DateTime _lastSampleTime = DateTime.MinValue;
    private DateTime _lastTriggerTime = DateTime.MinValue;
    private int _lastDirection;
    private int _lastTurnX;

    public TimeSpan SampleInterval { get; set; } = DefaultSampleInterval;

    public TimeSpan TrailWindow { get; set; } = DefaultTrailWindow;

    public int CopyRecentSamples(TrailSample[] target)
    {
        ExpireOldSamples(DateTime.UtcNow);

        if (target.Length == 0 || _samples.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var skip = Math.Max(0, _samples.Count - target.Length);
        var index = 0;
        var copied = 0;

        foreach (var sample in _samples)
        {
            if (index++ < skip)
            {
                continue;
            }

            var age = now - sample.Time;
            var life = 1f - (float)(age.TotalMilliseconds / Math.Max(1d, TrailWindow.TotalMilliseconds));
            target[copied++] = new TrailSample(sample.Point, Math.Clamp(life, 0f, 1f));
        }

        return copied;
    }

    public bool RegisterMove(ScreenPoint point)
    {
        var now = DateTime.UtcNow;
        if (now - _lastSampleTime < SampleInterval)
        {
            return false;
        }

        _lastSampleTime = now;
        AddSample(point, now);

        if (_lastPoint is null)
        {
            _lastPoint = point;
            _lastTurnX = point.X;
            return false;
        }

        var dx = point.X - _lastPoint.Value.X;
        var dy = point.Y - _lastPoint.Value.Y;
        _lastPoint = point;

        if (Math.Abs(dx) < 8 || Math.Abs(dx) < Math.Abs(dy) * 0.45)
        {
            ExpireOldTurns(now);
            return false;
        }

        var direction = Math.Sign(dx);
        if (_lastDirection == 0)
        {
            _lastDirection = direction;
            _lastTurnX = point.X;
            return false;
        }

        if (direction == _lastDirection)
        {
            return false;
        }

        if (Math.Abs(point.X - _lastTurnX) < 45)
        {
            return false;
        }

        _lastDirection = direction;
        _lastTurnX = point.X;
        _turns.Enqueue(new TurnPoint(point.X, now));
        ExpireOldTurns(now);

        if (now - _lastTriggerTime < Cooldown)
        {
            return false;
        }

        if (_turns.Count < 4)
        {
            return false;
        }

        var minX = _turns.Min(turn => turn.X);
        var maxX = _turns.Max(turn => turn.X);
        if (maxX - minX < 260)
        {
            return false;
        }

        _lastTriggerTime = now;
        _turns.Clear();
        return true;
    }

    private void AddSample(ScreenPoint point, DateTime now)
    {
        _samples.Enqueue(new SamplePoint(point, now));
        ExpireOldSamples(now);
    }

    private void ExpireOldSamples(DateTime now)
    {
        while (_samples.Count > 0 && now - _samples.Peek().Time > TrailWindow)
        {
            _samples.Dequeue();
        }
    }

    private void ExpireOldTurns(DateTime now)
    {
        while (_turns.Count > 0 && now - _turns.Peek().Time > DetectionWindow)
        {
            _turns.Dequeue();
        }
    }

    private readonly record struct TurnPoint(int X, DateTime Time);

    private readonly record struct SamplePoint(ScreenPoint Point, DateTime Time);
}
