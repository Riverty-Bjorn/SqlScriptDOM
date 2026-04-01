//------------------------------------------------------------------------------
// <copyright file="SqlScriptGeneratorVisitor.Comments.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.TransactSql.ScriptDom.ScriptGenerator
{
    internal abstract partial class SqlScriptGeneratorVisitor
    {
        #region Comment Tracking Fields

        /// <summary>
        /// Tracks the last token index processed for comment emission.
        /// Used to find comments between visited fragments.
        /// </summary>
        private int _lastProcessedTokenIndex = -1;

        /// <summary>
        /// The current script's token stream, set when visiting begins.
        /// </summary>
        private IList<TSqlParserToken> _currentTokenStream;

        /// <summary>
        /// Tracks which comment tokens have already been emitted to avoid duplicates.
        /// </summary>
        private readonly HashSet<int> _emittedCommentIndexes = new HashSet<int>();

        /// <summary>
        /// Tracks whether leading (file-level) comments have been emitted.
        /// </summary>
        private bool _leadingCommentsEmitted = false;

        #endregion

        #region Comment Preservation Methods

        /// <summary>
        /// Sets the token stream for comment tracking.
        /// Call this before visiting the root node when PreserveComments is enabled.
        /// </summary>
        /// <param name="tokenStream">The token stream from the parsed script.</param>
        protected void SetTokenStreamForComments(IList<TSqlParserToken> tokenStream)
        {
            _currentTokenStream = tokenStream;
            _lastProcessedTokenIndex = -1;
            _emittedCommentIndexes.Clear();
            _leadingCommentsEmitted = false;
        }

        /// <summary>
        /// Emits comments that appear before the first fragment in the script (file-level leading comments).
        /// Called once when generating the first fragment.
        /// </summary>
        /// <param name="fragment">The first fragment being generated.</param>
        protected void EmitLeadingComments(TSqlFragment fragment)
        {
            if (!_options.PreserveComments || _currentTokenStream == null || fragment == null)
            {
                return;
            }

            if (fragment.FirstTokenIndex <= 0)
            {
                return;
            }

            for (int i = 0; i < fragment.FirstTokenIndex && i < _currentTokenStream.Count; i++)
            {
                var token = _currentTokenStream[i];
                if (IsCommentToken(token) && !_emittedCommentIndexes.Contains(i))
                {
                    EmitCommentToken(token, isLeading: true);
                    _emittedCommentIndexes.Add(i);
                    _lastProcessedTokenIndex = i;
                }
            }
        }

        /// <summary>
        /// Emits comments that appear in the gap between the last emitted token and the current fragment.
        /// This captures comments embedded within sub-expressions.
        /// </summary>
        /// <param name="fragment">The fragment about to be generated.</param>
        protected void EmitGapComments(TSqlFragment fragment)
        {
            if (!_options.PreserveComments || _currentTokenStream == null || fragment == null)
            {
                return;
            }

            int startIndex = _lastProcessedTokenIndex + 1;
            int endIndex = fragment.FirstTokenIndex;

            if (endIndex <= startIndex)
            {
                return;
            }

            for (int i = startIndex; i < endIndex && i < _currentTokenStream.Count; i++)
            {
                var token = _currentTokenStream[i];
                if (IsCommentToken(token) && !_emittedCommentIndexes.Contains(i))
                {
                    EmitCommentToken(token, isLeading: true);
                    _emittedCommentIndexes.Add(i);
                    _lastProcessedTokenIndex = i;
                }
            }
        }

        /// <summary>
        /// Emits trailing comments that appear immediately after the fragment.
        /// </summary>
        /// <param name="fragment">The fragment that was just generated.</param>
        protected void EmitTrailingComments(TSqlFragment fragment)
        {
            if (!_options.PreserveComments || _currentTokenStream == null || fragment == null)
            {
                return;
            }

            int lastTokenIndex = fragment.LastTokenIndex;
            if (lastTokenIndex < 0 || lastTokenIndex >= _currentTokenStream.Count)
            {
                return;
            }

            // Scan for comments immediately following the fragment
            for (int i = lastTokenIndex + 1; i < _currentTokenStream.Count; i++)
            {
                var token = _currentTokenStream[i];

                if (IsCommentToken(token) && !_emittedCommentIndexes.Contains(i))
                {
                    EmitCommentToken(token, isLeading: ShouldEmitTrailingCommentAsLeading(lastTokenIndex + 1, i));
                    _emittedCommentIndexes.Add(i);
                    _lastProcessedTokenIndex = i;
                }
                else if (token.TokenType != TSqlTokenType.WhiteSpace)
                {
                    // Stop at next non-whitespace, non-comment token
                    break;
                }
            }
        }

        private bool ShouldEmitTrailingCommentAsLeading(int startIndex, int commentIndex)
        {
            for (int i = startIndex; i < commentIndex && i < _currentTokenStream.Count; i++)
            {
                var token = _currentTokenStream[i];
                if (token.TokenType == TSqlTokenType.WhiteSpace &&
                    token.Text != null &&
                    (token.Text.Contains("\r") || token.Text.Contains("\n")))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts blank lines in whitespace tokens between two token-stream positions,
        /// skipping batch separators (semicolons, GO) and stopping at the first comment
        /// or other real token. Returns the number of blank lines beyond the
        /// <paramref name="mandatoryNewlines"/> the caller already emits unconditionally.
        /// </summary>
        /// <param name="startExclusive">Token index after which scanning begins (exclusive).</param>
        /// <param name="endExclusive">Token index at which scanning stops (exclusive).</param>
        /// <param name="mandatoryNewlines">
        /// Newlines the caller will emit regardless (subtracted from the raw count).
        /// Use 1 for statement boundaries; use 2 for inter-batch gaps where the caller
        /// emits a newline before and after GO.
        /// </param>
        protected int CountBlankLinesBetween(int startExclusive, int endExclusive, int mandatoryNewlines = 1)
        {
            if (_currentTokenStream == null)
                return 0;

            int newlineCount = 0;
            for (int i = startExclusive + 1; i < endExclusive && i < _currentTokenStream.Count; i++)
            {
                var token = _currentTokenStream[i];
                if (token.TokenType == TSqlTokenType.WhiteSpace && token.Text != null)
                {
                    foreach (char c in token.Text)
                        if (c == '\n') newlineCount++;
                }
                else if (token.TokenType == TSqlTokenType.Semicolon ||
                         token.TokenType == TSqlTokenType.Go)
                {
                    continue; // skip batch separators
                }
                else
                {
                    break; // stop at first comment or real code token
                }
            }

            return Math.Max(0, newlineCount - mandatoryNewlines);
        }

        /// <summary>
        /// Updates tracking after generating a fragment.
        /// </summary>
        /// <param name="fragment">The fragment that was just generated.</param>
        protected void UpdateLastProcessedIndex(TSqlFragment fragment)
        {
            if (fragment != null && fragment.LastTokenIndex > _lastProcessedTokenIndex)
            {
                _lastProcessedTokenIndex = fragment.LastTokenIndex;
            }
        }

        /// <summary>
        /// Called from GenerateFragmentIfNotNull to handle comments before generating a fragment.
        /// This is the key integration point that enables comments within sub-expressions.
        /// </summary>
        /// <param name="fragment">The fragment about to be generated.</param>
        protected void HandleCommentsBeforeFragment(TSqlFragment fragment)
        {
            if (!_options.PreserveComments || _currentTokenStream == null || fragment == null)
            {
                return;
            }

            // Emit file-level leading comments once
            if (!_leadingCommentsEmitted)
            {
                EmitLeadingComments(fragment);
                _leadingCommentsEmitted = true;
            }

            // Emit any comments in the gap between last processed token and this fragment
            EmitGapComments(fragment);
        }

        /// <summary>
        /// Called from GenerateFragmentIfNotNull to handle comments after generating a fragment.
        /// </summary>
        /// <param name="fragment">The fragment that was just generated.</param>
        protected void HandleCommentsAfterFragment(TSqlFragment fragment)
        {
            if (!_options.PreserveComments || _currentTokenStream == null || fragment == null)
            {
                return;
            }

            if (fragment is TSqlStatement)
            {
                UpdateLastProcessedIndex(fragment);
                return;
            }

            // Emit trailing comments and update tracking
            EmitTrailingComments(fragment);
            UpdateLastProcessedIndex(fragment);
        }

        /// <summary>
        /// Called after generating a statement terminator so trailing statement comments stay after it.
        /// </summary>
        /// <param name="statement">The statement that was just fully generated.</param>
        protected void HandleCommentsAfterStatement(TSqlStatement statement)
        {
            if (!_options.PreserveComments || _currentTokenStream == null || statement == null)
            {
                return;
            }

            EmitTrailingComments(statement);
            UpdateLastProcessedIndex(statement);
        }

        /// <summary>
        /// Emits a comment token to the output.
        /// </summary>
        /// <param name="token">The comment token.</param>
        /// <param name="isLeading">True if this is a leading comment, false for trailing.</param>
        private void EmitCommentToken(TSqlParserToken token, bool isLeading)
        {
            if (token == null)
            {
                return;
            }

            if (isLeading && _writer.IsAtLineStart == false)
            {
                _writer.NewLine();
            }
            else if (!isLeading && _writer.IsAtLineStart == false)
            {
                _writer.AddToken(ScriptGeneratorSupporter.CreateWhitespaceToken(1));
            }

            if (token.TokenType == TSqlTokenType.SingleLineComment)
            {
                _writer.AddToken(new TSqlParserToken(TSqlTokenType.SingleLineComment, token.Text));

                if (isLeading)
                {
                    _writer.NewLine();
                }
                else
                {
                    _writer.RequestLineBreakBeforeNextToken();
                }
            }
            else if (token.TokenType == TSqlTokenType.MultilineComment)
            {
                _writer.AddToken(new TSqlParserToken(TSqlTokenType.MultilineComment, token.Text));

                if (isLeading)
                {
                    _writer.NewLine();
                }
            }
        }

        /// <summary>
        /// Emits any remaining comments at the end of the token stream.
        /// Call this after visiting the root fragment to capture comments that appear
        /// after the last statement (end-of-script comments).
        /// </summary>
        protected void EmitRemainingComments()
        {
            if (!_options.PreserveComments || _currentTokenStream == null)
            {
                return;
            }

            // Scan from the last processed token to the end of the token stream
            for (int i = _lastProcessedTokenIndex + 1; i < _currentTokenStream.Count; i++)
            {
                var token = _currentTokenStream[i];
                if (IsCommentToken(token) && !_emittedCommentIndexes.Contains(i))
                {
                    EmitCommentToken(token, isLeading: true);
                    _emittedCommentIndexes.Add(i);
                    _lastProcessedTokenIndex = i;
                }
            }
        }

        /// <summary>
        /// Checks if a token is a comment token.
        /// </summary>
        private static bool IsCommentToken(TSqlParserToken token)
        {
            return token != null &&
                   (token.TokenType == TSqlTokenType.SingleLineComment ||
                    token.TokenType == TSqlTokenType.MultilineComment);
        }

        #endregion
    }
}
