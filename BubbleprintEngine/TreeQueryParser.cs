using System.Text;
using System.Text.Json;

namespace BubbleprintEngine;

public enum TokenType
{
    Identifier,    // Components, $type, ConcentrationLogic
    Equals,        // =
    FuzzyEquals,   // ~=
    Child,         // >
    ArrayItem,     // [
    Dot,           // .
    And,           // &
    LeftParen,     // (
    RightParen,    // )
    EOF,            // End of File
    Unknown,
    DoubleDot
}

public record Token(TokenType Type, string Value);

public static class Lexer
{
    public static List<Token> Tokenize(string query)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < query.Length)
        {
            char c = query[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            Token TryComposite(char a, char b, TokenType ifSingle, TokenType ifComposite)
            {
                if (i + 1 < query.Length && query[i + 1] == b)
                {
                    i += 2;
                    return new(ifComposite, $"{a}{b}");
                }
                else
                {
                    i++;
                    return new(ifSingle, a.ToString());
                }
            }

            switch (c)
            {
                case '(': tokens.Add(new Token(TokenType.LeftParen, c.ToString())); i++; break;
                case ')': tokens.Add(new Token(TokenType.RightParen, c.ToString())); i++; break;
                case '[': tokens.Add(new Token(TokenType.ArrayItem, c.ToString())); i++; break;
                case '&': tokens.Add(new Token(TokenType.And, c.ToString())); i++; break;
                case '=': tokens.Add(new Token(TokenType.Equals, c.ToString())); i++; break;
                case '.': tokens.Add(TryComposite(c, '.', TokenType.Dot, TokenType.DoubleDot)); break;
                case '~': tokens.Add(TryComposite(c, '=', TokenType.Unknown, TokenType.FuzzyEquals)); break;
                default:
                    int start = i;
                    while (i < query.Length && !IsDelimiter(query[i]))
                    {
                        i++;
                    }
                    tokens.Add(new Token(TokenType.Identifier, query.Substring(start, i - start)));
                    break;
            }
        }

        tokens.Add(new Token(TokenType.EOF, ""));
        return tokens;
    }

    private static bool IsDelimiter(char c)
    {
        return char.IsWhiteSpace(c) || "()[]<>.&=~".Contains(c);
    }
}

public class Parser()
{
    private readonly Func<string, string, bool, Func<JsonElement, bool>> makeCheckKV = TreeQueryGenerator.CheckKV;

    private int position = 0;
    private List<Token> tokens = [];
    public void Reset(List<Token> tokens)
    {
        position = 0;
        this.tokens = tokens;
    }


    public Matcher Parse()
    {
        var matcher = ParseExpression();
        if (Current.Type != TokenType.EOF)
        {
            throw new Exception($"Unexpected token '{Current.Value}' at end of query.");
        }
        return matcher;
    }

    // Expression -> Term { '&' Term }
    private Matcher ParseExpression()
    {
        var left = ParseTerm();

        while (Current.Type == TokenType.And)
        {
            Advance(); // Consume '&'
            var right = ParseTerm();

            if (left is AndMatcher and)
            {
                and.Matchers.Add(right);
            }
            else
            {
                left = new AndMatcher([left, right]);
            }
        }
        return left;
    }

    // Term -> '(' Expression ')' | IDENTIFIER [ ('>'|'['|'.') Term | ('='|'~=') IDENTIFIER ]
    private Matcher ParseTerm()
    {
        if (Current.Type == TokenType.LeftParen)
        {
            Advance(); // Consume '('
            var expression = ParseExpression();
            if (Current.Type != TokenType.RightParen)
            {
                throw new Exception("Expected ')'");
            }
            Advance(); // Consume ')'
            return expression;
        }

        var key = Consume(TokenType.Identifier);

        // Lookahead to decide if it's a Path or a Predicate
        switch (Current.Type)
        {
            case TokenType.Equals or TokenType.FuzzyEquals:
                // It's a predicate: key=val
                var op = Current;
                Advance();
                var val = Consume(TokenType.Identifier);

                var desc = $"{key.Value} {op.Value} {val.Value}";

                return new PredicateMatcher(makeCheckKV(key.Value, val.Value, op.Type == TokenType.FuzzyEquals), desc);

            case TokenType.Child or TokenType.ArrayItem or TokenType.Dot:
                // It's a path: key > Term, key [ Term, key . Term
                var combinatorToken = Current;
                Advance();
                var nextRequirement = ParseTerm();

                var combinator = combinatorToken.Type switch
                {
                    TokenType.ArrayItem => NfaCombinator.ArrayItem,
                    _ => NfaCombinator.Child // Treat '>' and '.' as the same
                };

                return new PathMatcher(key.Value, combinator, nextRequirement);

            default:
                // It's a key existence check (e.g., just 'Components' followed by & or EOF)
                return new PredicateMatcher(e => e.TryGetProperty(key.Value, out _));
        }
    }

    private Token Current => tokens[position];
    private void Advance() { if (position < tokens.Count - 1) position++; }
    private Token Consume(TokenType type)
    {
        if (Current.Type == type)
        {
            var token = Current;
            Advance();
            return token;
        }
        throw new Exception($"Expected token {type} but got {Current.Type}");
    }
}


public static class TreeQueryPrinter
{
    public static string DumpAst(Matcher matcher, string indent = " ")
    {
        var sb = new StringBuilder();
        DumpAstRecursive(matcher, indent, sb);
        return sb.ToString();
    }

    public static void DumpAstRecursive(Matcher matcher, string indent, StringBuilder sb)
    {
        // Use a switch expression to handle the different types
        switch (matcher)
        {
            case AndMatcher and:
                sb.AppendLine($"{indent}AND");
                foreach (var m in and.Matchers)
                {
                    DumpAstRecursive(m, indent + "  ", sb);
                }
                break;

            case PathMatcher path:
                sb.AppendLine($"{indent}PATH ({path.KeyRequirement} {path.Combinator})");
                DumpAstRecursive(path.NextRequirement, indent + "  ", sb);
                break;

            case PredicateMatcher pred:
                sb.AppendLine($"{indent}PREDICATE ({pred.Desc})");
                break;
        }
    }

}
