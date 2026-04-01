//------------------------------------------------------------------------------
// <copyright file="SqlScriptGeneratorVisitor.TSqlScript.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace Microsoft.SqlServer.TransactSql.ScriptDom.ScriptGenerator
{
    partial class SqlScriptGeneratorVisitor
    {
        public override void ExplicitVisit(TSqlScript node)
        {
            // Initialize token stream for comment preservation
            if (_options.PreserveComments && node.ScriptTokenStream != null)
            {
                SetTokenStreamForComments(node.ScriptTokenStream);
            }

            TSqlBatch prevBatch = null;
            foreach (var item in node.Batches)
            {
                if (prevBatch != null)
                {
                    NewLine();
                    GenerateKeyword(TSqlTokenType.Go);
                    NewLine();

                    if (_options.PreserveComments && _currentTokenStream != null)
                    {
                        int blanks = CountBlankLinesBetween(prevBatch.LastTokenIndex, item.FirstTokenIndex, mandatoryNewlines: 2);
                        for (int b = 0; b < Math.Min(blanks, 3); b++) NewLine();
                    }
                }

                GenerateFragmentIfNotNull(item);
                prevBatch = item;
            }

            // Emit any remaining comments at end of script (after the last statement)
            EmitRemainingComments();
        }
    }
}
