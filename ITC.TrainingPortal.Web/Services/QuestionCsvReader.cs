using System.ComponentModel.DataAnnotations;
using ITC.TrainingPortal.Application.DTOs;
using Microsoft.VisualBasic.FileIO;

namespace ITC.TrainingPortal.Web.Services;

public static class QuestionCsvReader
{
    public const string Header = "QuestionText,AnswerType,OptionA,OptionB,OptionC,OptionD,CorrectOption,AnswerKeywords,SequenceOrder";
    public const string Template = Header + "\r\nWhich item protects your head?,MCQ,Helmet,Gloves,Boots,Goggles,A,,1\r\nDescribe safe isolation.,Typed,,,,,,isolate;power supply,2\r\n";

    public static IReadOnlyList<QuizQuestionDto> Read(TextReader reader, int quizId)
    {
        using var parser = new TextFieldParser(reader) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",");
        try
        {
            var header = parser.ReadFields();
            if (header == null || !header.SequenceEqual(Header.Split(','), StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("CSV headers must match the downloadable template, in the same order.");
            var questions = new List<QuizQuestionDto>();
            while (!parser.EndOfData)
            {
                var line = parser.LineNumber;
                var fields = parser.ReadFields()!;
                if (questions.Count == 500) throw new InvalidOperationException("Upload no more than 500 questions at a time.");
                if (fields.Length != 9) throw new InvalidOperationException($"Line {line}: expected 9 columns. Put text containing commas inside double quotes.");
                if (!string.Equals(fields[1], "MCQ", StringComparison.OrdinalIgnoreCase) && !string.Equals(fields[1], "Typed", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Line {line}: AnswerType must be MCQ or Typed.");
                if (!int.TryParse(fields[8], out var order) || order < 1)
                    throw new InvalidOperationException($"Line {line}: SequenceOrder must be a positive whole number.");
                var question = new QuizQuestionDto { QuizId = quizId, QuestionText = fields[0], IsTypedAnswer = fields[1].Equals("Typed", StringComparison.OrdinalIgnoreCase),
                    OptionA = fields[2], OptionB = fields[3], OptionC = fields[4], OptionD = fields[5], CorrectOption = fields[6].ToUpperInvariant(),
                    AnswerKeywords = fields[7], SequenceOrder = order };
                var errors = new List<ValidationResult>();
                if (!Validator.TryValidateObject(question, new(question), errors, true))
                    throw new InvalidOperationException($"Line {line}: {string.Join(" ", errors.Select(x => x.ErrorMessage))}");
                if (question.IsTypedAnswer && string.IsNullOrWhiteSpace(question.AnswerKeywords))
                    throw new InvalidOperationException($"Line {line}: provide AnswerKeywords for automatic marking of typed answers.");
                if (questions.Any(x => x.SequenceOrder == order))
                    throw new InvalidOperationException($"Line {line}: SequenceOrder {order} is repeated.");
                questions.Add(question);
            }
            if (questions.Count == 0) throw new InvalidOperationException("The CSV contains no questions.");
            return questions;
        }
        catch (MalformedLineException ex) { throw new InvalidOperationException($"Line {ex.LineNumber}: invalid CSV quoting. Check the template.", ex); }
    }
}
