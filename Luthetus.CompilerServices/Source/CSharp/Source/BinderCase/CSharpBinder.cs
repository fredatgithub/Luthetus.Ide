﻿using System.Collections.Immutable;
using Luthetus.CompilerServices.Lang.CSharp.Facts;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.CompilerServices.GenericLexer.Decoration;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Symbols;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxNodes;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxNodes.Expression;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxTokens;
using Luthetus.TextEditor.RazorLib.Lexes.Models;

namespace Luthetus.CompilerServices.Lang.CSharp.BinderCase;

public class CSharpBinder : IBinder
{
    private readonly BoundScope _globalScope = CSharpLanguageFacts.Scope.GetInitialGlobalScope();
    private readonly Dictionary<string, NamespaceStatementNode> _namespaceStatementNodes = CSharpLanguageFacts.Namespaces.GetInitialBoundNamespaceStatementNodes();
    /// <summary>The key for _symbolDefinitions is calculated by <see cref="ISymbol.GetSymbolDefinitionId"/></summary>
    private readonly Dictionary<string, SymbolDefinition> _symbolDefinitions = new();
    private readonly LuthetusDiagnosticBag _diagnosticBag = new();

    private List<BoundScope> _boundScopes = new();
    private BoundScope _currentScope;

    public CSharpBinder()
    {
        _currentScope = _globalScope;

        _boundScopes.Add(_globalScope);

        _boundScopes = _boundScopes
            .OrderBy(x => x.StartingIndexInclusive)
            .ToList();
    }

    public ResourceUri? CurrentResourceUri { get; set; }

    public ImmutableDictionary<string, NamespaceStatementNode> BoundNamespaceStatementNodes => _namespaceStatementNodes.ToImmutableDictionary();
    public ImmutableArray<ISymbol> Symbols => _symbolDefinitions.Values.SelectMany(x => x.SymbolReferences).Select(x => x.Symbol).ToImmutableArray();
    public Dictionary<string, SymbolDefinition> SymbolDefinitions => _symbolDefinitions;
    public ImmutableArray<BoundScope> BoundScopes => _boundScopes.ToImmutableArray();
    public ImmutableArray<TextEditorDiagnostic> DiagnosticsBag => _diagnosticBag.ToImmutableArray();

    ImmutableArray<ITextEditorSymbol> IBinder.SymbolsBag => Symbols
        .Select(s => (ITextEditorSymbol)s)
        .ToImmutableArray();

    public LiteralExpressionNode BindLiteralExpressionNode(LiteralExpressionNode literalExpressionNode)
    {
        var typeClauseNode = literalExpressionNode.LiteralSyntaxToken.SyntaxKind switch
        {
            SyntaxKind.NumericLiteralToken => CSharpLanguageFacts.Types.Int.ToTypeClause(),
            SyntaxKind.StringLiteralToken => CSharpLanguageFacts.Types.String.ToTypeClause(),
            _ => throw new NotImplementedException(),
        };

        return new LiteralExpressionNode(
            literalExpressionNode.LiteralSyntaxToken,
            typeClauseNode);
    }

    public BinaryOperatorNode BindBinaryOperatorNode(
        IExpressionNode leftExpressionNode,
        ISyntaxToken operatorToken,
        IExpressionNode rightExpressionNode)
    {
        if (leftExpressionNode.TypeClauseNode is null ||
            rightExpressionNode.TypeClauseNode is null)
        {
            throw new ApplicationException($"TODO: How should one handle an expression with a null {nameof(IExpressionNode.TypeClauseNode)}?");
        }

        if (leftExpressionNode.TypeClauseNode.ValueType == typeof(int) &&
            rightExpressionNode.TypeClauseNode.ValueType == typeof(int))
        {
            switch (operatorToken.SyntaxKind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.StarToken:
                case SyntaxKind.DivisionToken:
                    return new BinaryOperatorNode(
                        leftExpressionNode.TypeClauseNode,
                        operatorToken,
                        rightExpressionNode.TypeClauseNode,
                        CSharpLanguageFacts.Types.Int.ToTypeClause());
            }
        }
        else if (leftExpressionNode.TypeClauseNode.ValueType == typeof(string) &&
            rightExpressionNode.TypeClauseNode.ValueType == typeof(string))
        {
            switch (operatorToken.SyntaxKind)
            {
                case SyntaxKind.PlusToken:
                    return new BinaryOperatorNode(
                        leftExpressionNode.TypeClauseNode,
                        operatorToken,
                        rightExpressionNode.TypeClauseNode,
                        CSharpLanguageFacts.Types.String.ToTypeClause());
            }
        }

        throw new NotImplementedException();
    }

