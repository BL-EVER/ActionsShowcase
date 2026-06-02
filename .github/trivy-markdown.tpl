## Trivy vulnerability scan
{{- range . }}
{{- if .Vulnerabilities }}

### `{{ .Target }}` ({{ .Type }})

| Severity | CVE | Package | Installed | Fixed in | Title |
| --- | --- | --- | --- | --- | --- |
{{- range .Vulnerabilities }}
| {{ .Severity }} | [{{ .VulnerabilityID }}]({{ .PrimaryURL }}) | `{{ .PkgName }}` | `{{ .InstalledVersion }}` | {{ if .FixedVersion }}`{{ .FixedVersion }}`{{ else }}—{{ end }} | {{ .Title | replace "|" "\\|" | replace "\n" " " }} |
{{- end }}
{{- end }}
{{- end }}
