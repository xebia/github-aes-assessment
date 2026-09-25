namespace AesAssessment.Core;

public sealed class AssessmentValidator
{
    private static readonly HashSet<string> EvidenceStatuses =
        new(["observed", "team_reported", "inferred", "unknown"], StringComparer.Ordinal);
    private static readonly HashSet<string> Activities =
        new(["define", "deliver", "detect"], StringComparer.Ordinal);
    private static readonly HashSet<string> CategoryKinds =
        new(["stock", "activity", "participation"], StringComparer.Ordinal);
    private static readonly HashSet<string> StockNames =
        new(["governance", "shared_knowledge", "customer_value"], StringComparer.Ordinal);
    private static readonly HashSet<string> ParticipationNames =
        new(["director", "performer", "assessor"], StringComparer.Ordinal);

    public IReadOnlyList<string> Validate(AssessmentInput input)
    {
        var errors = new List<string>();
        if (input.SchemaVersion != SchemaVersions.Input)
            errors.Add($"schemaVersion must be '{SchemaVersions.Input}'.");
        if (string.IsNullOrWhiteSpace(input.AssessmentId))
            errors.Add("assessmentId is required.");
        if (string.IsNullOrWhiteSpace(input.Workflow))
            errors.Add("workflow is required.");
        if (input.Evidence.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != input.Evidence.Count)
            errors.Add("Evidence IDs must be unique.");
        foreach (var item in input.Evidence)
        {
            if (!EvidenceStatuses.Contains(item.Status))
                errors.Add($"Evidence '{item.Id}' has unsupported status '{item.Status}'.");
        }

        var evidenceIds = input.Evidence.Select(item => item.Id)
            .Concat(input.Questionnaire.Select(answer => answer.Id))
            .ToHashSet(StringComparer.Ordinal);
        ValidateReferences(input.Questionnaire.SelectMany(answer => answer.EvidenceReferences), evidenceIds, errors);
        return errors;
    }

    public IReadOnlyList<string> Validate(AssessmentReport report)
    {
        var errors = new List<string>();
        if (report.SchemaVersion != SchemaVersions.Assessment)
            errors.Add($"schemaVersion must be '{SchemaVersions.Assessment}'.");
        if (string.IsNullOrWhiteSpace(report.AssessmentId))
            errors.Add("assessmentId is required.");
        if (report.WorkflowMap.Count != 3 ||
            !report.WorkflowMap.Select(stage => stage.Activity).ToHashSet(StringComparer.Ordinal).SetEquals(Activities))
            errors.Add("workflowMap must contain define, deliver and detect exactly once.");

        var evidenceIds = report.SourceEvidence.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        if (evidenceIds.Count != report.SourceEvidence.Count)
            errors.Add("Source evidence IDs must be unique.");

        foreach (var finding in report.Findings)
        {
            if (!EvidenceStatuses.Contains(finding.EvidenceStatus))
                errors.Add($"Finding '{finding.Id}' has unsupported evidenceStatus '{finding.EvidenceStatus}'.");
            if (!CategoryKinds.Contains(finding.AesCategory.Kind))
                errors.Add($"Finding '{finding.Id}' has unsupported AES category kind '{finding.AesCategory.Kind}'.");
            if (!IsCategoryNameValid(finding.AesCategory))
                errors.Add($"Finding '{finding.Id}' has unsupported AES category name '{finding.AesCategory.Name}'.");
            if (finding.EvidenceStatus is "observed" or "team_reported" && finding.EvidenceReferences.Count == 0)
                errors.Add($"Finding '{finding.Id}' requires at least one evidence reference.");
            if (finding.EvidenceStatus == "unknown" && string.IsNullOrWhiteSpace(finding.ConfirmationQuestion))
                errors.Add($"Unknown finding '{finding.Id}' requires a confirmationQuestion.");
        }

        ValidateReferences(
            report.Findings.SelectMany(item => item.EvidenceReferences)
                .Concat(report.WorkflowMap.SelectMany(item => item.EvidenceReferences))
                .Concat(report.Unknowns.SelectMany(item => item.EvidenceReferences))
                .Concat(report.ProposedPilot.EvidenceReferences)
                .Concat(report.Measures.SelectMany(item => item.EvidenceReferences)),
            evidenceIds,
            errors);

        return errors;
    }

    private static bool IsCategoryNameValid(AesCategory category) =>
        category.Kind switch
        {
            "stock" => StockNames.Contains(category.Name),
            "activity" => Activities.Contains(category.Name),
            "participation" => ParticipationNames.Contains(category.Name),
            _ => false
        };

    private static void ValidateReferences(
        IEnumerable<string> references,
        IReadOnlySet<string> validIds,
        ICollection<string> errors)
    {
        foreach (var reference in references.Distinct(StringComparer.Ordinal))
        {
            if (!validIds.Contains(reference))
                errors.Add($"Evidence reference '{reference}' does not resolve.");
        }
    }
}
