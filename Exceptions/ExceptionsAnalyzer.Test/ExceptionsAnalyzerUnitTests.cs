using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestCommon;

namespace ExceptionsAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        private string CodeTemplate = @"
using System;

namespace ConsoleApp1
{{
    class TestClass
    {{
        public void TestMethod()
        {{
            try
            {{
            }}
            {0}
        }}
    }}
}}";

        [TestMethod]
        public void EmptyCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void CorrectRethrow()
        {
            var test = @"
            catch
            {
                throw;
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ExceptionUsedInCatch()
        {
            var test = @"
            catch(Exception e)
            {
                var s = e;
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void IncorrectRethrow()
        {
            var test = @"
            catch(Exception e)
            {
                throw e;
            }";
            var code = BuildCode(test);

            var expected = new DiagnosticResult
            {
                Id = Descriptors.RethrowSameExceptionId,
                Message = Descriptors.RethrowSameExceptionMessage,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 16, 17)
                    }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void RethrowWithoutInner()
        {
            var test = @"
            catch(Exception e)
            {
                throw new Exception(""test"");
            }";
            var code = BuildCode(test);

            var expected = GetDiagnostic(Descriptors.RethrowWithoutInnerExceptionId, Descriptors.RethrowWithoutInnerMessage, DiagnosticSeverity.Info);

            VerifyCSharpDiagnostic(code, expected);
        }

        private DiagnosticResult GetDiagnostic(string diagnosticId, string description, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
        {
            return new DiagnosticResult
            {
                Id = diagnosticId,
                Message = description,
                Severity = severity,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 14, 13)
                    }
            };
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ExceptionsAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExceptionsAnalyzer();
        }

        private string BuildCode(string catchClause) => string.Format(CodeTemplate, catchClause);
    }
}