    /// <summary>TODO: Construct a BoundStringInterpolationExpressionNode and identify the expressions within the string literal. For now I am just making the dollar sign the same color as a string literal.</summary>
    public void BindStringInterpolationExpression(
        DollarSignToken dollarSignToken)
    {
        AddSymbolReference(new StringInterpolationSymbol(dollarSignToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.StringLiteral,
        }));
    }

    public void BindFunctionDefinitionNode(FunctionDefinitionNode functionDefinitionNode)
    {
        var functionIdentifierText = functionDefinitionNode.FunctionIdentifier.TextSpan.GetText();

        var functionSymbol = new FunctionSymbol(functionDefinitionNode.FunctionIdentifier.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Function
        });

        AddSymbolDefinition(functionSymbol);

        if (!_currentScope.FunctionDefinitionMap.TryAdd(
                functionIdentifierText,
                functionDefinitionNode))
        {
            _diagnosticBag.ReportAlreadyDefinedFunction(
                functionDefinitionNode.FunctionIdentifier.TextSpan,
                functionIdentifierText);
        }
    }
    
    public FunctionArgumentEntryNode BindFunctionOptionalArgument(
        FunctionArgumentEntryNode functionArgumentEntryNode,
        ISyntaxToken compileTimeConstantToken,
        bool hasOutKeyword,
        bool hasInKeyword,
        bool hasRefKeyword)
    {
        var literalExpressionNode = new LiteralExpressionNode(
            compileTimeConstantToken,
            null);

        literalExpressionNode = BindLiteralExpressionNode(literalExpressionNode);
        
        if (literalExpressionNode.TypeClauseNode?.ValueType is null ||
            literalExpressionNode.TypeClauseNode.ValueType != functionArgumentEntryNode.VariableDeclarationStatementNode.TypeClauseNode.ValueType)
        {
            var optionalArgumentTextSpan = functionArgumentEntryNode.VariableDeclarationStatementNode.TypeClauseNode.TypeIdentifier.TextSpan with
            {
                EndingIndexExclusive = functionArgumentEntryNode.VariableDeclarationStatementNode.IdentifierToken.TextSpan.EndingIndexExclusive
            };

            _diagnosticBag.ReportBadFunctionOptionalArgumentDueToMismatchInType(
                optionalArgumentTextSpan, 
                functionArgumentEntryNode.VariableDeclarationStatementNode.IdentifierToken.TextSpan.GetText(),
                functionArgumentEntryNode.VariableDeclarationStatementNode.TypeClauseNode.ValueType?.Name ?? "null",
                literalExpressionNode.TypeClauseNode?.ValueType?.Name ?? "null");
        }

        return new FunctionArgumentEntryNode(
            functionArgumentEntryNode.VariableDeclarationStatementNode,
            true,
            hasOutKeyword,
            hasInKeyword,
            hasRefKeyword);
    }

    /// <summary>TODO: Validate that the returned bound expression node has the same result type as the enclosing scope.</summary>
    public ReturnStatementNode BindReturnStatementNode(
        KeywordToken keywordToken,
        IExpressionNode expressionNode)
    {
        _diagnosticBag.ReportReturnStatementsAreStillBeingImplemented(
                keywordToken.TextSpan);

        return new ReturnStatementNode(
            keywordToken,
            expressionNode);
    }

    public IfStatementNode BindIfStatementNode(
        KeywordToken ifKeywordToken,
        IExpressionNode expressionNode)
    {
        var boundIfStatementNode = new IfStatementNode(
            ifKeywordToken,
            expressionNode,
            null);

        return boundIfStatementNode;
    }

    public NamespaceStatementNode BindNamespaceStatementNode(
        KeywordToken keywordToken,
        IdentifierToken identifierToken)
    {
        AddSymbolReference(new NamespaceSymbol(identifierToken.TextSpan));

        var namespaceIdentifier = identifierToken.TextSpan.GetText();

        if (_namespaceStatementNodes.TryGetValue(
                namespaceIdentifier,
                out var boundNamespaceStatementNode))
        {
            return boundNamespaceStatementNode;
        }
        else
        {
            boundNamespaceStatementNode = new NamespaceStatementNode(
                keywordToken,
                identifierToken,
                ImmutableArray<NamespaceEntryNode>.Empty);

            var success = _namespaceStatementNodes.TryAdd(
                namespaceIdentifier,
                boundNamespaceStatementNode);

            if (!success)
                _namespaceStatementNodes[namespaceIdentifier] = boundNamespaceStatementNode;

            return boundNamespaceStatementNode;
        }
    }
    
    public NamespaceStatementNode RegisterBoundNamespaceEntryNode(
        NamespaceStatementNode inBoundNamespaceStatementNode,
        CodeBlockNode codeBlockNode)
    {
        var namespaceIdentifier = inBoundNamespaceStatementNode
            .IdentifierToken.TextSpan.GetText();

        if (_namespaceStatementNodes.TryGetValue(
                namespaceIdentifier,
                out var existingBoundNamespaceStatementNode))
        {
            var boundNamespaceEntryNode = new NamespaceEntryNode(
                inBoundNamespaceStatementNode.IdentifierToken.TextSpan.ResourceUri,
                codeBlockNode);

            var outChildren = existingBoundNamespaceStatementNode.NamespaceEntryNodeBag
                .Add(boundNamespaceEntryNode)
                .ToImmutableArray();

            var outBoundNamespaceStatementNode = new NamespaceStatementNode(
                existingBoundNamespaceStatementNode.KeywordToken,
                existingBoundNamespaceStatementNode.IdentifierToken,
                outChildren);

            _namespaceStatementNodes[namespaceIdentifier] = outBoundNamespaceStatementNode;

            return outBoundNamespaceStatementNode;
        }
        else
        {
            throw new NotImplementedException(
                $"The {nameof(inBoundNamespaceStatementNode)}" +
                $" was not found in the {nameof(_namespaceStatementNodes)} dictionary.");
        }
    }

    public void BindConstructorInvocationNode()
    {
        // Deleted what was in this method because it was nonsense, and causing errors. (2023-08-06)
    }

    public InheritanceStatementNode BindInheritanceStatementNode(TypeClauseNode typeClauseNode)
    {
        AddSymbolReference(new TypeSymbol(typeClauseNode.TypeIdentifier.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Type
        }));

        throw new NotImplementedException();
    }

    public void BindVariableDeclarationStatementNode(
        VariableDeclarationStatementNode variableDeclarationStatementNode)
    {
        var variableSymbol = new VariableSymbol(variableDeclarationStatementNode.IdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Variable
        });

        AddSymbolDefinition(variableSymbol);

        var text = variableDeclarationStatementNode.IdentifierToken.TextSpan.GetText();

        if (!_currentScope.VariableDeclarationMap.TryAdd(
                text,
                variableDeclarationStatementNode))
        {
            _diagnosticBag.ReportAlreadyDefinedVariable(
                variableDeclarationStatementNode.IdentifierToken.TextSpan,
                text);
        }
    }
    
    public VariableReferenceNode BindVariableReferenceNode(VariableReferenceNode variableReferenceNode)
    {
        var variableSymbol = new VariableSymbol(variableReferenceNode.VariableIdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Variable
        });

        AddSymbolDefinition(variableSymbol);

        var text = variableReferenceNode.VariableIdentifierToken.TextSpan.GetText();

        if (TryGetVariableHierarchically(text, out var variableDeclarationStatementNode))
        {
            variableReferenceNode = new VariableReferenceNode(
                variableReferenceNode.VariableIdentifierToken,
                variableDeclarationStatementNode!);
        }
        else
        {
            _diagnosticBag.ReportUndefinedVariable(
                variableReferenceNode.VariableIdentifierToken.TextSpan,
                text);
        }

        return variableReferenceNode;
    }

    public void BindVariableAssignmentExpressionNode(
        VariableAssignmentExpressionNode variableAssignmentExpressionNode)
    {
        var variableSymbol = new VariableSymbol(variableAssignmentExpressionNode.VariableIdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Variable
        });

        AddSymbolReference(variableSymbol);

        var text = variableAssignmentExpressionNode.VariableIdentifierToken.TextSpan.GetText();

        if (TryGetVariableHierarchically(
                text,
                out var variableDeclarationNode) &&
            variableDeclarationNode is not null)
        {
            variableDeclarationNode = new VariableDeclarationStatementNode(
                variableDeclarationNode.TypeClauseNode,
                variableDeclarationNode.IdentifierToken,
                true);

            _currentScope.VariableDeclarationMap[text] =
                variableDeclarationNode;
        }
        else
        {
            _diagnosticBag.ReportUndefinedVariable(
                variableAssignmentExpressionNode.VariableIdentifierToken.TextSpan,
                text);
        }
    }

    /// <summary>
    /// TODO: This should be 'BindPropertyDeclarationNode' and take the respective datatype. For now (2023-08-10) just giving an IdentifierToken is easier.
    /// </summary>
    public void BindPropertyDeclarationIdentifierToken(
        IdentifierToken identifierToken)
    {
        var propertySymbol = new PropertySymbol(identifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Property
        });

        AddSymbolDefinition(propertySymbol);
    }
    
    /// <summary>
    /// TODO: This should be 'BindPropertyDeclarationNode' and take the respective datatype. For now (2023-08-10) just giving an IdentifierToken is easier.
    /// </summary>
    public void BindConstructorDefinitionIdentifierToken(
        IdentifierToken identifierToken)
    {
        var constructorSymbol = new ConstructorSymbol(identifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Type
        });

        AddSymbolDefinition(constructorSymbol);
    }

    public void BindPropertyDeclarationNode(
        VariableDeclarationStatementNode variableDeclarationStatementNode)
    {
        var propertySymbol = new PropertySymbol(variableDeclarationStatementNode.IdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Property
        });

        AddSymbolDefinition(propertySymbol);

        var text = propertySymbol.TextSpan.GetText();

        if (_currentScope.VariableDeclarationMap.TryGetValue(
                text,
                out var existingVariableDeclarationStatementNode) ||
            existingVariableDeclarationStatementNode is null)
        {
            // TODO: The property was already declared, so report a diagnostic?
            // TODO: The property was already declared, so check that the return types match?
            return;
        }

        var success = _currentScope.VariableDeclarationMap.TryAdd(
            text,
            existingVariableDeclarationStatementNode);

        if (!success)
            _currentScope.VariableDeclarationMap[text] = existingVariableDeclarationStatementNode;
    }

    /// <summary>
    /// TODO: Fix BindIdentifierReferenceNode, it broke on (2023-07-26)
    /// </summary>
    // public BoundIdentifierReferenceNode BindIdentifierReferenceNode(
    //     IdentifierToken identifierToken)
    // {
    //     var text = identifierToken.TextSpan.GetText();
    //
    //     if (TryGetVariableHierarchically(
    //             text,
    //             out var variableDeclarationNode) &&
    //         variableDeclarationNode is not null)
    //     {
    //         AddSymbolReference(new VariableSymbol(identifierToken.TextSpan with
    //         {
    //             DecorationByte = (byte)GenericDecorationKind.Variable
    //         }));
    //
    //         return new BoundIdentifierReferenceNode(
    //             identifierToken,
    //             variableDeclarationNode.BoundClassReferenceNode);
    //     }
    //     else if (TryGetClassReferenceHierarchically(
    //                  identifierToken,
    //                  null,
    //                  out var boundClassReferenceNode) &&
    //              boundClassReferenceNode is not null)
    //     {
    //         AddSymbolReference(new TypeSymbol(identifierToken.TextSpan with
    //         {
    //             DecorationByte = (byte)GenericDecorationKind.Type
    //         }));
    //
    //         return new BoundIdentifierReferenceNode(
    //             identifierToken,
    //             boundClassReferenceNode);
    //     }
    //     else if (TryGetBoundFunctionDefinitionNodeHierarchically(
    //                  text,
    //                  out var boundFunctionDefinitionNode) &&
    //              boundFunctionDefinitionNode is not null)
    //     {
    //         // TODO: Would this conditional branch be for method groups? @onclick="MethodName"
    //
    //         AddSymbolReference(new FunctionSymbol(identifierToken.TextSpan with
    //         {
    //             DecorationByte = (byte)GenericDecorationKind.Function
    //         }));
    //
    //         return new BoundIdentifierReferenceNode(
    //             identifierToken,
    //             // TODO: Null is should not be passed in here
    //             null);
    //     }
    //     else
    //     {
    //         // TODO: The identifier was not found, so report a diagnostic?
    //         return new BoundIdentifierReferenceNode(
    //             identifierToken,
    //             // TODO: Null is should not be passed in here
    //             null);
    //     }
    // }

    public void BindFunctionInvocationNode(FunctionInvocationNode functionInvocationNode)
    {
        var functionInvocationIdentifierText = functionInvocationNode
            .FunctionInvocationIdentifierToken.TextSpan.GetText();

        var functionSymbol = new FunctionSymbol(functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Function
        });

        AddSymbolReference(functionSymbol);

        if (_currentScope.FunctionDefinitionMap.TryGetValue(
                functionInvocationIdentifierText,
                out var functionDefinitionNode) &&
            functionDefinitionNode is not null)
        {
            return;
        }
        else
        {
            _diagnosticBag.ReportUndefinedFunction(
                functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan,
                functionInvocationIdentifierText);
        }
    }
    
    public void BindNamespaceReference(
        IdentifierToken namespaceIdentifierToken)
    {
        var namespaceSymbol = new NamespaceSymbol(namespaceIdentifierToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.None
        });

        AddSymbolReference(namespaceSymbol);
    }
    
    public TypeClauseNode BindTypeClauseNode(TypeClauseNode typeClauseNode)
    {
        if (typeClauseNode.TypeIdentifier.SyntaxKind == SyntaxKind.IdentifierToken)
        {
            var typeSymbol = new TypeSymbol(typeClauseNode.TypeIdentifier.TextSpan with
            {
                DecorationByte = (byte)GenericDecorationKind.Type
            });

            AddSymbolReference(typeSymbol);
        }

        var matchingTypeDefintionNode = CSharpLanguageFacts.Types.TypeDefinitionNodes.SingleOrDefault(
            x => x.TypeIdentifier.TextSpan.GetText() == typeClauseNode.TypeIdentifier.TextSpan.GetText());

        if (matchingTypeDefintionNode is not null)
        {
            return new TypeClauseNode(
                typeClauseNode.TypeIdentifier,
                matchingTypeDefintionNode.ValueType,
                typeClauseNode.GenericParametersListingNode);
        }

        return typeClauseNode;
    }
    
    public void BindTypeIdentifier(IdentifierToken identifierToken)
    {
        if (identifierToken.SyntaxKind == SyntaxKind.IdentifierToken)
        {
            var typeSymbol = new TypeSymbol(identifierToken.TextSpan with
            {
                DecorationByte = (byte)GenericDecorationKind.Type
            });

            AddSymbolReference(typeSymbol);
        }
    }

    public UsingStatementNode BindUsingStatementNode(
        KeywordToken usingKeywordToken,
        IdentifierToken namespaceIdentifierToken)
    {
        AddSymbolReference(new NamespaceSymbol(namespaceIdentifierToken.TextSpan));

        var namespaceText = namespaceIdentifierToken.TextSpan.GetText();

        if (_namespaceStatementNodes.TryGetValue(
                namespaceText,
                out var boundNamespaceStatementNode))
        {
            AddNamespaceToCurrentScope(boundNamespaceStatementNode);
        }

        return new UsingStatementNode(
            usingKeywordToken,
            namespaceIdentifierToken);
    }

    /// <summary>TODO: Correctly implement this method. For now going to skip until the attribute closing square bracket.</summary>
    public AttributeNode BindAttributeNode(
        OpenSquareBracketToken openSquareBracketToken,
        CloseSquareBracketToken closeSquareBracketToken)
    {
        AddSymbolReference(new TypeSymbol(openSquareBracketToken.TextSpan with
        {
            DecorationByte = (byte)GenericDecorationKind.Type,
            EndingIndexExclusive = closeSquareBracketToken.TextSpan.EndingIndexExclusive
        }));

        return new AttributeNode(
            openSquareBracketToken,
            closeSquareBracketToken);
    }

    public void RegisterBoundScope(
        TypeClauseNode? scopeReturnTypeClauseNode,
        TextEditorTextSpan textEditorTextSpan)
    {
        var boundScope = new BoundScope(
            _currentScope,
            scopeReturnTypeClauseNode,
            textEditorTextSpan.StartingIndexInclusive,
            null,
            textEditorTextSpan.ResourceUri,
            new(),
            new(),
            new());

        _boundScopes.Add(boundScope);

        _boundScopes = _boundScopes
            .OrderBy(x => x.StartingIndexInclusive)
            .ToList();

        _currentScope = boundScope;
    }

    public void AddNamespaceToCurrentScope(
        NamespaceStatementNode boundNamespaceStatementNode)
    {
        var typeDefinitionNodes = boundNamespaceStatementNode
            .GetTopLevelTypeDefinitionNodes();

        foreach (var typeDefinitionNode in typeDefinitionNodes)
        {
            BindTypeDefinitionNode(typeDefinitionNode);
        }
    }

    public void DisposeBoundScope(
        TextEditorTextSpan textEditorTextSpan)
    {
        _currentScope.EndingIndexExclusive = textEditorTextSpan.EndingIndexExclusive;

        if (_currentScope.Parent is not null)
            _currentScope = _currentScope.Parent;
    }

    /// <summary>Search hierarchically through all the scopes, starting at the <see cref="_currentScope"/>.<br/><br/>If a match is found, then set the out parameter to it and return true.<br/><br/>If none of the searched scopes contained a match then set the out parameter to null and return false.</summary>
    public bool TryGetBoundFunctionDefinitionNodeHierarchically(
        string text,
        out FunctionDefinitionNode? functionDefinitionNode)
    {
        var localScope = _currentScope;

        while (localScope is not null)
        {
            if (localScope.FunctionDefinitionMap.TryGetValue(
                    text,
                    out functionDefinitionNode))
            {
                return true;
            }

            localScope = localScope.Parent;
        }

        functionDefinitionNode = null;
        return false;
    }

    /// <summary>
    /// TODO: Fix TryGetClassDefinitionHierarchically, it broke on (2023-07-27)
    /// 
    /// Search hierarchically through all the scopes, starting at the <see cref="_currentScope"/>.<br/><br/>If a match is found, then set the out parameter to it and return true.<br/><br/>If none of the searched scopes contained a match then set the out parameter to null and return false.</summary>
    // public bool TryGetClassDefinitionHierarchically(
    //     ISyntaxToken typeClauseToken,
    //     BoundGenericArgumentsNode? boundGenericArgumentsNode,
    //     out BoundClassDefinitionNode? boundClassDefinitionNode)
    // {
    //     var localScope = _currentScope;
    //
    //     while (localScope is not null)
    //     {
    //         if (localScope.TypeDefinitionMap.TryGetValue(
    //                 typeClauseToken.TextSpan.GetText(),
    //                 out boundClassDefinitionNode))
    //         {
    //             return true;
    //         }
    //
    //         localScope = localScope.Parent;
    //     }
    //
    //     boundClassDefinitionNode = null;
    //     return false;
    // }

    /// <summary>
    /// TODO: Fix TryGetClassReferenceHierarchically, it broke on (2023-07-26)
    /// 
    /// Search hierarchically through all the scopes, starting at the <see cref="_currentScope"/>.<br/><br/>If a match is found, then set the out parameter to it and return true.<br/><br/>If none of the searched scopes contained a match then set the out parameter to a fabricated instance and return false.
    /// </summary>
    // public bool TryGetClassReferenceHierarchically(
    //     ISyntaxToken typeClauseToken,
    //     BoundGenericArgumentsNode? boundGenericArgumentsNode,
    //     out BoundClassReferenceNode? boundClassReferenceNode,
    //     bool shouldCreateTypeSymbolReference = true,
    //     bool shouldReportUndefinedTypeOrNamespace = true,
    //     bool shouldCreateClassDefinitionIfUndefined = true)
    // {
    //     if (shouldCreateTypeSymbolReference &&
    //         typeClauseToken.SyntaxKind == SyntaxKind.IdentifierToken)
    //     {
    //         AddSymbolReference(new TypeSymbol(typeClauseToken.TextSpan with
    //         {
    //             DecorationByte = (byte)GenericDecorationKind.Type
    //         }));
    //     }
    //
    //     var localScope = _currentScope;
    //
    //     while (localScope is not null)
    //     {
    //         if (localScope.ClassDefinitionMap.TryGetValue(
    //                 typeClauseToken.TextSpan.GetText(),
    //                 out var existingBoundClassDefinitionNode))
    //         {
    //             boundClassReferenceNode = new BoundClassReferenceNode(
    //                 typeClauseToken,
    //                 existingBoundClassDefinitionNode.Type,
    //                 boundGenericArgumentsNode);
    //
    //             return true;
    //         }
    //
    //         localScope = localScope.Parent;
    //     }
    //
    //     if (shouldReportUndefinedTypeOrNamespace)
    //     {
    //         _diagnosticBag.ReportUndefinedTypeOrNamespace(
    //             typeClauseToken.TextSpan,
    //             typeClauseToken.TextSpan.GetText());
    //     }
    //
    //     if (shouldCreateClassDefinitionIfUndefined)
    //     {
    //         _ = TryBindClassDefinitionNode(
    //         typeClauseToken,
    //         boundGenericArgumentsNode,
    //         out var fabricatedBoundClassDefinitionNode);
    //
    //         boundClassReferenceNode = new BoundClassReferenceNode(
    //             typeClauseToken,
    //             fabricatedBoundClassDefinitionNode.Type,
    //             boundGenericArgumentsNode);
    //     }
    //     else
    //     {
    //         boundClassReferenceNode = null;
    //     }
    //
    //     return false;
    // }

    public void BindTypeDefinitionNode(
        TypeDefinitionNode typeDefinitionNode,
        bool shouldOverwrite = false)
    {
        var success = _currentScope.TypeDefinitionMap.TryAdd(
            typeDefinitionNode.TypeIdentifier.TextSpan.GetText(),
            typeDefinitionNode);

        if (!success && shouldOverwrite)
            _currentScope.TypeDefinitionMap[typeDefinitionNode.TypeIdentifier.TextSpan.GetText()] = typeDefinitionNode;
    }

    /// <summary>Search hierarchically through all the scopes, starting at the <see cref="_currentScope"/>.<br/><br/>If a match is found, then set the out parameter to it and return true.<br/><br/>If none of the searched scopes contained a match then set the out parameter to null and return false.</summary>
    public bool TryGetTypeHierarchically(
        string text,
        out TypeDefinitionNode? typeDefinitionNode)
    {
        var localScope = _currentScope;

        while (localScope is not null)
        {
            if (localScope.TypeDefinitionMap.TryGetValue(
                    text,
                    out typeDefinitionNode))
            {
                return true;
            }

            localScope = localScope.Parent;
        }

        typeDefinitionNode = null;
        return false;
    }
    
    /// <summary>Search hierarchically through all the scopes, starting at the <see cref="_currentScope"/>.<br/><br/>If a match is found, then set the out parameter to it and return true.<br/><br/>If none of the searched scopes contained a match then set the out parameter to null and return false.</summary>
    public bool TryGetVariableHierarchically(
        string text,
        out VariableDeclarationStatementNode? variableDeclarationStatementNode)
    {
        var localScope = _currentScope;

        while (localScope is not null)
        {
            if (localScope.VariableDeclarationMap.TryGetValue(
                    text,
                    out variableDeclarationStatementNode))
            {
                return true;
            }

            localScope = localScope.Parent;
        }

        variableDeclarationStatementNode = null;
        return false;
    }

    /// <summary>This method will handle the <see cref="SymbolDefinition"/>, but also invoke <see cref="AddSymbolReference"/> because each definition is being treated as a reference itself.</summary>
    private void AddSymbolDefinition(ISymbol symbol)
    {
        var symbolDefinitionId = ISymbol.GetSymbolDefinitionId(
            symbol.TextSpan.GetText(),
            _currentScope.BoundScopeKey);

        var symbolDefinition = new SymbolDefinition(
            _currentScope.BoundScopeKey,
            symbol);

        if (!_symbolDefinitions.TryAdd(
                symbolDefinitionId,
                symbolDefinition))
        {
            var existingSymbolDefinition = _symbolDefinitions[symbolDefinitionId];

            if (existingSymbolDefinition.IsFabricated)
            {
                _symbolDefinitions[symbolDefinitionId] = existingSymbolDefinition with
                {
                    IsFabricated = false
                };
            }
            // TODO: The else branch of this if statement would mean the Symbol definition was found twice, should a diagnostic be reported here?
        }

        AddSymbolReference(symbol);
    }

    private void AddSymbolReference(ISymbol symbol)
    {
        var symbolDefinitionId = ISymbol.GetSymbolDefinitionId(
            symbol.TextSpan.GetText(),
            _currentScope.BoundScopeKey);

        if (!_symbolDefinitions.TryGetValue(
                symbolDefinitionId,
                out var symbolDefinition))
        {
            symbolDefinition = new SymbolDefinition(
                _currentScope.BoundScopeKey,
                symbol)
            {
                IsFabricated = true
            };

            // TODO: Symbol definition was not found, should a diagnostic be reported here?
            var success = _symbolDefinitions.TryAdd(
                symbolDefinitionId,
                symbolDefinition);

            if (!success)
                _symbolDefinitions[symbolDefinitionId] = symbolDefinition;
        }

        symbolDefinition.SymbolReferences.Add(new SymbolReference(
            symbol,
            _currentScope.BoundScopeKey));
    }

    public void ClearStateByResourceUri(ResourceUri resourceUri)
    {
        foreach (var namespaceStatementKeyValuePair in _namespaceStatementNodes)
        {
            var keep = namespaceStatementKeyValuePair.Value.NamespaceEntryNodeBag
                .Where(x => x.ResourceUri != resourceUri)
                .ToImmutableArray();

            _namespaceStatementNodes[namespaceStatementKeyValuePair.Key] =
                new NamespaceStatementNode(
                    namespaceStatementKeyValuePair.Value.KeywordToken,
                    namespaceStatementKeyValuePair.Value.IdentifierToken,
                    keep);
        }

        foreach (var symbolDefinition in _symbolDefinitions)
        {
            var keep = symbolDefinition.Value.SymbolReferences
                .Where(x => x.Symbol.TextSpan.ResourceUri != resourceUri)
                .ToList();

            _symbolDefinitions[symbolDefinition.Key] =
                symbolDefinition.Value with
                {
                    SymbolReferences = keep
                };
        }

        _boundScopes = _boundScopes
            .Where(x => x.ResourceUri != resourceUri)
            .ToList();

        _diagnosticBag.ClearByResourceUri(resourceUri);
    }
}