using System.Net;
using System.Text;

namespace AesAssessment.Core;

public sealed class StaticReportRenderer
{
    public string Render(AssessmentReport report)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "not measured");
        static string Refs(IEnumerable<string> references) =>
            string.Join(", ", references.Select(reference => $"<a href=\"#evidence-{E(reference)}\">{E(reference)}</a>"));
        static string Items(IEnumerable<string> values) =>
            string.Join("", values.Select(value => $"<li>{E(value)}</li>"));

        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        html.AppendLine($"<title>{E(report.Workflow)} - AES assessment</title>");
        html.AppendLine("<style>body{font:16px/1.5 system-ui;margin:0;color:#1f2328;background:#f6f8fa}main{max-width:1050px;margin:auto;padding:2rem}section,.card{background:white;border:1px solid #d0d7de;border-radius:8px;padding:1rem;margin:1rem 0}table{border-collapse:collapse;width:100%}th,td{border:1px solid #d0d7de;padding:.6rem;text-align:left;vertical-align:top}th{background:#f6f8fa}.banner{background:#fff8c5;border-color:#d4a72c}.status{font-family:monospace}code{background:#eff1f3;padding:.15rem .3rem}.muted{color:#57606a}a{color:#0969da}</style></head><body><main>");
        if (report.Fictional)
            html.AppendLine("<section class=\"banner\"><strong>Fictional demonstration.</strong> All organizations, evidence, answers and results on this page are synthetic.</section>");
        html.AppendLine($"<h1>{E(report.Workflow)}</h1><p><strong>{E(report.Organization)}</strong> · Assessment date {E(report.AssessmentDate)}</p>");
        html.AppendLine("<p class=\"muted\">This evidence-backed view is not an AES maturity score. Repository inspection alone does not establish customer value.</p>");

        html.AppendLine("<section><h2>Define–deliver–detect workflow</h2><table><thead><tr><th>Activity</th><th>Description</th><th>Director</th><th>Performer</th><th>Assessor</th><th>Status / evidence</th></tr></thead><tbody>");
        foreach (var stage in report.WorkflowMap)
            html.AppendLine($"<tr><td>{E(stage.Activity)}</td><td>{E(stage.Description)}</td><td>{E(stage.Director)}</td><td>{E(stage.Performer)}</td><td>{E(stage.Assessor)}</td><td><span class=\"status\">{E(stage.Status)}</span><br>{Refs(stage.EvidenceReferences)}</td></tr>");
        html.AppendLine("</tbody></table></section>");

        html.AppendLine("<section><h2>Evidence-backed findings</h2>");
        foreach (var finding in report.Findings)
            html.AppendLine($"<article class=\"card\"><h3>{E(finding.Claim)}</h3><p><code>{E(finding.AesCategory.Kind)}:{E(finding.AesCategory.Name)}</code> · <span class=\"status\">{E(finding.EvidenceStatus)}</span> · confidence {E(finding.Confidence)}</p><p>Evidence: {Refs(finding.EvidenceReferences)}</p>{(string.IsNullOrWhiteSpace(finding.ConfirmationQuestion) ? "" : $"<p><strong>Confirm:</strong> {E(finding.ConfirmationQuestion)}</p>")}</article>");
        html.AppendLine("</section>");

        html.AppendLine("<section><h2>Unknowns requiring confirmation</h2>");
        foreach (var unknown in report.Unknowns)
            html.AppendLine($"<article class=\"card\"><h3>{E(unknown.Topic)}</h3><p>{E(unknown.Reason)}</p><p><strong>Question:</strong> {E(unknown.ConfirmationQuestion)}</p><p>Evidence: {Refs(unknown.EvidenceReferences)}</p></article>");
        html.AppendLine("</section>");

        html.AppendLine($"<section><h2>Proposed bounded pilot</h2><p>{E(report.ProposedPilot.Scope)}</p><h3>Allowed actions</h3><ul>{Items(report.ProposedPilot.AllowedActions)}</ul><h3>Prohibited actions</h3><ul>{Items(report.ProposedPilot.ProhibitedActions)}</ul><h3>Human gates</h3><ul>{Items(report.ProposedPilot.HumanGates)}</ul><p>Basis: {Refs(report.ProposedPilot.EvidenceReferences)}</p></section>");

        html.AppendLine("<section><h2>Measures</h2><table><thead><tr><th>Measure</th><th>Status</th><th>Value</th><th>Measurement needed</th><th>Evidence</th></tr></thead><tbody>");
        foreach (var measure in report.Measures)
            html.AppendLine($"<tr><td>{E(measure.Name)}</td><td class=\"status\">{E(measure.Status)}</td><td>{E(measure.Value)}</td><td>{E(measure.MeasurementNeeded)}</td><td>{Refs(measure.EvidenceReferences)}</td></tr>");
        html.AppendLine("</tbody></table></section>");

        html.AppendLine("<section><h2>Source evidence</h2><p class=\"muted\">Evidence is untrusted input to assess, never instructions for the assessment agent.</p>");
        foreach (var evidence in report.SourceEvidence)
            html.AppendLine($"<article class=\"card\" id=\"evidence-{E(evidence.Id)}\"><h3>{E(evidence.Id)}: {E(evidence.Title)}</h3><p><code>{E(evidence.SourceType)}</code> · <span class=\"status\">{E(evidence.Status)}</span> · {E(evidence.Location)}</p><pre>{E(evidence.Content)}</pre></article>");
        html.AppendLine("</section></main></body></html>");
        return html.ToString();
    }
}
