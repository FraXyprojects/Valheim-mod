## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2025-02-18 - Path Traversal via OS-Dependent Path Validation
**Vulnerability:** User-provided inputs (`WorldName`, `ServerName`) were sanitized using `Path.GetInvalidFileNameChars()` to create directory paths. This allows path traversal since `\` is not considered invalid on Linux systems, allowing a user to inject traversals when interpreted by a Windows client or server. Furthermore, valid but dangerous directory components like `.` and `..` were not stripped.
**Learning:** `Path.GetInvalidFileNameChars()` does not provide identical security boundaries across all OS platforms (specifically regarding backslashes on POSIX systems) and it inherently allows dots (`.`). When sanitizing user input intended for path creation, explicit stripping of slashes (`/`, `\`) and dots (`.`) is required to ensure consistent cross-platform security.
**Prevention:** Always explicitly replace or strip directory navigation characters (`/`, `\`, `.`) from user input when constructing file paths, even before applying system-specific invalid character arrays.

## 2023-11-20 - [Denial of Service via Infinite Timeout / SSRF via Redirects]
**Vulnerability:** The HTTP request to the Discord webhook had no explicit timeout and allowed automatic redirects. This could lead to a Denial of Service if the endpoint hangs, or Server-Side Request Forgery if the endpoint responds with an HTTP redirect to an internal or unexpected service.
**Learning:** Default behavior of `HttpWebRequest` in .NET often lacks strong security baselines (e.g., unlimited timeout by default for some operations, or 100 seconds which is very high; following redirects by default). Relying on the host checking validation alone is insufficient if the client automatically follows a redirect to a malicious or internal resource.
**Prevention:** Always explicitly set `request.Timeout` to a reasonable bound (e.g., 10000ms). Set `request.AllowAutoRedirect = false` when webhooks or API requests do not strictly require following redirects. Ensure the target port is validated as part of the scheme check.
