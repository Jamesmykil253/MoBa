using MOBA.Services;
using NUnit.Framework;

namespace MOBA.Tests.EditMode
{
    public class ServiceRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceRegistry.Clear();
        }

        [Test]
        public void RegisterAndResolve_ReturnsSameInstance()
        {
            var service = new ScoringService(2);
            ServiceRegistry.Register<IScoringService>(service);

            var resolved = ServiceRegistry.Resolve<IScoringService>();
            Assert.AreSame(service, resolved);
        }

        [Test]
        public void Register_DoesNotOverwriteWhenRequested()
        {
            var first = new ScoringService(2);
            var second = new ScoringService(2);

            ServiceRegistry.Register<IScoringService>(first);
            ServiceRegistry.Register<IScoringService>(second, overwrite: false);

            var resolved = ServiceRegistry.Resolve<IScoringService>();
            Assert.AreSame(first, resolved);
        }
    }
}
