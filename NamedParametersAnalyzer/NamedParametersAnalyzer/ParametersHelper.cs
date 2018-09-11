using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NamedParametersAnalyzer
{
    internal static class ParametersHelper
    {
        public static IEnumerable<(string name, bool isParams)> GetInvocationParameters(SemanticModel semanticModel, ExpressionSyntax call, CancellationToken cancellationToken = default(CancellationToken))
        {
            var symbolInfo = semanticModel
                .GetSymbolInfo(call, cancellationToken);
            var invocationModel = symbolInfo.Symbol as IMethodSymbol;

            return invocationModel.Parameters.Select(x => (name: x.Name, isParams: x.IsParams));
        }
    }
}
