using MOBA.Services;
using NUnit.Framework;

namespace MOBA.Tests.EditMode
{
    public class MatchLifecycleServiceTests
    {
        private ScoringService scoring;
        private MatchLifecycleService service;

        [SetUp]
        public void SetUp()
        {
            scoring = new ScoringService(2);
            service = new MatchLifecycleService(scoring);
            service.Configure(120f, 10);
        }

        [Test]
        public void StartMatch_ResetsScoresAndActivates()
        {
            scoring.AddScore(0, 5);
            Assert.IsTrue(service.StartMatch());
            Assert.IsTrue(service.IsActive);
            Assert.AreEqual(0, scoring.GetScore(0));
            Assert.AreEqual(120f, service.TimeRemaining);
        }

        [Test]
        public void Tick_TimeRunsOut_EndsMatchWithTimeout()
        {
            service.StartMatch();
            int winner = -2;
            service.MatchEnded += w => winner = w;

            service.Tick(60f);
            Assert.IsTrue(service.IsActive);
            service.Tick(61f);

            Assert.AreEqual(-1, winner);
            Assert.IsFalse(service.IsActive);
            Assert.AreEqual(0f, service.TimeRemaining);
        }

        [Test]
        public void Tick_ScoreReachesTarget_AnnouncesWinner()
        {
            service.StartMatch();
            int winner = -2;
            service.MatchEnded += w => winner = w;

            scoring.AddScore(1, 10);
            service.Tick(0f);

            Assert.AreEqual(1, winner);
            Assert.IsFalse(service.IsActive);
        }
    }
}
