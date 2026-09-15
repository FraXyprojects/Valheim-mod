## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2025-02-18 - Path Traversal via OS-Dependent Path Validation
**Vulnerability:** User-provided inputs (`WorldName`, `ServerName`) were sanitized using `Path.GetInvalidFileNameChars()` to create directory paths. This allows path traversal since `\` is not considered invalid on Linux systems, allowing a user to inject traversals when interpreted by a Windows client or server. Furthermore, valid but dangerous directory components like `.` and `..` were not stripped.
**Learning:** `Path.GetInvalidFileNameChars()` does not provide identical security boundaries across all OS platforms (specifically regarding backslashes on POSIX systems) and it inherently allows dots (`.`). When sanitizing user input intended for path creation, explicit stripping of slashes (`/`, `\`) and dots (`.`) is required to ensure consistent cross-platform security.
**Prevention:** Always explicitly replace or strip directory navigation characters (`/`, `\`, `.`) from user input when constructing file paths, even before applying system-specific invalid character arrays.

## 2024-05-15 - API Call Timeout & SSRF Prevention
**Vulnerability:** External webhook calls using `HttpWebRequest` did not have an explicit timeout and allowed auto-redirects, which could lead to thread hanging (DoS) and potential SSRF via open redirects on the target domain.
**Learning:** Default settings for `HttpWebRequest` include a 100-second timeout and `AllowAutoRedirect = true`. These should always be hardened for external API calls, especially when the URL is user-configurable, even if domain-restricted.
**Prevention:** Explicitly set `.Timeout` to a reasonable threshold (e.g., 10 seconds) and set `.AllowAutoRedirect = false` on outgoing web requests.
