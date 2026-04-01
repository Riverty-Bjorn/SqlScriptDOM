using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlServer.TransactSql.ScriptDom.ScriptGenerator;

namespace SqlScriptDom.Formatter;

/// <summary>
/// Options controlling how SQL is formatted by <see cref="SqlFormatter"/>.
/// All properties have opinionated defaults suited to readable, comment-preserving output.
/// </summary>
public sealed class FormatterOptions
{
    /// <summary>
    /// Target SQL Server version used for both parsing and script generation.
    /// Defaults to <see cref="SqlVersion.Sql170"/> (SQL Server 2025), which supports
    /// the broadest syntax. Lower versions will reject syntax they don't know.
    /// </summary>
    public SqlVersion SqlVersion { get; set; } = SqlVersion.Sql170;

    /// <summary>
    /// Whether to preserve comments from the original SQL in the output.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// Casing applied to T-SQL keywords. Defaults to <see cref="KeywordCasing.Uppercase"/>.
    /// </summary>
    public KeywordCasing KeywordCasing { get; set; } = KeywordCasing.Uppercase;

    /// <summary>Number of spaces per indentation level. Defaults to 4.</summary>
    public int IndentationSize { get; set; } = 4;

    /// <summary>Whether quoted identifiers are enabled during parsing. Defaults to <c>true</c>.</summary>
    public bool QuotedIdentifiers { get; set; } = true;

    // ── Clause placement ──────────────────────────────────────────────────────

    /// <summary>Emit FROM on a new line. Defaults to <c>true</c>.</summary>
    public bool NewLineBeforeFromClause { get; set; } = true;

    /// <summary>Emit WHERE on a new line. Defaults to <c>true</c>.</summary>
    public bool NewLineBeforeWhereClause { get; set; } = true;

    /// <summary>Emit ORDER BY on a new line. Defaults to <c>true</c>.</summary>
    public bool NewLineBeforeOrderByClause { get; set; } = true;

    /// <summary>Emit GROUP BY on a new line. Defaults to <c>true</c>.</summary>
    public bool NewLineBeforeGroupByClause { get; set; } = true;

    /// <summary>Emit HAVING on a new line. Defaults to <c>true</c>.</summary>
    public bool NewLineBeforeHavingClause { get; set; } = true;

    /// <summary>
    /// Converts this instance into the underlying <see cref="SqlScriptGeneratorOptions"/>
    /// consumed by ScriptDOM's script generator.
    /// </summary>
    internal SqlScriptGeneratorOptions ToScriptGeneratorOptions() => new()
    {
        SqlVersion = SqlVersion,
        PreserveComments = PreserveComments,
        KeywordCasing = KeywordCasing,
        IndentationSize = IndentationSize,
        NewLineBeforeFromClause = NewLineBeforeFromClause,
        NewLineBeforeWhereClause = NewLineBeforeWhereClause,
        NewLineBeforeOrderByClause = NewLineBeforeOrderByClause,
        NewLineBeforeGroupByClause = NewLineBeforeGroupByClause,
        NewLineBeforeHavingClause = NewLineBeforeHavingClause,
    };
}
