# Agentic Engineering System assessment MVP

This repository contains a local-first vertical slice for assessing **one engineering workflow** using GitHub's [Agentic Engineering System](https://github.com/resources/insights/agentic-engineering-system) as the conceptual source. It keeps three concepts distinct:

- **Stocks:** governance, shared knowledge and customer value.
- **Activities:** define, deliver and detect.
- **Participation modes:** director, performer and assessor.

The output is an evidence-backed discussion aid, **not an AES maturity score**. Repository inspection can show artifacts and controls; it cannot by itself establish customer value.

## What is included

- A .NET 10 CLI and reusable core library.
- AgentRC as the primary repository-readiness collector, plus a narrow supplemental collector for workflow evidence AgentRC does not emit.
- Versioned JSON Schemas for normalized input and assessment output.
- A fictional FableCart Retail example concerning a false store-pickup availability report.
- A GitHub Agentic Workflow that proposes assessment changes through a pull request.
- A conventional GitHub Actions workflow that validates, renders and optionally deploys an approved report to GitHub Pages.
- Tests for structural/domain validation, evidence provenance, deterministic collection and rendering unknown or unmeasured values.

Everything under `fixtures/fablecart` and `assessments/fablecart` is synthetic and clearly fictional. It contains no real client, system, credential or customer data.

## Run locally

Prerequisite: the SDK selected by `global.json` (currently .NET 10.0.300 or a compatible latest patch).

```powershell
dotnet test AesAssessment.slnx

dotnet run --project src\AesAssessment.Cli -- `
  collect fixtures\fablecart assessments\fablecart\input.json

# Optional live collection: invokes AgentRC through npx, then adds only
# workflow-specific supplemental evidence and questionnaire answers.
dotnet run --project src\AesAssessment.Cli -- `
  collect-live fixtures\fablecart . assessments\fablecart\input-live.json

dotnet run --project src\AesAssessment.Cli -- `
  validate-input assessments\fablecart\input.json

dotnet run --project src\AesAssessment.Cli -- `
  validate-report assessments\fablecart\assessment.json

dotnet run --project src\AesAssessment.Cli -- `
  render assessments\fablecart\assessment.json artifacts\site
```

Open `artifacts/site/index.html`. The deterministic fixture collection, validation and rendering commands do not invoke an AI model, call GitHub or publish anything.

The deterministic `collect` command imports the checked-in fictional AgentRC `CommandResult` fixture and requires no credentials. `collect-live` requires Node.js 22+ and network access for `npx github:microsoft/agentrc`; AgentRC readiness itself does not invoke a model.

## Why AgentRC is integrated, not duplicated

[AgentRC](https://github.com/microsoft/agentrc) 2.1.0 is an MIT-licensed, experimental tool for repository AI readiness. Its `readiness --json` command reports a `CommandResult` envelope containing repository/area detection, nine readiness pillars, criteria, evidence paths, reasons, extras and a five-level **AgentRC-specific** maturity model. This project preserves that raw envelope as the observed `agentrc-readiness` evidence item.

The evaluated revision was `microsoft/agentrc` main at `daa92160534031186327e3d8b25b11beeda7c021`. The documented invocation is:

```powershell
npx -y github:microsoft/agentrc readiness <repository> --json --quiet --per-area
```

AgentRC currently fetches from a moving GitHub branch in its documented `npx` form, so production adoption should mirror or package an approved revision and update it deliberately. An attempted commit-pinned `npx github:microsoft/agentrc#<sha>` run failed with an npm GitFetcher packaging error in this environment.

AgentRC and AES answer different questions:

| AgentRC supplies | Supplemental collector still supplies |
|---|---|
| Repository files/configuration, detected areas, build/test/docs/security/AI-tooling readiness criteria and evidence paths | Workflow-specific issue reports, semantic policy/test excerpts, questionnaire answers, decision owners, allowed agent actions, escalation, workflow boundary, verification meaning and post-release signals |

The supplemental collector must not recreate AgentRC's readiness checks. It adds content only when the AgentRC readiness envelope cannot express the workflow fact needed by an AES finding.

AgentRC's score is **never mapped to an AES maturity score**. During evaluation it detected this .NET solution as a monorepo but reported missing build scripts, test scripts and type-check configuration despite working `.csproj` files and passing `dotnet test`. Its current criteria are partly JavaScript-oriented, so every criterion is evidence to review rather than ground truth. It also cannot establish customer value, ownership, permissions or actual team behavior from repository inspection.

## Data contract and evidence status

Schemas are under `schemas/v1`. Both documents use semantic schema version `1.0.0`. Every finding includes a claim, one AES category, evidence references, an evidence status, confidence and an optional confirmation question.

| Status | Meaning |
|---|---|
| `observed` | Directly present in the collected artifact. |
| `team_reported` | Supplied by a human answer and not independently verified. |
| `inferred` | A bounded interpretation of cited evidence. |
| `unknown` | Unsupported; a confirmation question is required. |

