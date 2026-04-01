using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlScriptDom.Formatter;

/// <summary>
/// The result of a format operation, carrying both the formatted SQL and any
/// parse errors encountered. A result with errors still contains the best-effort
/// formatted output from a partial parse.
/// </summary>
public sealed class FormatResult
{
    internal FormatResult(string formattedSql, IList<ParseError> errors)
    {
        FormattedSql = formattedSql;
        ParseErrors = errors.ToList().AsReadOnly();
    }

    /// <summary>The formatted SQL text.</summary>
    public string FormattedSql { get; }

    /// <summary>Parse errors encountered while processing the input SQL.</summary>
    public IReadOnlyList<ParseError> ParseErrors { get; }

    /// <summary><c>true</c> when the input contained syntax the parser could not understand.</summary>
    public bool HasErrors => ParseErrors.Count > 0;

    /// <summary>Returns <see cref="FormattedSql"/>.</summary>
    public override string ToString() => FormattedSql;
}
