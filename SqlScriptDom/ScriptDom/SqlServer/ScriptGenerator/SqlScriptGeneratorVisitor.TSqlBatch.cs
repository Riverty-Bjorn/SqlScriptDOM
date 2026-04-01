//------------------------------------------------------------------------------
// <copyright file="SqlScriptGeneratorVisitor.TSqlBatch.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.SqlServer.TransactSql.ScriptDom.ScriptGenerator
{
    partial class SqlScriptGeneratorVisitor
    {
        public override void ExplicitVisit(TSqlBatch node)
        {
            for (int i = 0; i < node.Statements.Count; i++)
            {
                TSqlStatement statement = node.Statements[i];
                GenerateFragmentIfNotNull(statement);

                GenerateSemiColonWhenNecessary(statement);

                bool isLastStatementInBatch = i == node.Statements.Count - 1;
                if (!isLastStatementInBatch && statement is TSqlStatementSnippet == false)
                {
                    int newlineCount;
                    if (_options.PreserveComments && _currentTokenStream != null)
                    {
                        int blankLines = CountBlankLinesBetween(statement.LastTokenIndex, node.Statements[i + 1].FirstTokenIndex);
                        newlineCount = 1 + Math.Min(blankLines, 3); // 1 for line-break + preserved blank lines
                    }
                    else
                    {
                        newlineCount = 2; // default: one blank line
                    }
                    for (int n = 0; n < newlineCount; n++) NewLine();
                }
            }
        }
    }
}
