namespace ExceptionsAnalyzer
{
    internal enum StatementAnalysisResult
    {
        Undefined = 0, // Shouldn't be returned from anywhere, only for .FirstOrDefault or similar cases
        // Negative options - should be short-circuited with error.
        RethrowSameException = 1,
        RethrowWithoutInnerException = 2,
        // Neutral case - should check further. If no usages at all, then means incorrect behavior
        NoUsage = 3,
        // Correct cases
        Used = 4,
        CorrectRethrow = 5
    }
}
