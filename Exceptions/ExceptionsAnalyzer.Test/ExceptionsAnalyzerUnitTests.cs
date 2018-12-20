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
        public void SwallowException()
        {
            var test = @"
            catch
            {
            }";
            var code = BuildCode(test);

            var expected = new DiagnosticResult
            {
                Id = Descriptors.NoExceptionUsageId,
                Message = Descriptors.NoExceptionUsageMessage,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 14, 13)
                    }
            };

            VerifyCSharpDiagnostic(code, expected);
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
        public void IfThenInCatch()
        {
            var test = @"
            catch(Exception e)
            {
                if (true)
                {
                    throw;
                }
                else
                {
                    // something
                }
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void IfElseInCatch()
        {
            var test = @"
            catch(Exception e)
            {
                if (true)
                {
                    // something
                }
                else
                {
                    throw;
                }
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void IfConditionInCatch()
        {
            var test = @"
            catch(Exception e)
            {
                if (string.IsNullOrEmpty(e.Message))
                {
                    // something
                }
                else
                {
                    // something
                }
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void IfElseIncorrectRethrowInCatch()
        {
            var test = @"
            catch(Exception e)
            {
                if (string.IsNullOrEmpty(e.Message))
                {
                    throw;
                }
                else
                {
                    throw new Exception();
                }
            }";
            var code = BuildCode(test);

            var expected = new DiagnosticResult
            {
                Id = Descriptors.RethrowWithoutInnerExceptionId,
                Message = Descriptors.RethrowWithoutInnerMessage,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 14, 13)
                    }
            };

            VerifyCSharpDiagnostic(code, expected);
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

            var expected = GetDiagnostic(Descriptors.RethrowWithoutInnerExceptionId, Descriptors.RethrowWithoutInnerMessage, DiagnosticSeverity.Warning);

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ExceptionDetailsVerifiedCorrect()
        {
            var test = @"
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ExceptionUsedInReturn()
        {
            var test = @"
            catch(Exception e)
            {
               return $""Exception happened {e}"";
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ExceptionUsedInStringInterpolation()
        {
            var test = @"
            catch(Exception e)
            {
                Console.WriteLine($""Exception happened {e}"");
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void NoVariableName_Correct()
        {
            var test = @"
            catch (FileNotFoundException)
            {
                return;
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void InnerTryCatch_Correct()
        {
            var test = @"
            catch (Exception e)
            {
                try
                {
                    Console.WriteLine($""Exception happened! {e}"");
                }
                catch
                {
                    throw;
                }
            }";
            var code = BuildCode(test);

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void InnerTryCatch_Incorrect()
        {
            var test = @"
            catch (Exception e)
            {
                try
                {
                    var k = e;
                }
                catch(Exception inner)
                {
                    throw new Exception();
                }
            }";
            var code = BuildCode(test);

            var expected = new DiagnosticResult
            {
                Id = Descriptors.RethrowWithoutInnerExceptionId,
                Message = Descriptors.RethrowWithoutInnerMessage,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 20, 17)
                    }
            };

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