The domain validator additionally checks that evidence references resolve, the workflow map contains define/deliver/detect, AES category names match their kind, and unknown findings ask for confirmation. The checked-in example is an intentionally human-reviewable draft: it does not invent permissions, metrics or customer outcomes.

## Assessment flow

1. Complete `QUESTIONNAIRE.md` or provide equivalent structured answers.
2. Collect repository and issue fixtures into normalized input with the CLI.
3. Run the Agentic Workflow to draft the define–deliver–detect map, participation modes, findings, unknowns and a bounded pilot.
4. Review the proposed JSON in its pull request. Verify every factual claim against its evidence links and resolve or preserve unknowns.
5. Merge only after human approval. There is no automatic merge.
6. The Pages workflow validates and renders only the merged report on `main`. Publication remains opt-in.

Issue bodies, PR text, repository files and questionnaire answers are **untrusted input**. The assessment agent treats them as evidence, never as instructions. Agent output is constrained to a pull request and cannot deploy the site directly.

## Agentic Workflow setup

The editable source is `.github/workflows/aes-assessment.md`; the generated workflow is `.github/workflows/aes-assessment.lock.yml`. The lock file was generated with `gh-aw` 0.88.7 in strict mode. Keep both files under review and never hand-edit the executable lock file.

Install and validate the compiler:

```powershell
gh extension install github/gh-aw
gh aw validate aes-assessment --strict --dir .
gh aw compile aes-assessment --strict --approve --no-check-update
```

For a Git checkout, also run:

```powershell
gh aw compile aes-assessment --strict --actionlint --approve --no-check-update
```

The dedicated strict validator succeeded in this workspace, and compilation generated the checked-in lock file. The additional compile-time `actionlint` invocation could not complete because this provided folder is not a Git repository; it reported only `not in a git repository`. Run that command after placing the files in their target Git repository.

The compiled workflow separates the read-only agent job, threat detection and the permission-scoped `create-pull-request` safe-output job. Its policy permits one draft PR changing only `assessments/fablecart/assessment.json`, disables fallback issue creation and provides no merge or Pages capability.

The workflow requires read access to repository content and the ability to create a branch/pull request through its declared safe output. It does **not** receive deployment, merge or repository-administration permission. Configure branch protection so the report PR requires human approval.

If the compiler available to your environment produces a changed lock file, review and commit that generated result rather than hand-editing it.

## Opt-in GitHub Pages publication

Publication is disabled unless the repository variable `PUBLISH_AES_REPORT` equals `true`.

1. Review and merge the assessment PR to protected `main`.
2. In repository **Settings → Pages**, select **GitHub Actions** as the source.
3. Add repository variable `PUBLISH_AES_REPORT=true`.
4. Merge a human-approved pull request that changes `assessments/fablecart/assessment.json`.

The deploy job alone receives `pages: write` and `id-token: write`. The agentic workflow has no Pages permission. A private source repository does **not** by itself make the resulting Pages site private; confirm the repository/organization Pages visibility and plan before enabling publication. Do not publish confidential assessment evidence to a public site.

The Pages workflow listens only for a closed pull request into `main`, verifies that it was merged and that the assessment JSON changed, then checks out the merged base branch. It has no push or manual trigger. Protect `main` and require report review so the merge is a meaningful human publication gate.

## Adding real or cross-repository evidence

Implement another `IEvidenceCollector` only for evidence unavailable from AgentRC and map it into `AssessmentInput`; keep collection separate from assessment reasoning. Start with metadata/content read scopes only, record canonical URLs and immutable identifiers, and preserve source status.

The default `GITHUB_TOKEN` is scoped to the workflow repository and must not be assumed to read arbitrary repositories. For explicitly approved cross-repository collection, use a GitHub App installation or fine-grained token limited to named repositories and read-only permissions. Store credentials in Actions secrets, never fixtures or report JSON. Document the access owner and remove access after the assessment.

## Limits

- One workflow and one demonstration repository only.
- AgentRC covers local repository readiness; the supplemental collector reads local fixture files and no GitHub API adapter is implemented.
- Automated inspection cannot establish customer value, decision accountability or team practice without human confirmation.
- Review minutes, agent token costs, defect rates and post-release outcomes remain `not_measured` until a measurement definition and instrumentation exist.
- The renderer is intentionally static and minimal so the same schemas and core logic can support a later Blazor interface.

## Next smallest real-workflow step

Choose one non-sensitive customer workflow and complete the questionnaire with
its accountable owners. Run `collect-live` against one explicitly approved
repository, replace the fictional supplemental fixtures with redacted issue or
policy exports, and review the normalized input before enabling the agentic
workflow. Do not add cross-repository credentials until the first repository's
evidence gaps are known.
