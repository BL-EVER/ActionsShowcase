# ActionsShowcase

A small ASP.NET Core 10 Web API used to demonstrate a full local + CI workflow: build, unit tests with coverage, BDD service tests in containers, container image build, vulnerability scanning, secret scanning, and Git-side guard rails via hooks.

## Solution layout

| Project | Purpose |
|---|---|
| `ActionsShowcase/` | Web API (`/Secret`, `/Random`), bound to `appsettings.json` via `IOptionsMonitor`. Scalar UI at `/scalar/v1`. |
| `ActionsShowcase.Tests.Unit/` | xUnit unit + `WebApplicationFactory` integration tests. 100% line/branch coverage on user code. |
| `ActionsShowcase.Tests.Service/` | Reqnroll BDD scenarios driven against a Testcontainers-hosted instance of the API. |

```
dotnet restore ActionsShowcase.slnx
dotnet build   ActionsShowcase.slnx
dotnet run     --project ActionsShowcase
```

Then open `https://localhost:7119/scalar/v1` for the API explorer.

---

## Running the tests

```
# Unit tests (with coverage, excluding generated sources)
dotnet test ActionsShowcase.Tests.Unit/ActionsShowcase.Tests.Unit.csproj \
    --collect:"XPlat Code Coverage" \
    --settings ActionsShowcase.Tests.Unit/coverlet.runsettings

# Service tests (require Docker)
dotnet test ActionsShowcase.Tests.Service/ActionsShowcase.Tests.Service.csproj
```

The service tests publish the API as a container image via `dotnet publish -t:PublishContainer`, start it with Testcontainers, and drive it over HTTP using Reqnroll feature files in `ActionsShowcase.Tests.Service/Features/`.

---

## Git hooks

Hooks live under `.githooks/` in source control rather than inside `.git/hooks/` (which is not tracked). They are activated by pointing Git at that directory with `core.hooksPath`.

### Activation

You don't need to do anything manually — `Directory.Build.props` at the repo root runs `git config core.hooksPath .githooks` before every build, gated on both `.git` and `.githooks` existing. So after the first `dotnet build` on a fresh clone, hooks are live.

To opt in manually without building:

```
git config core.hooksPath .githooks
```

To opt out:

```
git config --unset core.hooksPath
```

### What each hook does

| Hook | Trigger | Behavior |
|---|---|---|
| `.githooks/pre-commit` | `git commit` | Runs `dotnet format ActionsShowcase.slnx --verify-no-changes`. Aborts the commit if anything would reformat. Fix with `dotnet format ActionsShowcase.slnx`, re-stage, retry. |
| `.githooks/pre-push` | `git push` | Runs `dotnet build -c Release` and the unit tests (`--no-build`). Service tests are intentionally excluded so push isn't slow or Docker-dependent. |
| `.githooks/prepare-commit-msg` | Every commit message is prepared | Prepends `[branch-name] ` to the commit message. Skips merge/squash/amend commits and the long-lived branches `main`/`master`/`develop`/`development`. Idempotent — won't double-prefix. |

### Bypassing a hook

```
git commit --no-verify   # skips pre-commit + commit-msg + prepare-commit-msg
git push   --no-verify   # skips pre-push
```

### Cross-platform contributors

On Windows, Git runs hooks via the bundled bash, no executable bit needed. For macOS/Linux contributors, set the executable bit in the index so the hooks are runnable everywhere:

```
git update-index --chmod=+x .githooks/pre-commit .githooks/pre-push .githooks/prepare-commit-msg
```

---

## CI pipeline (`.github/workflows/ci.yml`)

### Trigger

```yaml
on:
  push:
    branches: [main, development]
  workflow_dispatch:   # manual run from the Actions tab
```

### Job graph

```
        push to development                         push to main
              │                                          │
              ▼                                          ▼
        ┌───────────┐                             ┌───────────┐    ┌────────────┐
        │   test    │                             │   test    │    │ trufflehog │
        └───────────┘                             └─────┬─────┘    └────────────┘
        ┌───────────┐                                   ▼
        │trufflehog │                            ┌──────────────┐
        └───────────┘                            │ docker-build │
                                                 └──────┬───────┘
                                                        ▼
                                                 ┌──────────────┐
                                                 │    trivy     │
                                                 └──────────────┘
```

`test` and `trufflehog` run on both branches. `docker-build` and `trivy` run on `main` only.

### Permissions

