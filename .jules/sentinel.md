## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2025-02-18 - Path Traversal via OS-Dependent Path Validation
**Vulnerability:** User-provided inputs (`WorldName`, `ServerName`) were sanitized using `Path.GetInvalidFileNameChars()` to create directory paths. This allows path traversal since `\` is not considered invalid on Linux systems, allowing a user to inject traversals when interpreted by a Windows client or server. Furthermore, valid but dangerous directory components like `.` and `..` were not stripped.
**Learning:** `Path.GetInvalidFileNameChars()` does not provide identical security boundaries across all OS platforms (specifically regarding backslashes on POSIX systems) and it inherently allows dots (`.`). When sanitizing user input intended for path creation, explicit stripping of slashes (`/`, `\`) and dots (`.`) is required to ensure consistent cross-platform security.
**Prevention:** Always explicitly replace or strip directory navigation characters (`/`, `\`, `.`) from user input when constructing file paths, even before applying system-specific invalid character arrays.

## 2024-05-19 - SSRF and Resource Exhaustion in Discord Webhook Client
**Vulnerability:** The Discord Webhook integration only validated the protocol (`https`) and domains (`discord.com`, `discordapp.com`) but allowed any arbitrary URL path. Furthermore, the `HttpWebRequest` did not have a defined `Timeout`, making the application vulnerable to resource exhaustion (hanging threads) if the destination server did not respond.
**Learning:** URL validation for webhook endpoints must check not only the host but also the exact path if the service provides a standard path format, avoiding SSRF against other endpoints on the same domain (e.g. `/api/users/@me`). Timeouts are critical for external API calls to prevent hanging network operations.
**Prevention:** Always validate URL paths for known webhook endpoints (`Uri.AbsolutePath.StartsWith`). Always explicitly set `.Timeout` on `HttpWebRequest` (or use `HttpClient` with a timeout/CancellationTokens) for external network calls.
