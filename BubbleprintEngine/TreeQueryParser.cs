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
                case '>': tokens.Add(new Token(TokenType.Child, c.ToString())); i++; break;
                case '.': tokens.Add(TryComposite(c, '.', TokenType.Dot, TokenType.DoubleDot)); break;
                case '~': tokens.Add(TryComposite(c, '=', TokenType.Unknown, TokenType.FuzzyEquals)); break;
                default:
                    int start = i;
                    while (i < query.Length && !IsDelimiter(query[i]))
                    {
                        i++;
                    }
                    if (i > start)
                    {
                        tokens.Add(new Token(TokenType.Identifier, query.Substring(start, i - start)));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Unknown, c.ToString()));
                        i++;
                    }
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

    public bool Error = false;
    public string ErrorMessage { get; private set; } = "";
    private int position = 0;
    private List<Token> tokens = [];
    public void Reset(List<Token> tokens)
    {
        Error = false;
        ErrorMessage = "";
        position = 0;
        this.tokens = tokens;
    }


    public Matcher Parse()
    {
        if (!TryParseExpression(out var matcher))
        {
            Error = true;
            SetError($"Unexpected token '{Current.Value}'.");
            return NullMatcher.Instance;
        }

        if (Current.Type != TokenType.EOF)
        {
            Error = true;
            SetError($"Unexpected token '{Current.Value}'.");
            return NullMatcher.Instance;
        }

        return matcher;
    }

    // Expression -> Relation { '&' Relation }
    private bool TryParseExpression(out Matcher expr)
    {
        expr = NullMatcher.Instance;

        if (!TryParseRelation(out var left)) return false;

        while (Current.Type == TokenType.And)
        {
            Advance(); // Consume '&'
            if (!TryParseRelation(out var right)) return false;

            if (left is AndMatcher and)
            {
                and.Matchers.Add(right);
            }
            else
            {
                left = new AndMatcher([left, right]);
            }
        }
        expr = left;
        return true;
    }

    // Relation -> Primary [ ('>'|'..') Relation ]
    // Relations are right-associative so A..B..C means A..(B..C).
    private bool TryParseRelation(out Matcher matcher)
    {
        if (!TryParsePrimary(out var anchor))
        {
            matcher = NullMatcher.Instance;
            return false;
        }

        if (Current.Type is not (TokenType.Child or TokenType.DoubleDot))
        {
            matcher = anchor;
            return true;
        }

        var combinator = Current.Type == TokenType.DoubleDot
            ? NfaCombinator.Descendant
            : NfaCombinator.Child;
        Advance();

        if (!TryParseRelation(out var target))
        {
            matcher = NullMatcher.Instance;
            return false;
        }

        matcher = new RelationMatcher(anchor, combinator, target);
        return true;
    }

    // Primary -> '(' Expression ')' | IDENTIFIER [ PathSuffix | Predicate ]
    private bool TryParsePrimary(out Matcher matcher)
    {
        matcher = NullMatcher.Instance;

        if (Current.Type == TokenType.LeftParen)
        {
            Advance(); // Consume '('
            if (!TryParseExpression(out var expression)) return false;

            if (Current.Type != TokenType.RightParen)
            {
                SetError("Expected ')'.");
                return false;
            }

            Advance(); // Consume ')'
            matcher = expression;
            return true;
        }

        if (!TryConsume(TokenType.Identifier, out var key))
        {
            SetError("Expected an identifier or '('.");
            return false;
        }

        // Lookahead to decide if it's a Path or a Predicate
        switch (Current.Type)
        {
            case TokenType.Equals or TokenType.FuzzyEquals:
                // It's a predicate: key=val
                var op = Current;
                Advance();
                if (!TryConsume(TokenType.Identifier, out var val))
                {
                    SetError($"Expected a value after '{op.Value}'.");
                    return false;
                }

                var desc = $"{key.Value} {op.Value} {val.Value}";

                matcher = new PredicateMatcher(
                    makeCheckKV(key.Value, val.Value, op.Type == TokenType.FuzzyEquals),
                    desc,
                    key.Value,
                    val.Value,
                    op.Type == TokenType.FuzzyEquals);
                return true;

            case TokenType.Child or TokenType.ArrayItem or TokenType.Dot or TokenType.DoubleDot:
                // A combinator immediately after a bare identifier is a property path.
                var combinatorToken = Current;
                Advance();
                if (!TryParseRelation(out var nextRequirement)) return false;

                var combinator = combinatorToken.Type switch
                {
                    TokenType.ArrayItem => NfaCombinator.ArrayItem,
                    TokenType.DoubleDot => NfaCombinator.Descendant,
                    _ => NfaCombinator.Child // Treat '>' and '.' as the same
                };

                matcher = new PathMatcher(key.Value, combinator, nextRequirement);
                return true;

            default:
                // It's a key existence check (e.g., just 'Components' followed by & or EOF)
                matcher = new PredicateMatcher(
                    e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key.Value, out _),
                    key.Value,
                    key.Value);
                return true;
        }
    }

    private Token Current => tokens[position];
    private void Advance() { if (position < tokens.Count - 1) position++; }
    private bool TryConsume(TokenType type, out Token token)
    {
        if (Current.Type == type)
        {
            token = Current;
            Advance();
            return true;
        }

        token = new(TokenType.Unknown, "");
        return false;
    }

    private void SetError(string message)
    {
        if (ErrorMessage.Length == 0)
            ErrorMessage = message;
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

            case RelationMatcher relation:
                sb.AppendLine($"{indent}RELATION ({relation.Combinator})");
                DumpAstRecursive(relation.Anchor, indent + "  ", sb);
                DumpAstRecursive(relation.Target, indent + "  ", sb);
                break;

            case PredicateMatcher pred:
                sb.AppendLine($"{indent}PREDICATE ({pred.Desc})");
                break;
        }
    }

}
