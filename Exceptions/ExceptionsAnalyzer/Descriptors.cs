using Microsoft.CodeAnalysis;

namespace ExceptionsAnalyzer
{
    public static class Descriptors
    {
        private static readonly string Title = "Exception analyzer";
       
        private const string Category = "Code correctness";

        public const string NoExceptionUsageId = "E001";
        public static readonly string NoExceptionUsageMessage = "Swallowing exceptions without handling considered as bad practice";
        internal static readonly DiagnosticDescriptor NoExceptionUsageDescriptor = new DiagnosticDescriptor(
            id: NoExceptionUsageId, 
            title: Title, 
            messageFormat: NoExceptionUsageMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning, 
            isEnabledByDefault: true,
            description: NoExceptionUsageMessage);

        public const string RethrowSameExceptionId = "E002";
        public static readonly string RethrowSameExceptionMessage = "Rethrow same exception lead to lost of call stack";
        internal static readonly DiagnosticDescriptor RethrowSameExceptionDescriptor = new DiagnosticDescriptor(
            RethrowSameExceptionId,
            Title, 
            RethrowSameExceptionMessage, 
            Category, 
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true, 
            description: RethrowSameExceptionMessage);

        public const string RethrowWithoutInnerExceptionId = "E003";
        public static readonly string RethrowWithoutInnerMessage = "Rethrow same exception lead to lost of call stack";
        internal static readonly DiagnosticDescriptor RethrowWithoutInnerDescriptor = new DiagnosticDescriptor(
            RethrowWithoutInnerExceptionId,
            Title,
            RethrowWithoutInnerMessage, 
            Category, 
            DiagnosticSeverity.Info,
            isEnabledByDefault: true, 
            description: RethrowWithoutInnerMessage);

        internal const string ExceptionAnalyzerErrorId = "E000";
        private static readonly string ExceptionAnalyzerErrorMessage = "Swallowing exceptions without handling considered as bad practice";
        internal static readonly DiagnosticDescriptor ExceptionAnalyzerErrorDescriptor = new DiagnosticDescriptor(
            id: ExceptionAnalyzerErrorId,
            title: Title,
            messageFormat: NoExceptionUsageMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: NoExceptionUsageMessage);
    }
}
