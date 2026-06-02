# ActionsShowcase

A small ASP.NET Core 10 Web API used to demonstrate a full local + CI workflow: build, unit tests with coverage and an enforced threshold, BDD service tests in containers, container image build, vulnerability scanning, secret scanning, and Git-side guard rails via hooks.

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

You don't need to do anything manually — `Directory.Build.props` at the repo root runs `git config core.hooksPath .githooks` on the first build and writes a marker at `.git/.hooks-configured`. Subsequent builds skip the step. This matters because a `dotnet build` triggered from inside a `git push` runs while `.git/config` is locked; without the marker, the inner `git config` would warn (and on Visual Studio that warning bubbles up as a failed push).

Manual one-liners:

```
git config core.hooksPath .githooks      # opt in
git config --unset core.hooksPath        # opt out
```

### What each hook does

| Hook | Trigger | Behavior |
|---|---|---|
| `.githooks/pre-commit` | `git commit` | Runs `dotnet format ActionsShowcase.slnx --verify-no-changes`. Aborts the commit if anything would reformat. Fix with `dotnet format ActionsShowcase.slnx`, re-stage, retry. |
| `.githooks/pre-push` | `git push` | Runs `dotnet build -c Release` and the unit tests (`--no-build`). Service tests are intentionally excluded so push isn't slow or Docker-dependent. |
| `.githooks/prepare-commit-msg` | Every commit message is prepared | Prepends `[branch-name] ` to the commit message. Skips merge/squash/amend commits and the long-lived branches `main`/`master`/`develop`/`development`. Idempotent — won't double-prefix. |

Both `pre-commit` and `pre-push` detect their shell — **Git Bash** (paths under `/c/...`) or **WSL bash** (paths under `/mnt/c/...`) — via `/proc/version` and probe `dotnet.exe` in both `Program Files\dotnet` and `Program Files (x86)\dotnet`, falling back to a `dotnet`/`dotnet.exe` PATH lookup. This is needed because `#!/usr/bin/env bash` may resolve to WSL bash when invoked from Visual Studio, while a CLI/PowerShell push uses Git for Windows' mingw bash.

### Bypassing a hook

```
git commit --no-verify   # skips pre-commit + commit-msg + prepare-commit-msg
git push   --no-verify   # skips pre-push
```

### Line endings and cross-platform

`.gitattributes` pins `.githooks/*` and `*.sh` to LF. This is required on Windows because bash silently fails to invoke a hook whose shebang line ends in CRLF — it tries to exec a binary literally named `bash\r`.

For macOS/Linux contributors, set the executable bit in the index so the hooks are runnable there too:

```
git update-index --chmod=+x .githooks/pre-commit .githooks/pre-push .githooks/prepare-commit-msg
```

### Known limitation: `prepare-commit-msg` in Visual Studio

VS's commit UI reads the message from its own text box, not from `.git/COMMIT_EDITMSG`, so the branch-name prefix is only applied to commits made from the CLI. Commit from the terminal when you want the prefix.

---

## CI pipeline (`.github/workflows/ci.yml`)

### Triggers

```yaml
on:
  push:
    branches: [main, development]
  pull_request:
    branches: [main, development]
  workflow_dispatch:

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

Every PR targeting `main` or `development` runs the build + tests + coverage gate. The concurrency block cancels in-progress runs when new commits land on the same ref (e.g. a PR force-push) so older runs don't waste minutes.

### Job graph

```
        PR / push to development            push to main
              │                                  │
              ▼                                  ▼
        ┌───────────┐                      ┌───────────┐    ┌────────────┐
        │   test    │                      │   test    │    │ trufflehog │
        └───────────┘                      └─────┬─────┘    └────────────┘
        ┌───────────┐                            ▼
        │trufflehog │                     ┌──────────────┐
        └───────────┘                     │ docker-build │
                                          └──────┬───────┘
                                                 ▼
                                          ┌──────────────┐
                                          │    trivy     │
                                          └──────────────┘
```

`test` and `trufflehog` run on every PR and every push to `main`/`development`. `docker-build` and `trivy` are gated to pushes on `main` only.

### Permissions

```yaml
permissions:
  contents: read
  security-events: write   # Trivy SARIF upload to GitHub Security tab
  actions: read
