using Luthetus.CompilerServices.Lang.DotNetSolution.Facts;
using Luthetus.CompilerServices.Lang.DotNetSolution.SyntaxActors;
using Luthetus.CompilerServices.Lang.DotNetSolution.Tests.TestData;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.SyntaxTokens;

namespace Luthetus.CompilerServices.Lang.DotNetSolution.Tests.Tests.Parser;

public class GlobalSectionExtensibilityGlobalsDoesParse
{
    [Fact]
    public void FULL_DOES_PARSE()
    {
        var lexer = new DotNetSolutionLexer(
            new(string.Empty),
            TestDataGlobalSectionExtensibilityGlobals.FULL);

        lexer.LexGlobalSection(() => false);

        Assert.Equal(6, lexer.SyntaxTokens.Length);

        var startToken = (KeywordToken)lexer.SyntaxTokens[0];
        Assert.Equal(LexSolutionFacts.GlobalSection.START_TOKEN, startToken.TextSpan.GetText());

        var startParameterToken = (KeywordToken)lexer.SyntaxTokens[1];
        Assert.Equal(LexSolutionFacts.GlobalSectionExtensibilityGlobals.START_TOKEN, startParameterToken.TextSpan.GetText());

        var startOrderToken = (KeywordToken)lexer.SyntaxTokens[2];
        Assert.Equal(TestDataGlobalSectionExtensibilityGlobals.START_TOKEN_ORDER, startOrderToken.TextSpan.GetText());

        var firstPropertyNameToken = (IdentifierToken)lexer.SyntaxTokens[3];
        Assert.Equal(TestDataGlobalSectionExtensibilityGlobals.FIRST_PROPERTY_NAME, firstPropertyNameToken.TextSpan.GetText());

        var firstPropertyValueToken = (IdentifierToken)lexer.SyntaxTokens[4];
        Assert.Equal(TestDataGlobalSectionExtensibilityGlobals.FIRST_PROPERTY_VALUE, firstPropertyValueToken.TextSpan.GetText());

        var endToken = (KeywordToken)lexer.SyntaxTokens[5];
        Assert.Equal(LexSolutionFacts.GlobalSection.END_TOKEN, endToken.TextSpan.GetText());
    }
}