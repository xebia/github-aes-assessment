using AesAssessment.Core;

return await Cli.RunAsync(args);

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help")
            {
                PrintHelp();
                return 0;
            }

            return args[0] switch
            {
                "collect" => await CollectAsync(args, liveAgentRc: false),
                "collect-live" => await CollectAsync(args, liveAgentRc: true),
                "validate-input" => await ValidateInputAsync(args),
                "validate-report" => await ValidateReportAsync(args),
                "render" => await RenderAsync(args),
                _ => UsageError($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> CollectAsync(string[] args, bool liveAgentRc)
    {
        var expectedCount = liveAgentRc ? 4 : 3;
        RequireCount(
            args,
            expectedCount,
            liveAgentRc
                ? "collect-live <supplement-directory> <repository-directory> <output.json>"
                : "collect <fixture-directory> <output.json>");
        var input = await new AssessmentInputCollector().CollectFixtureAsync(
            args[1],
            liveAgentRc ? args[2] : null);
        var errors = new AssessmentValidator().Validate(input);
        if (errors.Count > 0)
            return ValidationFailure(errors);
        var output = liveAgentRc ? args[3] : args[2];
        await JsonSupport.WriteAsync(output, input);
        Console.WriteLine($"Collected {input.Evidence.Count} evidence items into {output}.");
        return 0;
    }

    private static async Task<int> ValidateInputAsync(string[] args)
    {
        RequireCount(args, 2, "validate-input <input.json>");
        var input = await JsonSupport.ReadAsync<AssessmentInput>(args[1]);
        return ValidationResult(new AssessmentValidator().Validate(input));
    }

    private static async Task<int> ValidateReportAsync(string[] args)
    {
        RequireCount(args, 2, "validate-report <assessment.json>");
        var report = await JsonSupport.ReadAsync<AssessmentReport>(args[1]);
        return ValidationResult(new AssessmentValidator().Validate(report));
    }

    private static async Task<int> RenderAsync(string[] args)
    {
        RequireCount(args, 3, "render <assessment.json> <output-directory>");
        var report = await JsonSupport.ReadAsync<AssessmentReport>(args[1]);
        var errors = new AssessmentValidator().Validate(report);
        if (errors.Count > 0)
            return ValidationFailure(errors);
        Directory.CreateDirectory(args[2]);
        var outputPath = Path.Combine(args[2], "index.html");
        await File.WriteAllTextAsync(outputPath, new StaticReportRenderer().Render(report));
        Console.WriteLine($"Rendered {outputPath}.");
        return 0;
    }

    private static int ValidationResult(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            Console.WriteLine("Validation succeeded.");
            return 0;
        }
        return ValidationFailure(errors);
    }

    private static int ValidationFailure(IEnumerable<string> errors)
    {
        foreach (var error in errors)
            Console.Error.WriteLine($"Validation error: {error}");
        return 1;
    }

    private static void RequireCount(string[] args, int count, string usage)
    {
        if (args.Length != count)
            throw new InvalidDataException($"Usage: aes-assessment {usage}");
    }

    private static int UsageError(string message)
    {
        Console.Error.WriteLine(message);
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            AES assessment CLI

              collect <fixture-directory> <output.json>
              collect-live <supplement-directory> <repository-directory> <output.json>
              validate-input <input.json>
              validate-report <assessment.json>
              render <assessment.json> <output-directory>
            """);
    }
}
