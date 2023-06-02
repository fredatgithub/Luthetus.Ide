﻿using Luthetus.Ide.ClassLib.CompilerServices.Common.BinderCase.BoundNodes.Statements;
using Luthetus.Ide.ClassLib.CompilerServices.Common.BinderCase.BoundNodes;
using Luthetus.Ide.ClassLib.CompilerServices.Common.Syntax;
using Luthetus.Ide.ClassLib.CompilerServices.Languages.CSharp.LexerCase;
using Luthetus.Ide.ClassLib.CompilerServices.Languages.CSharp.ParserCase;
using Luthetus.TextEditor.RazorLib.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Luthetus.Ide.ClassLib.CompilerServices.Common.BinderCase.BoundNodes.Expression;

namespace Luthetus.Ide.Tests.Basics.CompilerServices.Languages.CSharp.ParserCase;

public partial class ParserTests
{
    [Fact]
    public void SHOULD_PARSE_VARIABLE_DECLARATION_STATEMENT()
    {
        string sourceText = @"int x;"
            .ReplaceLineEndings("\n");

        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new Lexer(
            resourceUri,
            sourceText);

        lexer.Lex();

        var parser = new Parser(
            lexer.SyntaxTokens,
            lexer.Diagnostics);

        var compilationUnit = parser.Parse();

        Assert.Single(compilationUnit.Children);

        var boundVariableDeclarationStatementNode =
            (BoundVariableDeclarationStatementNode)compilationUnit.Children
                .Single();

        Assert.Equal(
            SyntaxKind.BoundVariableDeclarationStatementNode,
            boundVariableDeclarationStatementNode.SyntaxKind);

        Assert.Equal(
            2,
            boundVariableDeclarationStatementNode.Children.Length);

        var boundTypeNode = (BoundTypeNode)boundVariableDeclarationStatementNode
            .Children[0];

        Assert.Equal(
            SyntaxKind.BoundTypeNode,
            boundTypeNode.SyntaxKind);

        Assert.Equal(
            typeof(int),
            boundTypeNode.Type);

        var identifierToken = boundVariableDeclarationStatementNode.Children[1];

        Assert.Equal(
            SyntaxKind.IdentifierToken,
            identifierToken.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_VARIABLE_DECLARATION_STATEMENT_THEN_VARIABLE_ASSIGNMENT_STATEMENT()
    {
        string sourceText = @"int x;
x = 42;"
            .ReplaceLineEndings("\n");

        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new Lexer(
            resourceUri,
            sourceText);

        lexer.Lex();

        var parser = new Parser(
            lexer.SyntaxTokens,
            lexer.Diagnostics);

        var compilationUnit = parser.Parse();

        Assert.Equal(2, compilationUnit.Children.Length);

        var boundVariableDeclarationStatementNode =
            (BoundVariableDeclarationStatementNode)compilationUnit.Children[0];

        Assert.Equal(
            SyntaxKind.BoundVariableDeclarationStatementNode,
            boundVariableDeclarationStatementNode.SyntaxKind);

        var boundVariableAssignmentStatementNode =
            (BoundVariableAssignmentStatementNode)compilationUnit.Children[1];

        Assert.Equal(
            SyntaxKind.BoundVariableAssignmentStatementNode,
            boundVariableAssignmentStatementNode.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_COMPOUND_VARIABLE_DECLARATION_AND_ASSIGNMENT_STATEMENT()
    {
        string sourceText = @"int x = 42;"
            .ReplaceLineEndings("\n");

        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new Lexer(
            resourceUri,
            sourceText);

        lexer.Lex();

        var parser = new Parser(
            lexer.SyntaxTokens,
            lexer.Diagnostics);

        var compilationUnit = parser.Parse();

        Assert.Equal(2, compilationUnit.Children.Length);

        var boundVariableDeclarationStatementNode =
            (BoundVariableDeclarationStatementNode)compilationUnit.Children[0];

        Assert.Equal(
            SyntaxKind.BoundVariableDeclarationStatementNode,
            boundVariableDeclarationStatementNode.SyntaxKind);

        var boundVariableAssignmentStatementNode =
            (BoundVariableAssignmentStatementNode)compilationUnit.Children[1];

        Assert.Equal(
            SyntaxKind.BoundVariableAssignmentStatementNode,
            boundVariableAssignmentStatementNode.SyntaxKind);
    }

    [Fact]
    public void SHOULD_PARSE_CONDITIONAL_VAR_KEYWORD()
    {
        var sourceText = @"var var = 2;

var x = var * 2;"
            .ReplaceLineEndings("\n");

        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new Lexer(
            resourceUri,
            sourceText);

        lexer.Lex();

        var modelParser = new Parser(
            lexer.SyntaxTokens,
            lexer.Diagnostics);

        var compilationUnit = modelParser.Parse();

        // BoundVariableDeclarationStatementNode
        {
            var boundVariableDeclarationStatementNode =
                (BoundVariableDeclarationStatementNode)
                compilationUnit.Children[0];

            Assert.NotNull(boundVariableDeclarationStatementNode);
        }

        // BoundVariableAssignmentStatementNode
        {
            var boundVariableAssignmentStatementNode =
                (BoundVariableAssignmentStatementNode)
                compilationUnit.Children[1];

            Assert.NotNull(boundVariableAssignmentStatementNode);
        }

        // BoundVariableDeclarationStatementNode
        {
            var boundVariableDeclarationStatementNode =
                (BoundVariableDeclarationStatementNode)
                compilationUnit.Children[2];

            Assert.NotNull(boundVariableDeclarationStatementNode);
        }

        // BoundVariableAssignmentStatementNode
        {
            var boundVariableAssignmentStatementNode =
                (BoundVariableAssignmentStatementNode)
                compilationUnit.Children[3];

            Assert.NotNull(boundVariableAssignmentStatementNode);
        }
    }

    [Fact]
    public void SHOULD_PARSE_VARIABLE_REFERENCE()
    {
        var sourceText = @"private int _count;

private void IncrementCountOnClick()
{
	_count++;
}"
            .ReplaceLineEndings("\n");

        var resourceUri = new ResourceUri(string.Empty);

        var lexer = new Lexer(
            resourceUri,
            sourceText);

        lexer.Lex();

        var modelParser = new Parser(
            lexer.SyntaxTokens,
            lexer.Diagnostics);

        var compilationUnit = modelParser.Parse();

        // BoundVariableDeclarationStatementNode
        {
            var boundVariableDeclarationStatementNode =
            (BoundVariableDeclarationStatementNode)
            compilationUnit.Children[0];

            Assert.NotNull(boundVariableDeclarationStatementNode);
        }

        // BoundFunctionDeclarationNode
        {
            var boundFunctionDeclarationNode =
            (BoundFunctionDeclarationNode)
            compilationUnit.Children[1];

            Assert.NotNull(boundFunctionDeclarationNode);
        }

        // BoundIdentifierReferenceNode
        {
            var boundIdentifierReferenceNode =
            (BoundIdentifierReferenceNode)
            compilationUnit.Children[2];

            Assert.NotNull(boundIdentifierReferenceNode);
        }
    }
}
