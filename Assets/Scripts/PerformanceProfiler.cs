using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;

namespace MOBA.Debugging
{
    /// <summary>
    /// Lightweight profiling helper for ad-hoc runtime measurements. Integrates with Unity's Profiler
    /// while keeping a rolling window of timing statistics accessible at runtime for debug overlays.
    /// </summary>
    public static class PerformanceProfiler
    {
        private static readonly double TickToMilliseconds = 1000d / Stopwatch.Frequency;
        private static readonly ConcurrentDictionary<string, SampleAccumulator> Accumulators = new();

        public static SampleToken BeginSample(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Sample name must be provided.", nameof(name));
            }

            Profiler.BeginSample(name);
            long startTicks = Stopwatch.GetTimestamp();
            return new SampleToken(name, startTicks, true);
        }

        public static void EndSample(SampleToken token)
        {
            if (!token.IsValid)
            {
                return;
            }

            if (token.UsesProfiler)
            {
                Profiler.EndSample();
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - token.StartTicks;
            double milliseconds = elapsedTicks * TickToMilliseconds;
            var accumulator = Accumulators.GetOrAdd(token.Name, _ => new SampleAccumulator());
            accumulator.Record(milliseconds);
        }

        public static IDisposable Measure(string name)
        {
            return new SampleScope(name);
        }

        public static IReadOnlyDictionary<string, SampleMetrics> GetSnapshot()
        {
            var snapshot = new Dictionary<string, SampleMetrics>();
            foreach (var pair in Accumulators)
            {
                snapshot[pair.Key] = pair.Value.GetMetrics();
            }

            return snapshot;
        }

        public static void Clear()
        {
            Accumulators.Clear();
        }

        public readonly struct SampleToken
        {
            internal SampleToken(string name, long startTicks, bool usesProfiler)
            {
                Name = name;
                StartTicks = startTicks;
                UsesProfiler = usesProfiler;
                IsValid = true;
            }

            public string Name { get; }
            internal long StartTicks { get; }
            internal bool UsesProfiler { get; }
            internal bool IsValid { get; }
        }

        private sealed class SampleAccumulator
        {
            private const int WindowSize = 120;
            private readonly double[] samples = new double[WindowSize];
            private readonly object gate = new object();
            private int sampleCount;
            private int nextIndex;
            private double total;

            public void Record(double value)
            {
                lock (gate)
                {
                    if (sampleCount < WindowSize)
                    {
                        samples[nextIndex] = value;
                        total += value;
                        sampleCount++;
                        nextIndex = (nextIndex + 1) % WindowSize;
                    }
                    else
                    {
                        double replaced = samples[nextIndex];
                        total = total - replaced + value;
                        samples[nextIndex] = value;
                        nextIndex = (nextIndex + 1) % WindowSize;
                    }
                }
            }

            public SampleMetrics GetMetrics()
            {
                lock (gate)
                {
                    if (sampleCount == 0)
                    {
                        return default;
                    }

                    int length = sampleCount;
                    double min = double.MaxValue;
                    double max = double.MinValue;

                    for (int i = 0; i < length; i++)
                    {
                        double value = samples[i];
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }

                    double average = total / length;
                    return new SampleMetrics((float)average, (float)min, (float)max, length);
                }
            }
        }

        public readonly struct SampleMetrics
        {
            public SampleMetrics(float averageMilliseconds, float minMilliseconds, float maxMilliseconds, int sampleCount)
            {
                AverageMilliseconds = averageMilliseconds;
                MinMilliseconds = minMilliseconds;
                MaxMilliseconds = maxMilliseconds;
                SampleCount = sampleCount;
            }

            public float AverageMilliseconds { get; }
            public float MinMilliseconds { get; }
            public float MaxMilliseconds { get; }
            public int SampleCount { get; }
        }

        private readonly struct SampleScope : IDisposable
        {
            private readonly SampleToken token;

            public SampleScope(string sampleName)
            {
                token = BeginSample(sampleName);
            }

            public void Dispose()
            {
                EndSample(token);
            }
        }
    }
}
