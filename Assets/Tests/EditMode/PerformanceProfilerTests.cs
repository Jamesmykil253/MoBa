using System.Threading;
using NUnit.Framework;
using MOBA.Debugging;

namespace MOBA.Tests.EditMode
{
    public class PerformanceProfilerTests
    {
        [SetUp]
        public void SetUp()
        {
            PerformanceProfiler.Clear();
        }

        [Test]
        public void MeasureScope_RecordsSamples()
        {
            using (PerformanceProfiler.Measure("EditMode/TestSample"))
            {
                Thread.Sleep(5);
            }

            var snapshot = PerformanceProfiler.GetSnapshot();
            Assert.IsTrue(snapshot.ContainsKey("EditMode/TestSample"));
            var metrics = snapshot["EditMode/TestSample"];
            Assert.Greater(metrics.AverageMilliseconds, 0f);
            Assert.Greater(metrics.SampleCount, 0);
        }

        [Test]
        public void BeginAndEndSample_CapturesTiming()
        {
            var token = PerformanceProfiler.BeginSample("EditMode/ManualSample");
            Thread.Sleep(2);
            PerformanceProfiler.EndSample(token);

            var snapshot = PerformanceProfiler.GetSnapshot();
            Assert.That(snapshot.ContainsKey("EditMode/ManualSample"));
            var metrics = snapshot["EditMode/ManualSample"];
            Assert.That(metrics.MaxMilliseconds, Is.GreaterThanOrEqualTo(metrics.MinMilliseconds));
        }
    }
}
