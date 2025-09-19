using System;
using System.Collections.Generic;
using MOBA.ErrorHandling;
using NUnit.Framework;

namespace MOBA.Tests.EditMode
{
    public class ErrorHandlerTests
    {
        private Action<ErrorLog> loggedHandler;
        private Action<ErrorSeverity> thresholdHandler;

        [SetUp]
        public void SetUp()
        {
            ErrorHandler.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (loggedHandler != null)
            {
                ErrorHandler.OnErrorLogged -= loggedHandler;
                loggedHandler = null;
            }
            if (thresholdHandler != null)
            {
                ErrorHandler.OnErrorThresholdExceeded -= thresholdHandler;
                thresholdHandler = null;
            }
            ErrorHandler.Dispose();
        }

        [Test]
        public void LogError_RaisesEventAndUpdatesCounts()
        {
            ErrorLog captured = null;
            loggedHandler = log => captured = log;
            ErrorHandler.OnErrorLogged += loggedHandler;

            ErrorHandler.LogError("TestContext", "Test message");

            Assert.IsNotNull(captured);
            Assert.AreEqual(ErrorSeverity.Error, captured.Severity);
            Assert.AreEqual("TestContext", captured.Context);

            var counts = GetPrivateField<Dictionary<ErrorSeverity, int>>("errorCounts");
            Assert.AreEqual(1, counts[ErrorSeverity.Error]);
        }

        [Test]
        public void LogCritical_TriggersThresholdEvent()
        {
            bool thresholdRaised = false;
            thresholdHandler = severity =>
            {
                if (severity == ErrorSeverity.Critical)
                {
                    thresholdRaised = true;
                }
            };
            ErrorHandler.OnErrorThresholdExceeded += thresholdHandler;

            ErrorHandler.LogCritical("ThresholdContext", "Critical failure");

            Assert.IsTrue(thresholdRaised, "Expected critical error threshold event to fire.");
        }

        private static T GetPrivateField<T>(string fieldName)
        {
            var field = typeof(ErrorHandler).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (T)field.GetValue(null);
        }
    }
}
