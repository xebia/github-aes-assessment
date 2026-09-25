using System.Diagnostics;
using System.Text.Json;

namespace AesAssessment.Core;

public interface IEvidenceCollector
{
    Task<IReadOnlyList<EvidenceItem>> CollectAsync(
        EvidenceCollectionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record EvidenceCollectionRequest
{
    public required string Source { get; init; }
    public required bool Fictional { get; init; }
    public string? Location { get; init; }
}

public sealed class AgentRcEvidenceCollector : IEvidenceCollector
{
    public const string EvidenceId = "agentrc-readiness";
    public const string EvaluatedRevision = "microsoft/agentrc 2.1.0 (main at daa92160534031186327e3d8b25b11beeda7c021)";

    public async Task<IReadOnlyList<EvidenceItem>> CollectAsync(
        EvidenceCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var content = File.Exists(request.Source)
            ? await File.ReadAllTextAsync(request.Source, cancellationToken)
            : await RunAgentRcAsync(request.Source, cancellationToken);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        if (!root.TryGetProperty("ok", out var ok) || !ok.GetBoolean() ||
            !root.TryGetProperty("status", out var status) || status.GetString() != "success" ||
            !root.TryGetProperty("data", out _))
        {
            throw new InvalidDataException("AgentRC did not return a successful CommandResult envelope.");
        }

        return
        [
            new EvidenceItem
            {
                Id = EvidenceId,
                SourceType = "agentrc_readiness",
                Title = $"AgentRC repository-readiness result ({EvaluatedRevision})",
                Location = request.Location ?? request.Source,
                Status = "observed",
                Content = content.Trim(),
                Fictional = request.Fictional
            }
        ];
    }

    private static async Task<string> RunAgentRcAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveNpx(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("github:microsoft/agentrc");
        startInfo.ArgumentList.Add("readiness");
        startInfo.ArgumentList.Add(repositoryPath);
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("--quiet");
        startInfo.ArgumentList.Add("--per-area");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidDataException("Could not start AgentRC through npx.");
        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidDataException(
                $"AgentRC exited with code {process.ExitCode}: {(await standardError).Trim()}");
        }

        return await standardOutput;
    }

    private static string ResolveNpx()
    {
        var executable = OperatingSystem.IsWindows() ? "npx.cmd" : "npx";
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "")
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), executable);
            if (File.Exists(candidate))
                return candidate;
        }

        return executable;
    }
}

public sealed class SupplementalFixtureEvidenceCollector : IEvidenceCollector
{
    public async Task<IReadOnlyList<EvidenceItem>> CollectAsync(
        EvidenceCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var manifest = await JsonSupport.ReadAsync<FixtureManifest>(
            Path.Combine(request.Source, "manifest.json"),
            cancellationToken);
        var evidence = new List<EvidenceItem>();
        foreach (var item in manifest.SupplementalEvidence.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            var contentPath = ResolveWithin(request.Source, item.ContentFile);
            evidence.Add(new EvidenceItem
            {
                Id = item.Id,
                SourceType = item.SourceType,
                Title = item.Title,
                Location = item.Location,
                Status = item.Status,
                Content = await File.ReadAllTextAsync(contentPath, cancellationToken),
                Fictional = request.Fictional
            });
        }
        return evidence;
    }

    internal static string ResolveWithin(string source, string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(source, relativePath));
        var sourceRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Fixture path '{path}' is outside the fixture directory.");
        return path;
    }
}

public sealed class AssessmentInputCollector
{
    public async Task<AssessmentInput> CollectFixtureAsync(
        string source,
        string? liveRepositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        var manifest = await JsonSupport.ReadAsync<FixtureManifest>(
            Path.Combine(source, "manifest.json"),
            cancellationToken);
        var agentRcSource = liveRepositoryPath ??
            SupplementalFixtureEvidenceCollector.ResolveWithin(source, manifest.AgentRcFile);
        IEvidenceCollector agentRcCollector = new AgentRcEvidenceCollector();
        IEvidenceCollector supplementalCollector = new SupplementalFixtureEvidenceCollector();

        var agentRcEvidence = await agentRcCollector.CollectAsync(
            new EvidenceCollectionRequest
            {
                Source = agentRcSource,
                Location = liveRepositoryPath is null ? manifest.AgentRcFile : liveRepositoryPath,
                Fictional = manifest.Fictional
            },
            cancellationToken);
        var supplementalEvidence = await supplementalCollector.CollectAsync(
            new EvidenceCollectionRequest { Source = source, Fictional = manifest.Fictional },
            cancellationToken);
        var questionnairePath = SupplementalFixtureEvidenceCollector.ResolveWithin(
            source,
            manifest.QuestionnaireFile);
        var questionnaire = await JsonSupport.ReadAsync<IReadOnlyList<QuestionnaireAnswer>>(
            questionnairePath,
            cancellationToken);

        return new AssessmentInput
        {
            SchemaVersion = SchemaVersions.Input,
            AssessmentId = manifest.AssessmentId,
            Fictional = manifest.Fictional,
            Organization = manifest.Organization,
            Workflow = manifest.Workflow,
            AssessmentDate = manifest.AssessmentDate,
            Evidence = agentRcEvidence.Concat(supplementalEvidence)
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .ToArray(),
            Questionnaire = questionnaire.OrderBy(answer => answer.Id, StringComparer.Ordinal).ToArray()
        };
    }
}

public sealed record FixtureManifest
{
    public required string AssessmentId { get; init; }
    public required bool Fictional { get; init; }
    public required string Organization { get; init; }
    public required string Workflow { get; init; }
    public required string AssessmentDate { get; init; }
    public required string AgentRcFile { get; init; }
    public required string QuestionnaireFile { get; init; }
    public required IReadOnlyList<FixtureEvidenceEntry> SupplementalEvidence { get; init; }
}

public sealed record FixtureEvidenceEntry
{
    public required string Id { get; init; }
    public required string SourceType { get; init; }
    public required string Title { get; init; }
    public required string Location { get; init; }
    public required string Status { get; init; }
    public required string ContentFile { get; init; }
}
