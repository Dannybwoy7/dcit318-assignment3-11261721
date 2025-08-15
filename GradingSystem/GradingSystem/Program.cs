using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SchoolGradingSystem
//students.txt input file and report.txt output file located

//in dcit318-assignment3-11261721\GradingSystem\GradingSystem\bin\Debug\net8.0
{
    // ---------- Custom Exceptions ----------
    public class InvalidScoreFormatException : Exception
    {
        public InvalidScoreFormatException(string message) : base(message) { }
    }

    public class MissingFieldException : Exception
    {
        public MissingFieldException(string message) : base(message) { }
    }

    // ---------- Student class ----------
    public class Student
    {
        public int Id { get; }
        public string FullName { get; }
        public int Score { get; }

        public Student(int id, string fullName, int score)
        {
            Id = id;
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Score = score;
        }

        // Grade assignment per specification
        public string GetGrade()
        {
            if (Score >= 80 && Score <= 100) return "A";
            if (Score >= 70 && Score <= 79) return "B";
            if (Score >= 60 && Score <= 69) return "C";
            if (Score >= 50 && Score <= 59) return "D";
            return "F";
        }

        public override string ToString()
            => $"{FullName} (ID: {Id}): Score = {Score}, Grade = {GetGrade()}";
    }

    // ---------- Student result processing ----------
    public class StudentResultProcessor
    {
        /// <summary>
        /// Reads students from a text file where each line is:
        /// Id, FullName, Score
        /// </summary>
        /// <param name="inputFilePath">Path to the input file</param>
        /// <returns>List of valid Student objects</returns>
        public List<Student> ReadStudentsFromFile(string inputFilePath)
        {
            var students = new List<Student>();

            // Let FileNotFoundException bubble up to caller per spec.
            using var reader = new StreamReader(inputFilePath);

            string? line;
            int lineNumber = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                // Skip empty/whitespace-only lines
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Split by comma
                var parts = line.Split(',');

                // If fewer than 3 tokens -> missing fields
                if (parts.Length < 3)
                {
                    throw new MissingFieldException($"Line {lineNumber}: Missing field(s). Expected format 'Id, FullName, Score'. Line contents: '{line}'");
                }

                // Handle cases where the name contains commas by:
                // - taking first token as Id, last token as Score, and joining middle tokens as FullName
                var idToken = parts[0].Trim();
                var scoreToken = parts[^1].Trim();
                var nameTokens = new string[parts.Length - 2];
                Array.Copy(parts, 1, nameTokens, 0, parts.Length - 2);
                var fullName = string.Join(",", nameTokens).Trim();

                // Validate ID
                if (!int.TryParse(idToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    // ID parse failure treated as MissingFieldException per strictness (spec didn't require custom)
                    throw new MissingFieldException($"Line {lineNumber}: Invalid or missing ID value ('{idToken}').");
                }

                // Validate Score
                if (!int.TryParse(scoreToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var score))
                {
                    throw new InvalidScoreFormatException($"Line {lineNumber}: Score '{scoreToken}' is not a valid integer.");
                }

                // Optional: ensure score within 0-100. If out of range, treat as invalid format (or you may handle differently).
                if (score < 0 || score > 100)
                {
                    throw new InvalidScoreFormatException($"Line {lineNumber}: Score '{score}' is out of valid range (0-100).");
                }

                // Final check for fullName non-empty
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    throw new MissingFieldException($"Line {lineNumber}: FullName is missing or empty.");
                }

                // Create Student and add
                var student = new Student(id, fullName, score);
                students.Add(student);
            }

            return students;
        }

        /// <summary>
        /// Writes a summary report to the output file with lines like:
        /// "Alice Smith (ID: 101): Score = 84, Grade = A"
        /// </summary>
        public void WriteReportToFile(List<Student> students, string outputFilePath)
        {
            using var writer = new StreamWriter(outputFilePath, append: false);

            foreach (var student in students)
            {
                writer.WriteLine(student.ToString());
            }
        }
    }

    // ---------- Program entry ----------
    class Program
    {
        static void Main()
        {
            // Default file names - change paths as needed
            const string inputFile = "students.txt";
            const string outputFile = "report.txt";

            var processor = new StudentResultProcessor();

            try
            {
                Console.WriteLine($"Reading students from '{inputFile}'...");
                var students = processor.ReadStudentsFromFile(inputFile);

                Console.WriteLine($"Read {students.Count} valid student(s). Writing report to '{outputFile}'...");
                processor.WriteReportToFile(students, outputFile);

                Console.WriteLine("Report written successfully. Summary:");
                foreach (var s in students)
                    Console.WriteLine(" - " + s);

                Console.WriteLine($"\nDone. Output file: {Path.GetFullPath(outputFile)}");
            }
            catch (FileNotFoundException fnf)
            {
                Console.WriteLine($"File not found: {fnf.FileName ?? inputFile}. Please ensure the input file exists.");
            }
            catch (InvalidScoreFormatException isf)
            {
                Console.WriteLine($"Invalid score format: {isf.Message}");
            }
            catch (MissingFieldException mf)
            {
                Console.WriteLine($"Missing field error: {mf.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
