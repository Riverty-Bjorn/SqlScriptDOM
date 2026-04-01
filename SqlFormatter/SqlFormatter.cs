using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlServer.TransactSql.ScriptDom.ScriptGenerator;

namespace SqlScriptDom.Formatter;

/// <summary>
/// Formats T-SQL text using the ScriptDOM parser and script generator.
/// Comments and blank-line structure are preserved by default.
/// </summary>
public static class SqlFormatter
{
    private static readonly FormatterOptions DefaultOptions = new();

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Formats <paramref name="sql"/> and returns the formatted text.
    /// Throws <see cref="FormatException"/> if the SQL contains parse errors.
    /// Use <see cref="TryFormat"/> to handle errors without exceptions.
    /// </summary>
    /// <param name="sql">The T-SQL source text to format.</param>
    /// <param name="options">Formatting options; defaults are used when <c>null</c>.</param>
    /// <exception cref="FormatException">Thrown when parse errors are present.</exception>
    public static string Format(string sql, FormatterOptions? options = null)
    {
        var result = TryFormat(sql, options);
        if (result.HasErrors)
        {
            var message = BuildErrorMessage(result.ParseErrors);
            throw new FormatException(message);
        }
        return result.FormattedSql;
    }

    /// <summary>
    /// Formats <paramref name="sql"/> and returns a <see cref="FormatResult"/> that
    /// always contains output — even when parse errors were encountered — so callers
    /// can decide how to handle partial results.
    /// </summary>
    /// <param name="sql">The T-SQL source text to format.</param>
    /// <param name="options">Formatting options; defaults are used when <c>null</c>.</param>
    public static FormatResult TryFormat(string sql, FormatterOptions? options = null)
    {
        options ??= DefaultOptions;
        var parser = CreateParser(options);
        var fragment = parser.Parse(new StringReader(sql), out var errors);
        var generator = CreateGenerator(options);
        generator.GenerateScript(fragment, out var formatted);
        return new FormatResult(formatted, errors);
    }

    /// <summary>
    /// Reads a SQL file, formats it, and writes the result to <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="inputPath">Path to the input <c>.sql</c> file.</param>
    /// <param name="outputPath">Path to write the formatted output to.</param>
    /// <param name="options">Formatting options; defaults are used when <c>null</c>.</param>
    /// <param name="encoding">
    /// Encoding used for both reading and writing. Defaults to UTF-8 with BOM detection.
    /// </param>
    /// <returns>The <see cref="FormatResult"/> for the operation.</returns>
    public static FormatResult FormatFile(
        string inputPath,
        string outputPath,
        FormatterOptions? options = null,
        Encoding? encoding = null)
    {
        encoding ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var sql = File.ReadAllText(inputPath, encoding);
        var result = TryFormat(sql, options);
        File.WriteAllText(outputPath, result.FormattedSql, encoding);
        return result;
    }

    // ── Internals ──────────────────────────────────────────────────────────────

    private static TSqlParser CreateParser(FormatterOptions options)
    {
        bool qi = options.QuotedIdentifiers;
        return options.SqlVersion switch
        {
            SqlVersion.Sql80  => new TSql80Parser(qi),
            SqlVersion.Sql90  => new TSql90Parser(qi),
            SqlVersion.Sql100 => new TSql100Parser(qi),
            SqlVersion.Sql110 => new TSql110Parser(qi),
            SqlVersion.Sql120 => new TSql120Parser(qi),
            SqlVersion.Sql130 => new TSql130Parser(qi),
            SqlVersion.Sql140 => new TSql140Parser(qi),
            SqlVersion.Sql150 => new TSql150Parser(qi),
            SqlVersion.Sql160 => new TSql160Parser(qi),
            SqlVersion.Sql170 => new TSql170Parser(qi),
            _ => new TSql170Parser(qi),
        };
    }

    private static SqlScriptGenerator CreateGenerator(FormatterOptions options)
    {
        var genOptions = options.ToScriptGeneratorOptions();
        return options.SqlVersion switch
        {
            SqlVersion.Sql80  => new Sql80ScriptGenerator(genOptions),
            SqlVersion.Sql90  => new Sql90ScriptGenerator(genOptions),
            SqlVersion.Sql100 => new Sql100ScriptGenerator(genOptions),
            SqlVersion.Sql110 => new Sql110ScriptGenerator(genOptions),
            SqlVersion.Sql120 => new Sql120ScriptGenerator(genOptions),
            SqlVersion.Sql130 => new Sql130ScriptGenerator(genOptions),
            SqlVersion.Sql140 => new Sql140ScriptGenerator(genOptions),
            SqlVersion.Sql150 => new Sql150ScriptGenerator(genOptions),
            SqlVersion.Sql160 => new Sql160ScriptGenerator(genOptions),
            SqlVersion.Sql170 => new Sql170ScriptGenerator(genOptions),
            _ => new Sql170ScriptGenerator(genOptions),
        };
    }

    private static string BuildErrorMessage(IReadOnlyList<ParseError> errors)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SQL contained {errors.Count} parse error(s):");
        foreach (var e in errors)
            sb.AppendLine($"  Line {e.Line}, Column {e.Column}: {e.Message}");
        return sb.ToString().TrimEnd();
    }
}
