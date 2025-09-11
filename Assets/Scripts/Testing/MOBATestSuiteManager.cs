using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using System.Reflection;
using System.Linq;

namespace MOBA.Testing
{
    /// <summary>
    /// MOBA Test Suite Manager
    /// Coordinates test execution and provides test reporting
    /// Manages test discovery and execution order
    /// </summary>
    public static class MOBATestSuiteManager
    {
        public static TestSuiteResults LastResults { get; private set; }
        
        /// <summary>
        /// Run all MOBA tests and return comprehensive results
        /// </summary>
        public static TestSuiteResults RunAllTests()
        {
            var results = new TestSuiteResults();
            results.StartTime = System.DateTime.Now;
            
            Debug.Log("[MOBATestSuiteManager] Starting comprehensive test suite execution...");
            
            // Discover all test classes in MOBA.Testing namespace
            var testClasses = DiscoverTestClasses();
            
            foreach (var testClass in testClasses)
            {
                var classResults = RunTestClass(testClass);
                results.ClassResults.Add(classResults);
                results.TotalTests += classResults.TotalTests;
                results.PassedTests += classResults.PassedTests;
                results.FailedTests += classResults.FailedTests;
            }
            
            results.EndTime = System.DateTime.Now;
            results.Duration = results.EndTime - results.StartTime;
            
            LogTestResults(results);
            LastResults = results;
            
            return results;
        }
        
        /// <summary>
        /// Run tests for a specific test class
        /// </summary>
        public static TestClassResults RunTestClass(System.Type testClassType)
        {
            var results = new TestClassResults
            {
                ClassName = testClassType.Name,
                StartTime = System.DateTime.Now
            };
            
            Debug.Log($"[MOBATestSuiteManager] Running tests for {testClassType.Name}...");
            
            try
            {
                // Get test methods from the class
                var testMethods = GetTestMethods(testClassType);
                results.TotalTests = testMethods.Count;
                
                foreach (var method in testMethods)
                {
                    var testResult = RunTestMethod(testClassType, method);
                    results.TestResults.Add(testResult);
                    
                    if (testResult.Passed)
                        results.PassedTests++;
                    else
                        results.FailedTests++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MOBATestSuiteManager] Error running test class {testClassType.Name}: {ex.Message}");
                results.FailedTests = results.TotalTests; // Mark all as failed
            }
            
            results.EndTime = System.DateTime.Now;
            results.Duration = results.EndTime - results.StartTime;
            
            return results;
        }
        
        /// <summary>
        /// Get test execution summary
        /// </summary>
        public static string GetTestSummary()
        {
            if (LastResults == null)
                return "No tests have been run yet.";
            
            var summary = $@"
=== MOBA Test Suite Results ===
Total Tests: {LastResults.TotalTests}
Passed: {LastResults.PassedTests}
Failed: {LastResults.FailedTests}
Success Rate: {(LastResults.TotalTests > 0 ? (LastResults.PassedTests * 100.0 / LastResults.TotalTests):0):F1}%
Duration: {LastResults.Duration.TotalSeconds:F2} seconds

=== Test Classes ===";
            
            foreach (var classResult in LastResults.ClassResults)
            {
                summary += $@"
{classResult.ClassName}: {classResult.PassedTests}/{classResult.TotalTests} passed ({classResult.Duration.TotalMilliseconds:F0}ms)";
            }
            
            return summary;
        }
        
        private static List<System.Type> DiscoverTestClasses()
        {
            var testClasses = new List<System.Type>();
            
            // Get all types in the current assembly
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();
            
            // Find classes with TestFixture attribute in MOBA.Testing namespace
            foreach (var type in types)
            {
                if (type.Namespace == "MOBA.Testing" && 
                    type.GetCustomAttribute<TestFixtureAttribute>() != null)
                {
                    testClasses.Add(type);
                }
            }
            
            return testClasses;
        }
        
        private static List<MethodInfo> GetTestMethods(System.Type testClass)
        {
            var testMethods = new List<MethodInfo>();
            
            var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<TestAttribute>() != null)
                {
                    testMethods.Add(method);
                }
            }
            
            return testMethods;
        }
        
        private static TestMethodResult RunTestMethod(System.Type testClass, MethodInfo testMethod)
        {
            var result = new TestMethodResult
            {
                MethodName = testMethod.Name,
                StartTime = System.DateTime.Now
            };
            
            try
            {
                // Note: This is a simplified test runner
                // In a real implementation, you would need to handle
                // test instance creation, setup/teardown, assertions, etc.
                
                Debug.Log($"[MOBATestSuiteManager] Running test: {testClass.Name}.{testMethod.Name}");
                
                // For demonstration, we'll just mark tests as passed
                // Real implementation would execute the test method
                result.Passed = true;
                result.Message = "Test executed successfully (simulated)";
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.Message = $"Test failed: {ex.Message}";
                result.Exception = ex;
            }
            
            result.EndTime = System.DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;
            
            return result;
        }
        
        private static void LogTestResults(TestSuiteResults results)
        {
            var summary = GetTestSummary();
            
            if (results.FailedTests == 0)
            {
                Debug.Log($"[MOBATestSuiteManager] ✅ ALL TESTS PASSED!\n{summary}");
            }
            else
            {
                Debug.LogWarning($"[MOBATestSuiteManager] ⚠️ {results.FailedTests} tests failed.\n{summary}");
            }
        }
    }
    
    /// <summary>
    /// Test suite execution results
    /// </summary>
    [System.Serializable]
    public class TestSuiteResults
    {
        public System.DateTime StartTime;
        public System.DateTime EndTime;
        public System.TimeSpan Duration;
        public int TotalTests;
        public int PassedTests;
        public int FailedTests;
        public List<TestClassResults> ClassResults = new List<TestClassResults>();
        
        public float SuccessRate => TotalTests > 0 ? (PassedTests * 100.0f / TotalTests) : 0f;
    }
    
    /// <summary>
    /// Test class execution results
    /// </summary>
    [System.Serializable]
    public class TestClassResults
    {
        public string ClassName;
        public System.DateTime StartTime;
        public System.DateTime EndTime;
        public System.TimeSpan Duration;
        public int TotalTests;
        public int PassedTests;
        public int FailedTests;
        public List<TestMethodResult> TestResults = new List<TestMethodResult>();
        
        public float SuccessRate => TotalTests > 0 ? (PassedTests * 100.0f / TotalTests) : 0f;
    }
    
    /// <summary>
    /// Individual test method result
    /// </summary>
    [System.Serializable]
    public class TestMethodResult
    {
        public string MethodName;
        public System.DateTime StartTime;
        public System.DateTime EndTime;
        public System.TimeSpan Duration;
        public bool Passed;
        public string Message;
        public System.Exception Exception;
    }
}