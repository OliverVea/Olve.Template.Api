{{/*
The bundled Dex runs when auth.mode is dex, unless dex.enabled is false (a Dex elsewhere).
*/}}
{{- define "app.dexEnabled" -}}
{{- $auth := .Values.auth | default dict -}}
{{- and (eq ($auth.mode | default "") "dex") (ne (toString .Values.dex.enabled) "false") -}}
{{- end -}}

{{/*
The server's settings: .Values.config, with .Values.auth (when set) mapped onto Auth__*. As YAML, for fromYaml.
  mode: none                       # Auth__Disable=true: every endpoint is anonymous
  mode: dex, issuer, clientId?, frontendClientId?, allow: { subjects, users, orgs, emails }
  mode: authentik, authority, audience, frontendAuthority?, frontendAudience?
*/}}
{{- define "app.config" -}}
{{- $c := deepCopy (.Values.config | default dict) -}}
{{- with .Values.auth -}}
{{- $mode := .mode | default "" -}}
{{- if eq $mode "none" -}}
{{- $_ := set $c "Auth__Disable" "true" -}}
{{- else if eq $mode "dex" -}}
{{- if not .issuer -}}
{{- fail "auth.mode dex needs auth.issuer (https://<host>/dex)" -}}
{{- end -}}
{{- $clientId := .clientId | default $.Chart.Name -}}
{{- $_ := set $c "Auth__Authority" .issuer -}}
{{- $_ := set $c "Auth__Audience" $clientId -}}
{{- $_ := set $c "Auth__FrontendAuthority" .issuer -}}
{{- $_ := set $c "Auth__FrontendAudience" (.frontendClientId | default $clientId) -}}
{{- if hasPrefix "http://" .issuer -}}
{{- $_ := set $c "Auth__RequireHttpsMetadata" "false" -}}
{{- end -}}
{{- $allow := .allow | default dict -}}
{{- if not (or $allow.subjects $allow.users $allow.orgs $allow.emails) -}}
{{- fail "auth.mode dex needs auth.allow (subjects, users, orgs or emails): an upstream such as GitHub lets anyone sign in" -}}
{{- end -}}
{{- range $kind := list "subjects" "users" "orgs" "emails" -}}
{{- range $i, $v := (get $allow $kind | default list) -}}
{{- $_ := set $c (printf "Auth__Allow__%s__%d" (title $kind) $i) $v -}}
{{- end -}}
{{- end -}}
{{- else if eq $mode "authentik" -}}
{{- $_ := set $c "Auth__Authority" .authority -}}
{{- $_ := set $c "Auth__Audience" .audience -}}
{{- with .frontendAuthority }}{{ $_ := set $c "Auth__FrontendAuthority" . }}{{ end -}}
{{- with .frontendAudience }}{{ $_ := set $c "Auth__FrontendAudience" . }}{{ end -}}
{{- else if $mode -}}
{{- fail (printf "auth.mode must be none, dex or authentik, got %q" $mode) -}}
{{- end -}}
{{- end -}}
{{- if not (or (get $c "Auth__Authority") (get $c "Auth__SigningKey") (eq (toString (get $c "Auth__Disable")) "true")) -}}
{{- fail "auth is not configured: set auth (mode none, dex or authentik) or config.Auth__*" -}}
{{- end -}}
{{- toYaml $c -}}
{{- end -}}
