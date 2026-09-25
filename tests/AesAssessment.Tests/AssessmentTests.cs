using AesAssessment.Core;
using Json.Schema;
using System.Text.Json;

namespace AesAssessment.Tests;

public sealed class AssessmentTests
{
    [Theory]
    [InlineData("schemas/v1/assessment-input.schema.json", "assessments/fablecart/input.json")]
    [InlineData("schemas/v1/assessment-report.schema.json", "assessments/fablecart/assessment.json")]
    public async Task CheckedInExamplesConformToPublishedJsonSchemas(string schemaPath, string documentPath)
    {
        var root = FindRepositoryRoot();
        var schema = JsonSchema.FromText(await File.ReadAllTextAsync(Path.Combine(root, PathFromSlash(schemaPath))));
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, PathFromSlash(documentPath))));

        var result = schema.Evaluate(document.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        Assert.True(result.IsValid, string.Join(
            Environment.NewLine,
            (result.Details ?? []).Where(detail => !detail.IsValid).Select(detail => detail.ToString())));
    }

    [Fact]
    public async Task ExampleAssessmentPassesDomainAndProvenanceValidation()
    {
        var root = FindRepositoryRoot();
        var report = await JsonSupport.ReadAsync<AssessmentReport>(
            Path.Combine(root, "assessments", "fablecart", "assessment.json"));

        var errors = new AssessmentValidator().Validate(report);

        Assert.Empty(errors);
        Assert.All(
            report.Findings.SelectMany(finding => finding.EvidenceReferences),
            reference => Assert.Contains(report.SourceEvidence, evidence => evidence.Id == reference));
    }

    [Fact]
    public async Task CollectorIsDeterministicAndLabelsAllFixtureEvidenceFictional()
    {
        var source = Path.Combine(FindRepositoryRoot(), "fixtures", "fablecart");
        var collector = new AssessmentInputCollector();

        var first = await collector.CollectFixtureAsync(source);
        var second = await collector.CollectFixtureAsync(source);

        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first, JsonSupport.Options),
            System.Text.Json.JsonSerializer.Serialize(second, JsonSupport.Options));
        Assert.True(first.Fictional);
        Assert.All(first.Evidence, evidence => Assert.True(evidence.Fictional));
        Assert.Contains(first.Evidence, evidence => evidence.Id == AgentRcEvidenceCollector.EvidenceId);
    }

    [Fact]
    public void ValidatorRejectsUnresolvedEvidenceReference()
    {
        var report = MinimalReport() with
        {
            Findings =
            [
                new Finding
                {
                    Id = "finding-1",
                    Claim = "A claim",
                    AesCategory = new AesCategory { Kind = "stock", Name = "governance" },
                    EvidenceReferences = ["missing"],
                    EvidenceStatus = "observed",
                    Confidence = "high"
                }
            ]
        };

        var errors = new AssessmentValidator().Validate(report);

        Assert.Contains(errors, error => error.Contains("'missing'", StringComparison.Ordinal));
    }

    [Fact]
    public void RendererShowsUnknownAndNotMeasuredValuesWithoutInventingValues()
    {
        var report = MinimalReport() with
        {
            Unknowns =
            [
                new UnknownItem
                {
                    Topic = "Customer outcome",
                    Reason = "No measurement exists.",
                    ConfirmationQuestion = "What should be measured?",
                    EvidenceReferences = []
                }
            ],
            Measures =
            [
                new Measure
                {
                    Name = "Defect rate",
                    Status = "not_measured",
                    Value = null,
                    MeasurementNeeded = "Define and instrument the measure.",
                    EvidenceReferences = []
                }
            ]
        };

        var html = new StaticReportRenderer().Render(report);

        Assert.Contains("not_measured", html);
        Assert.Contains("not measured", html);
        Assert.Contains("No measurement exists.", html);
        Assert.DoesNotContain("maturity score:", html, StringComparison.OrdinalIgnoreCase);
    }

    private static AssessmentReport MinimalReport() => new()
    {
        SchemaVersion = SchemaVersions.Assessment,
        AssessmentId = "test",
        Fictional = true,
        Organization = "Test",
        Workflow = "Test workflow",
        AssessmentDate = "2026-09-25",
        WorkflowMap =
        [
            Stage("define"),
            Stage("deliver"),
            Stage("detect")
        ],
        Findings = [],
        Unknowns = [],
        ProposedPilot = new ProposedPilot
        {
            Scope = "Draft only",
            AllowedActions = [],
            ProhibitedActions = [],
            HumanGates = [],
            EvidenceReferences = []
        },
        Measures = [],
        SourceEvidence = []
    };

    private static WorkflowStage Stage(string activity) => new()
    {
        Activity = activity,
        Description = "Test",
        Director = "unknown",
        Performer = "unknown",
        Assessor = "unknown",
        EvidenceReferences = [],
        Status = "unknown"
    };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AesAssessment.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static string PathFromSlash(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar);
}
