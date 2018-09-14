namespace ExceptionsAnalyzer
{
    internal enum StatementAnalysisResult
    {
        NoUsage,
        Used,
        RethrowSameException,
        RethrowWithoutInnerException,
        CorrectRethrow
    }
}
