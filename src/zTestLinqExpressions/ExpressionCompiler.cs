using System;
using System.Linq.Expressions;
using zTestInterfaces;

namespace zTestLinqExpressions;

/// <summary>
/// Class that uses LINQ Expressions with Compile() to dynamically generate code.
/// This assembly demonstrates risky behavior that should be detected by AssemblyPreflightAnalyzer.
/// </summary>
public class ExpressionCompiler : IClass1
{
    public string Stuff(string input)
    {
        // Create a LINQ expression tree and compile it to a delegate
        var parameter = Expression.Parameter(typeof(string), "x");
        var expression = Expression.Lambda<Func<string, string>>(
            Expression.Call(
                parameter,
                typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!
            ),
            parameter
        );
        
        var compiled = expression.Compile();
        var result = compiled(input);
        return result + " - compiled at runtime";
    }
}
