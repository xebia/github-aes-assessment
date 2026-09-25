using System.Text.Json.Serialization;

namespace AesAssessment.Core;

public static class SchemaVersions
{
    public const string Input = "1.0.0";
    public const string Assessment = "1.0.0";
}

public sealed record AssessmentInput
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("assessmentId")]
    public required string AssessmentId { get; init; }

    [JsonPropertyName("fictional")]
    public required bool Fictional { get; init; }

    [JsonPropertyName("organization")]
    public required string Organization { get; init; }

    [JsonPropertyName("workflow")]
    public required string Workflow { get; init; }

    [JsonPropertyName("assessmentDate")]
    public required string AssessmentDate { get; init; }

    [JsonPropertyName("evidence")]
    public required IReadOnlyList<EvidenceItem> Evidence { get; init; }

    [JsonPropertyName("questionnaire")]
    public required IReadOnlyList<QuestionnaireAnswer> Questionnaire { get; init; }
}

public sealed record EvidenceItem
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("sourceType")]
    public required string SourceType { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("location")]
    public required string Location { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("fictional")]
    public required bool Fictional { get; init; }
}

public sealed record QuestionnaireAnswer
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("question")]
    public required string Question { get; init; }

    [JsonPropertyName("answer")]
    public string? Answer { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required IReadOnlyList<string> EvidenceReferences { get; init; }
}

public sealed record AssessmentReport
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("assessmentId")]
    public required string AssessmentId { get; init; }

    [JsonPropertyName("fictional")]
    public required bool Fictional { get; init; }

    [JsonPropertyName("organization")]
    public required string Organization { get; init; }

    [JsonPropertyName("workflow")]
    public required string Workflow { get; init; }

    [JsonPropertyName("assessmentDate")]
    public required string AssessmentDate { get; init; }

    [JsonPropertyName("workflowMap")]
    public required IReadOnlyList<WorkflowStage> WorkflowMap { get; init; }

    [JsonPropertyName("findings")]
    public required IReadOnlyList<Finding> Findings { get; init; }

    [JsonPropertyName("unknowns")]
    public required IReadOnlyList<UnknownItem> Unknowns { get; init; }

    [JsonPropertyName("proposedPilot")]
    public required ProposedPilot ProposedPilot { get; init; }

    [JsonPropertyName("measures")]
    public required IReadOnlyList<Measure> Measures { get; init; }

    [JsonPropertyName("sourceEvidence")]
    public required IReadOnlyList<EvidenceItem> SourceEvidence { get; init; }
}

public sealed record WorkflowStage
{
    [JsonPropertyName("activity")]
    public required string Activity { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("director")]
    public required string Director { get; init; }

    [JsonPropertyName("performer")]
    public required string Performer { get; init; }

    [JsonPropertyName("assessor")]
    public required string Assessor { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required IReadOnlyList<string> EvidenceReferences { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

public sealed record Finding
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("claim")]
    public required string Claim { get; init; }

    [JsonPropertyName("aesCategory")]
    public required AesCategory AesCategory { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required IReadOnlyList<string> EvidenceReferences { get; init; }

    [JsonPropertyName("evidenceStatus")]
    public required string EvidenceStatus { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }

    [JsonPropertyName("confirmationQuestion")]
    public string? ConfirmationQuestion { get; init; }
}

public sealed record AesCategory
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public sealed record UnknownItem
{
    [JsonPropertyName("topic")]
    public required string Topic { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("confirmationQuestion")]
    public required string ConfirmationQuestion { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required IReadOnlyList<string> EvidenceReferences { get; init; }
}

public sealed record ProposedPilot
{
    [JsonPropertyName("scope")]
    public required string Scope { get; init; }

    [JsonPropertyName("allowedActions")]
    public required IReadOnlyList<string> AllowedActions { get; init; }

    [JsonPropertyName("prohibitedActions")]
    public required IReadOnlyList<string> ProhibitedActions { get; init; }

    [JsonPropertyName("humanGates")]
    public required IReadOnlyList<string> HumanGates { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required IReadOnlyList<string> EvidenceReferences { get; init; }
}

public sealed record Measure
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("measurementNeeded")]
    public required string MeasurementNeeded { get; init; }

    [JsonPropertyName("evidenceReferences")]
    public required IReadOnlyList<string> EvidenceReferences { get; init; }
}
