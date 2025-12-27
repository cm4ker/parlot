using Parlot.Fluent;
using System.Linq;
using Xunit;

using static Parlot.Fluent.Parsers;

namespace Parlot.Tests;

public class ErrorCollectionTests
{
    [Fact]
    public void ContinueOnErrorDefaultsToFalse()
    {
        var scanner = new Scanner("test");
        var context = new ParseContext(scanner);
        
        Assert.False(context.ContinueOnError);
    }

    [Fact]
    public void ErrorsCollectionIsInitiallyEmpty()
    {
        var scanner = new Scanner("test");
        var context = new ParseContext(scanner);
        
        Assert.NotNull(context.Errors);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void ElseErrorShouldThrowWhenContinueOnErrorIsFalse()
    {
        Parser<char> parser = Terms.Char('a').ElseError("Expected 'a'");
        
        var exception = Assert.Throws<ParseException>(() => parser.Parse("b"));
        Assert.Equal("Expected 'a'", exception.Message);
    }

    [Fact]
    public void ElseErrorShouldAddToErrorsWhenContinueOnErrorIsTrue()
    {
        Parser<char> parser = Terms.Char('a').ElseError("Expected 'a'");
        var scanner = new Scanner("b");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result = new ParseResult<char>();
        var success = parser.Parse(context, ref result);
        
        Assert.False(success);
        Assert.Single(context.Errors);
        Assert.Equal("Expected 'a'", context.Errors[0].Message);
    }

    [Fact]
    public void ErrorShouldThrowWhenContinueOnErrorIsFalse()
    {
        Parser<char> parser = Terms.Char('a').Error("Should not match 'a'");
        
        var exception = Assert.Throws<ParseException>(() => parser.Parse("a"));
        Assert.Equal("Should not match 'a'", exception.Message);
    }

    [Fact]
    public void ErrorShouldAddToErrorsWhenContinueOnErrorIsTrue()
    {
        Parser<char> parser = Terms.Char('a').Error("Should not match 'a'");
        var scanner = new Scanner("a");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result = new ParseResult<char>();
        var success = parser.Parse(context, ref result);
        
        Assert.False(success);
        Assert.Single(context.Errors);
        Assert.Equal("Should not match 'a'", context.Errors[0].Message);
    }

    [Fact]
    public void MultipleErrorsShouldBeCollected()
    {
        // Create a parser that tries to match 'a' or 'b', both with error messages
        Parser<char> parserA = Terms.Char('a').ElseError("Expected 'a'");
        Parser<char> parserB = Terms.Char('b').ElseError("Expected 'b'");
        
        var scanner = new Scanner("c");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result1 = new ParseResult<char>();
        var success1 = parserA.Parse(context, ref result1);
        
        Assert.False(success1);
        Assert.Single(context.Errors);
        
        // Reset scanner position for second parser
        scanner.Cursor.ResetPosition(new TextPosition(0, 1, 1));
        
        var result2 = new ParseResult<char>();
        var success2 = parserB.Parse(context, ref result2);
        
        Assert.False(success2);
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal("Expected 'a'", context.Errors[0].Message);
        Assert.Equal("Expected 'b'", context.Errors[1].Message);
    }

    [Fact]
    public void ErrorsIncludeCorrectPosition()
    {
        Parser<char> parser = Terms.Char('a').ElseError("Expected 'a'");
        var scanner = new Scanner("b");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result = new ParseResult<char>();
        parser.Parse(context, ref result);
        
        Assert.Single(context.Errors);
        Assert.Equal(0, context.Errors[0].Position.Offset);
        Assert.Equal(1, context.Errors[0].Position.Line);
        Assert.Equal(1, context.Errors[0].Position.Column);
    }

    [Fact]
    public void ContinueOnErrorAllowsParsingToContinue()
    {
        // Create a parser that tries multiple alternatives
        Parser<char> parser = OneOf(
            Terms.Char('a').ElseError("Expected 'a'"),
            Terms.Char('b').ElseError("Expected 'b'"),
            Terms.Char('c')
        );
        
        var scanner = new Scanner("c");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result = new ParseResult<char>();
        var success = parser.Parse(context, ref result);
        
        // Should succeed on 'c' after failing on 'a' and 'b'
        Assert.True(success);
        Assert.Equal('c', result.Value);
        
        // The errors from 'a' and 'b' should still be collected
        Assert.Equal(2, context.Errors.Count);
    }

    [Fact]
    public void BackwardCompatibilityWithOldConstructors()
    {
        // Old constructor without continueOnError parameter
        var scanner = new Scanner("test");
        var context1 = new ParseContext(scanner, useNewLines: false);
        Assert.False(context1.ContinueOnError);

        var context2 = new ParseContext(scanner, System.Threading.CancellationToken.None);
        Assert.False(context2.ContinueOnError);
    }

    [Fact]
    public void ErrorWithGenericTypes()
    {
        // Test Error<T, U> variant (Error<U> method which creates Error<T, U> parser)
        Parser<char> innerParser = Terms.Char('a');
        Parser<object> parser = innerParser.Error<object>("Should not match 'a'");
        
        var scanner = new Scanner("a");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result = new ParseResult<object>();
        var success = parser.Parse(context, ref result);
        
        Assert.False(success);
        Assert.Single(context.Errors);
        Assert.Equal("Should not match 'a'", context.Errors[0].Message);
    }

    [Fact]
    public void ParseContextWithAllParameters()
    {
        var scanner = new Scanner("test");
        var context = new ParseContext(
            scanner,
            useNewLines: true,
            disableLoopDetection: true,
            continueOnError: true
        );
        
        Assert.True(context.UseNewLines);
        Assert.True(context.DisableLoopDetection);
        Assert.True(context.ContinueOnError);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void ClearingErrorsWorks()
    {
        Parser<char> parser = Terms.Char('a').ElseError("Expected 'a'");
        var scanner = new Scanner("b");
        var context = new ParseContext(scanner, continueOnError: true);
        
        var result = new ParseResult<char>();
        parser.Parse(context, ref result);
        
        Assert.Single(context.Errors);
        
        context.Errors.Clear();
        Assert.Empty(context.Errors);
    }
}
