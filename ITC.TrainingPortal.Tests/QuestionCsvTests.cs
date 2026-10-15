using ITC.TrainingPortal.Application.Services;
using ITC.TrainingPortal.Web.Services;

namespace ITC.TrainingPortal.Tests;

public sealed class QuestionCsvTests
{
    [Theory]
    [InlineData("ISOLATE the power-supply!", "isolate;power supply", true)]
    [InlineData("Isolate the power", "isolate;power supply", false)]
    [InlineData("unsafe", "safe", false)]
    [InlineData("Wear safety gloves", "safety gloves", true)]
    [InlineData("Wear gloves for safety", "safety gloves", false)]
    [InlineData("Anything", "", false)]
    [InlineData("Anything", ";;", false)]
    public void Matching_Requires_All_Whole_Words_And_Phrases(string answer, string keywords, bool expected)
        => Assert.Equal(expected, KeywordAnswerMatcher.IsMatch(answer, keywords));

    [Fact]
    public void Template_Parses_Both_Question_Types()
    {
        var rows = QuestionCsvReader.Read(new StringReader(QuestionCsvReader.Template), 10);
        Assert.Equal(2, rows.Count);
        Assert.False(rows[0].IsTypedAnswer);
        Assert.True(rows[1].IsTypedAnswer);
        Assert.Equal("isolate;power supply", rows[1].AnswerKeywords);
    }

    [Fact]
    public void Quoted_Commas_Newlines_And_Quotes_Are_Preserved()
    {
        var csv = QuestionCsvReader.Header + "\r\n\"Explain, with \"\"care\"\"\r\nthe steps\",Typed,,,,,,isolate,1\r\n";
        var question = Assert.Single(QuestionCsvReader.Read(new StringReader(csv), 10));
        Assert.Contains("Explain, with \"care\"", question.QuestionText);
        Assert.Contains("the steps", question.QuestionText);
    }

    [Theory]
    [InlineData("Q,Typed,,,,,,,1", "AnswerKeywords")]
    [InlineData("Q,Typed,,,,,,!!!,1", "letters or numbers")]
    [InlineData("Q,Unknown,,,,,,word,1", "AnswerType")]
    [InlineData("Q,MCQ,A,B,C,D,Z,,1", "correct option")]
    [InlineData("Q,Typed,,,,,,word,0", "positive")]
    [InlineData("Q,Typed", "9 columns")]
    [InlineData("\"Unclosed,Typed,,,,,,word,1", "quoting")]
    public void Bad_Rows_Report_Actionable_Errors(string row, string expected)
    {
        var error = Assert.Throws<InvalidOperationException>(() => QuestionCsvReader.Read(new StringReader(QuestionCsvReader.Header + "\n" + row), 10));
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Duplicate_Order_Is_Rejected() => Assert.Throws<InvalidOperationException>(() =>
        QuestionCsvReader.Read(new StringReader(QuestionCsvReader.Header + "\nQ,Typed,,,,,,word,1\nQ2,Typed,,,,,,word,1"), 10));
}
