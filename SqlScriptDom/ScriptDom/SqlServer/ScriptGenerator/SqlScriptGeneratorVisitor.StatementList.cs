//------------------------------------------------------------------------------
// <copyright file="SqlScriptGeneratorVisitor.StatementList.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace Microsoft.SqlServer.TransactSql.ScriptDom.ScriptGenerator
{
    partial class SqlScriptGeneratorVisitor
    {
        public override void ExplicitVisit(StatementList node)
        {
            if (node.Statements != null)
            {
                for (int i = 0; i < node.Statements.Count; i++)
                {
                    TSqlStatement statement = node.Statements[i];
                    if (i > 0)
                    {
                        int newlineCount;
                        if (_options.PreserveComments && _currentTokenStream != null)
                        {
                            int blankLines = CountBlankLinesBetween(node.Statements[i - 1].LastTokenIndex, statement.FirstTokenIndex);
                            newlineCount = 1 + Math.Min(blankLines, 3);
                        }
                        else
                        {
                            newlineCount = _options.NumNewlinesAfterStatement;
                        }
                        for (int n = 0; n < newlineCount; n++) NewLine();
                    }

                    GenerateFragmentIfNotNull(statement);
                    GenerateSemiColonWhenNecessary(statement);
                }
            }
        }
    }
}
