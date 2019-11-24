using System;
using System.Diagnostics;

namespace MovieRecommendationSystem.Infrastructure
{
    public static class TimeUtilities
    {
        public static TimeSpan MeasureDuration(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        public static TimeSpan MeasureDuration<T>(Func<T> action, out T result)
        {
            var stopwatch = Stopwatch.StartNew();
            result = action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
    }
}