```

### Job: `test` — build, tests, and 80 % coverage gate

1. `actions/checkout@v6`, then `actions/setup-dotnet@v5` pinned to `10.0.x`.
2. `dotnet restore ActionsShowcase.slnx` then `dotnet build --no-restore -c Release`.
3. **Unit tests with coverage**: `dotnet test ... --collect:"XPlat Code Coverage" --settings ActionsShowcase.Tests.Unit/coverlet.runsettings`. The runsettings file excludes generated code so coverage reflects user code only.
4. **Service tests**: `dotnet test` against the Tests.Service project. Inside, the test fixture spins up the API via `dotnet publish -t:PublishContainer` and Testcontainers (the hosted Ubuntu runner has Docker preinstalled).
5. **Coverage summary + 80 % gate**: `irongut/CodeCoverageSummary@v1.3.0` converts the Cobertura XML into a markdown badge + table appended to `$GITHUB_STEP_SUMMARY`. With `thresholds: '80 80'` and `fail_below_min: true`, the action exits non-zero when line coverage drops below 80 %, failing the job.
6. **Artifacts** (`if: always()` so they survive a test or coverage failure):
    - `code-coverage` — raw `coverage.cobertura.xml` + the rendered markdown.
    - `test-results` — `*.trx` files from both test projects.

### Job: `docker-build` — image via .NET SDK

Runs only on push to `main`, after `test` passes.

```
dotnet publish ActionsShowcase/ActionsShowcase.csproj \
    --configuration Release \
    -t:PublishContainer \
    -p:ContainerRepository=actionsshowcase \
    -p:ContainerImageTag=${{ github.sha }}
```

The SDK builds an OCI image without a Dockerfile (driven by `<ContainerBaseImage>` and `<ContainerPort>` in `ActionsShowcase.csproj`). The image is exported with `docker save` to `image.tar` and uploaded as the `container-image` artifact so `trivy` can load it without rebuilding.

### Job: `trivy` — image vulnerability scan

Runs only on push to `main`, after `docker-build`.

1. Downloads the `container-image` artifact and `docker load`s it.
2. `aquasecurity/trivy-action@v0.36.0` scans the image at `CRITICAL,HIGH,MEDIUM`, `--ignore-unfixed`, writing SARIF to `trivy-results.sarif`. `exit-code: '0'` so findings don't fail the job — flip to `'1'` to gate.
3. SARIF is uploaded to the Security tab via `github/codeql-action/upload-sarif@v4` (`continue-on-error: true` so the pipeline still passes when Code Scanning isn't enabled — private repo without Advanced Security, or simply not yet enabled).
4. A second Trivy run uses `format: template` with `.github/trivy-markdown.tpl` to render `trivy-report.md` — one `###` section per scan target, a real markdown table per section (Severity / CVE link / Package / Installed / Fixed in / Title), with pipes in titles escaped so a noisy field can't break the layout.
5. `trivy-report.md` is uploaded as the `trivy-report` artifact and `cat`'d straight into the job summary (no code fence — it's already markdown). If no table rows are present, a "No vulnerabilities found" note is written instead.

To make Security-tab findings appear: repo **Security → Code scanning → Set up**. Free on public repos; private repos need GitHub Advanced Security.

### Job: `trufflehog` — secret scan

Runs on every PR and push to `main`/`development`, in parallel with everything else (no `needs:`).

1. `actions/checkout@v6` with `fetch-depth: 0` so TruffleHog can walk git history.
2. `trufflesecurity/trufflehog@main` runs with `--results=verified,unknown`. The action exits non-zero on findings, failing the job.
3. A follow-up summary step (`if: always()`) writes a one-line status to the job summary so it shows up on the Actions run page even when the job is red.

### Required PR status check

The 80 % coverage gate and build/test failures only **block** merges if you require the matching status check on the protected branches. One time, per protected branch:

1. **Settings → Branches → Branch protection rules → Add rule** (or **Rulesets** in the newer UI).
2. Pattern: `main` (repeat for `development`).
3. Tick **Require status checks to pass before merging**.
4. Search for and add the check named **`Build & test (unit + service)`** (the `name:` of the `test` job).
5. Optionally tick **Require branches to be up to date before merging**.

After this, a PR with a failing build, failing test, or coverage < 80 % can't be merged.

### Where outputs land

| Output | Location |
|---|---|
| Coverage summary, Trivy markdown report, TruffleHog status | Run page **Summary** tab (`$GITHUB_STEP_SUMMARY`) |
| `coverage.cobertura.xml`, `*.trx`, `image.tar`, `trivy-results.sarif`, `trivy-report.md` | Run page **Artifacts** section |
| Trivy SARIF findings | Repo **Security → Code scanning alerts** (if Code Scanning is enabled) |
| TruffleHog findings detail | Step log of the `trufflehog` job |

### Tweaking knobs

- **Gate merges on Trivy findings**: set `exit-code: '1'` on the first Trivy step.
- **Trim severity**: change `severity: CRITICAL,HIGH,MEDIUM` on both Trivy steps.
- **Change the coverage threshold**: edit `thresholds:` on the coverage step (the lower number is the failure threshold; set both equal to fail at that value).
- **Change .NET SDK version**: edit `DOTNET_VERSION` in the `env:` block.
- **Disable a hook locally**: pass `--no-verify` or `git config --unset core.hooksPath`.
