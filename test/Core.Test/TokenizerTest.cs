using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class TokenizerTest
    {
        [Fact]
        public void ParseVariable()
        {
            // Arrange
            var comparer = new TokenEqualityComparer();

            // Act 
            var tokens = Parse("a $b$ c");

            // Assert
            Assert.Equal(
                new[] { 
                    new Token(TokenCategory.Text, "a "),
                    new Token(TokenCategory.Variable, "b"),
                    new Token(TokenCategory.Text, " c")
                },
                tokens,
                comparer);
        }

        // Test parsing escape "$$" into "$".
        [Fact]
        public void ParseEscape()
        {
            // Arrange
            var comparer = new TokenEqualityComparer();

            // Act 
            var tokens = Parse("a $$b$$ c");

            // Assert
            Assert.Equal(
                new[] { 
                    new Token(TokenCategory.Text, "a $b$ c")
                },
                tokens,
                comparer);
        }

        // Tests that an unfinished variable is parsed as text
        [Fact]
        public void ParseUnmatchedDollarSign()
        {
            // Arrange
            var comparer = new TokenEqualityComparer();

            // Act 
            var tokens = Parse("a $b$ $c");

            // Assert
            Assert.Equal(
                new[] { 
                    new Token(TokenCategory.Text, "a "),
                    new Token(TokenCategory.Variable, "b"),
                    new Token(TokenCategory.Text, " $c")
                },
                tokens,
                comparer);
        }

        // Parsing string containing two consequtive variables.
        [Fact]
        public void ParseVariables()
        {
            // Arrange
            var comparer = new TokenEqualityComparer();

            // Act 
            var tokens = Parse("a $b$$c$");

            // Assert
            Assert.Equal(
                new[] { 
                    new Token(TokenCategory.Text, "a "),
                    new Token(TokenCategory.Variable, "b"),
                    new Token(TokenCategory.Variable, "c")
                },
                tokens,
                comparer);
        }


        // Tests that a dollar sign at the end is returned as "$".
        [Fact]
        public void ParseDollarSignAtEnd()
        {
            // Arrange
            var comparer = new TokenEqualityComparer();

            // Act 
            var tokens = Parse("ab$");

            // Assert
            Assert.Equal(
                new[] { 
                    new Token(TokenCategory.Text, "ab$")
                },
                tokens,
                comparer);
        }

        // Tests that non word character cannot be used in a variable.
        [Fact]
        public void ParseNonwordCharInVariable()
        {
            // Arrange
            var comparer = new TokenEqualityComparer();

            // Act 
            var tokens = Parse("a $b c$d$");

            // Assert
            Assert.Equal(
                new[] { 
                    new Token(TokenCategory.Text, "a $b c"),
                    new Token(TokenCategory.Variable, "d"),
                },
                tokens,
                comparer);            
        }

        private class TokenEqualityComparer : IEqualityComparer<Token>
        {
            public bool Equals(Token x, Token y)
            {
                return x.Category == y.Category && x.Value == y.Value;
            }

            public int GetHashCode(Token obj)
            {
                return obj.GetHashCode();
            }
        }

        private static List<Token> Parse(string text)
        {
            var tokenizer = new Tokenizer(text);
            List<Token> result = new List<Token>();
            for (; ; )
            {
                var token = tokenizer.Read();
                if (token == null)
                {
                    break;
                }

                var lastToken = result.Count >= 1 ? result[result.Count - 1] : null;
                if (token.Category == TokenCategory.Text && lastToken != null &&
                    lastToken.Category == TokenCategory.Text)
                {
                    result[result.Count - 1] = new Token(
                        TokenCategory.Text,
                        lastToken.Value + token.Value);
                }
                else
                {
                    result.Add(token);
                }
            }

            return result;
        }
    }
}
