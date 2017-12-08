﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments
{
    public abstract class AbstractDocumentationCommentTests
    {
        protected abstract char DocumentationCommentCharacter { get; }

        internal abstract VisualStudio.Commanding.ICommandHandler CreateCommandHandler(IWaitIndicator waitIndicator, ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService);

        protected abstract TestWorkspace CreateTestWorkspace(string code);

        protected void VerifyTypingCharacter(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService);

                    var commandArgs = new TypeCharCommandArgs(view, view.TextBuffer, DocumentationCommentCharacter);
                    var nextHandler = CreateInsertTextHandler(view, DocumentationCommentCharacter.ToString());

                    CommandHandlerUtils.ExecuteCommand(commandHandler, commandArgs, nextHandler, new TestCommandExecutionContext());
                });
        }

        protected void VerifyPressingEnter(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true,
            Action<TestWorkspace> setOptionsOpt = null)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                setOptionsOpt: setOptionsOpt,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService);

                    var commandArgs = new ReturnKeyCommandArgs(view, view.TextBuffer);
                    var nextHandler = CreateInsertTextHandler(view, "\r\n");
                    CommandHandlerUtils.ExecuteCommand(commandHandler, commandArgs, nextHandler, new TestCommandExecutionContext());
                });
        }

        protected void VerifyInsertCommentCommand(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService);

                    var commandArgs = new InsertCommentCommandArgs(view, view.TextBuffer);
                    Action nextHandler = delegate { };

                    CommandHandlerUtils.ExecuteCommand(commandHandler, commandArgs, nextHandler, new TestCommandExecutionContext());
                });
        }

        protected void VerifyOpenLineAbove(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService);

                    var commandArgs = new OpenLineAboveCommandArgs(view, view.TextBuffer);
                    void nextHandler()
                    {
                        var editorOperations = editorOperationsFactoryService.GetEditorOperations(view);
                        editorOperations.OpenLineAbove();
                    }

                    CommandHandlerUtils.ExecuteCommand(commandHandler, commandArgs, nextHandler, new TestCommandExecutionContext());
                });
        }

        protected void VerifyOpenLineBelow(string initialMarkup, string expectedMarkup, bool useTabs = false, bool autoGenerateXmlDocComments = true)
        {
            Verify(initialMarkup, expectedMarkup, useTabs, autoGenerateXmlDocComments,
                execute: (view, undoHistoryRegistry, editorOperationsFactoryService, completionService) =>
                {
                    var commandHandler = CreateCommandHandler(TestWaitIndicator.Default, undoHistoryRegistry, editorOperationsFactoryService);

                    var commandArgs = new OpenLineBelowCommandArgs(view, view.TextBuffer);
                    void nextHandler()
                    {
                        var editorOperations = editorOperationsFactoryService.GetEditorOperations(view);
                        editorOperations.OpenLineBelow();
                    }

                    CommandHandlerUtils.ExecuteCommand(commandHandler, commandArgs, nextHandler, new TestCommandExecutionContext());
                });
        }

        private Action CreateInsertTextHandler(ITextView textView, string text)
        {
            return () =>
            {
                var caretPosition = textView.Caret.Position.BufferPosition;
                var newSpanshot = textView.TextBuffer.Insert(caretPosition, text);
                textView.Caret.MoveTo(new SnapshotPoint(newSpanshot, caretPosition + text.Length));
            };
        }

        private void Verify(string initialMarkup, string expectedMarkup, bool useTabs, bool autoGenerateXmlDocComments,
            Action<IWpfTextView, ITextUndoHistoryRegistry, IEditorOperationsFactoryService, IAsyncCompletionService> execute,
            Action<TestWorkspace> setOptionsOpt = null)
        {
            using (var workspace = CreateTestWorkspace(initialMarkup))
            {
                var testDocument = workspace.Documents.Single();

                Assert.True(testDocument.CursorPosition.HasValue, "No caret position set!");
                var startCaretPosition = testDocument.CursorPosition.Value;

                var view = testDocument.GetTextView();

                if (testDocument.SelectedSpans.Any())
                {
                    var selectedSpan = testDocument.SelectedSpans[0];

                    var isReversed = selectedSpan.Start == startCaretPosition
                        ? true
                        : false;

                    view.Selection.Select(new SnapshotSpan(view.TextSnapshot, selectedSpan.Start, selectedSpan.Length), isReversed);
                }

                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

                var options = workspace.Options;

                options = options.WithChangedOption(FormattingOptions.UseTabs, testDocument.Project.Language, useTabs);
                options = options.WithChangedOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration, testDocument.Project.Language, autoGenerateXmlDocComments);

                workspace.Options = options;

                setOptionsOpt?.Invoke(workspace);

                execute(
                    view,
                    workspace.GetService<ITextUndoHistoryRegistry>(),
                    workspace.GetService<IEditorOperationsFactoryService>(),
                    workspace.GetService<IAsyncCompletionService>());
                MarkupTestFile.GetPosition(expectedMarkup, out var expectedCode, out int expectedPosition);

                Assert.Equal(expectedCode, view.TextSnapshot.GetText());

                var endCaretPosition = view.Caret.Position.BufferPosition.Position;
                Assert.True(expectedPosition == endCaretPosition,
                    string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, endCaretPosition));
            }
        }
    }
}
