using BubbleprintEngine;

namespace BubbleprintEngine.Tests;

public class TreeQueryLexerTests
{
    [Fact]
    public void Tokenize_SupportedArrayQuery_ProducesExpectedTokens()
    {
        var tokens = Lexer.Tokenize("Components[$type~=Damage");

        Assert.Collection(
            tokens,
            token => Assert.Equal(new Token(TokenType.Identifier, "Components"), token),
            token => Assert.Equal(new Token(TokenType.ArrayItem, "["), token),
            token => Assert.Equal(new Token(TokenType.Identifier, "$type"), token),
            token => Assert.Equal(new Token(TokenType.FuzzyEquals, "~="), token),
            token => Assert.Equal(new Token(TokenType.Identifier, "Damage"), token),
            token => Assert.Equal(new Token(TokenType.EOF, ""), token));
    }

    [Fact]
    public async Task Tokenize_GreaterThanPath_Completes()
    {
        var tokenization = Task.Run(() => Lexer.Tokenize("Root > Child"));
        var completed = await Task.WhenAny(tokenization, Task.Delay(TimeSpan.FromMilliseconds(250)));

        Assert.Same(tokenization, completed);
        var tokens = await tokenization;
        Assert.Contains(tokens, token => token.Type == TokenType.Child);
    }
}
