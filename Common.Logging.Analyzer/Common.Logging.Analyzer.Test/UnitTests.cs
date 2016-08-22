using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Common.Logging.Analyzer;

namespace Common.Logging.Analyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        [TestMethod]
        public void ShouldPassIfFastMethodIsUsed()
        {
            var analysisTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger<MyClass>();
        }
    }";

            VerifyCSharpDiagnostic(analysisTest);
        }

        [TestMethod]
        public void ShouldFailIfSlowMethodIsUsed()
        {
            var analysisTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetCurrentClassLogger();
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "CommonLoggingAnalyzer",
                Message = "Do not use GetCurrentClassLogger() method - use GetLogger<> instead",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 39)
                        }
            };

            VerifyCSharpDiagnostic(analysisTest, expected);

            var quickFixTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger<MyClass>();
        }
    }";
            VerifyCSharpFix(analysisTest, quickFixTest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CommonLoggingAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CommonLoggingAnalyzerAnalyzer();
        }
    }
}