﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.ExtractMethod
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text.Editor.Commanding
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractMethod
    <Export(GetType(VisualStudio.Commanding.ICommandHandler))>
    <HandlesCommand(GetType(ExtractMethodCommandArgs))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.ExtractMethod)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class ExtractMethodCommandHandler
        Inherits AbstractExtractMethodCommandHandler

        <ImportingConstructor()>
        Public Sub New(undoManager As ITextBufferUndoManagerProvider,
                       editorOperationsFactoryService As IEditorOperationsFactoryService,
                       renameService As IInlineRenameService)
            MyBase.New(undoManager, editorOperationsFactoryService, renameService)
        End Sub
    End Class
End Namespace