```yaml
permissions:
  contents: read           # actions/checkout
  security-events: write   # Trivy SARIF upload to the Security tab
  actions: read
```

### Job: `test` — build & test (unit + service)

1. `actions/checkout@v4`, then `actions/setup-dotnet@v4` pinned to `10.0.x`.
2. `dotnet restore ActionsShowcase.slnx` then `dotnet build --no-restore`.
3. **Unit tests with coverage**: `dotnet test ... --collect:"XPlat Code Coverage" --settings ActionsShowcase.Tests.Unit/coverlet.runsettings`. The runsettings file excludes generated code so coverage reflects only user code.
4. **Service tests**: `dotnet test` against the Tests.Service project. Inside, the test fixture spins up the API via `dotnet publish -t:PublishContainer` and Testcontainers. The hosted Ubuntu runner has Docker preinstalled.
5. **Coverage summary**: `irongut/CodeCoverageSummary@v1.3.0` converts the Cobertura XML into a markdown table that is appended to `$GITHUB_STEP_SUMMARY` (shown on the run's summary page). Thresholds `60 80` colour the badge red/yellow/green.
6. **Artifacts** uploaded with `if: always()` so they survive a test failure:
    - `code-coverage` — raw `coverage.cobertura.xml` + the rendered markdown.
    - `test-results` — `*.trx` files from both test projects.

### Job: `docker-build` — image via .NET SDK

Runs on `main` only, after `test` passes.

```
dotnet publish ActionsShowcase/ActionsShowcase.csproj \
    --configuration Release \
    -t:PublishContainer \
    -p:ContainerRepository=actionsshowcase \
    -p:ContainerImageTag=${{ github.sha }}
```

The SDK builds an OCI image without a Dockerfile (driven by the `<ContainerBaseImage>` and `<ContainerPort>` settings in `ActionsShowcase.csproj`). The image is exported with `docker save` to `image.tar` and uploaded as the `container-image` artifact so the next job can load it without rebuilding.

### Job: `trivy` — image vulnerability scan

Runs on `main` only, after `docker-build`.

1. Downloads the `container-image` artifact and `docker load`s it.
2. `aquasecurity/trivy-action@v0.36.0` scans the image at `CRITICAL,HIGH,MEDIUM`, `--ignore-unfixed`, writing SARIF to `trivy-results.sarif`. `exit-code: '0'` means findings don't fail the job — change to `'1'` to gate merges.
3. SARIF is uploaded to the GitHub Security tab via `github/codeql-action/upload-sarif@v3`. This step has `continue-on-error: true` so the pipeline still passes when Code Scanning is disabled (private repo without Advanced Security, or simply not enabled yet).
4. A second Trivy run produces a plain-text table that is appended to the job summary so findings are visible without leaving the Actions tab. The SARIF is also uploaded as the `trivy-results` artifact.

To see findings in the Security tab: repo **Security → Code scanning → Set up**. Free on public repos; private repos require GitHub Advanced Security.

### Job: `trufflehog` — secret scan

Runs on both `main` and `development`, parallel to everything else (no `needs:`).

1. `actions/checkout@v4` with `fetch-depth: 0` so TruffleHog can walk git history.
2. `trufflesecurity/trufflehog@main` runs with `--results=verified,unknown`. The action exits non-zero when it surfaces a finding, which fails the job.
3. The follow-up summary step (with `if: always()`) writes a one-line status (clean vs. findings present) into the job summary so it shows up on the Actions run page even when the job is red.

### Where outputs land

| Output | Location |
|---|---|
| Coverage summary, Trivy table, TruffleHog status | Run page **Summary** tab (`$GITHUB_STEP_SUMMARY`) |
| `coverage.cobertura.xml`, `*.trx`, `image.tar`, `trivy-results.sarif` | Run page **Artifacts** section |
| Trivy SARIF findings | Repo **Security → Code scanning alerts** (if Code Scanning is enabled) |
| TruffleHog findings detail | Step log of the `trufflehog` job |

### Tweaking knobs

- **Gate merges on Trivy findings**: set `exit-code: '1'` on the `Run Trivy` step.
- **Trim severity**: change `severity: CRITICAL,HIGH,MEDIUM` on both Trivy steps.
- **Run service tests on PRs**: add `pull_request:` to the `on:` block.
- **Change .NET SDK version**: edit `DOTNET_VERSION` in the `env:` block.
- **Disable a hook locally**: `git config core.hooksPath ''` or pass `--no-verify`.
