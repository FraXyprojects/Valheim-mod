## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2025-02-18 - Path Traversal via OS-Dependent Path Validation
**Vulnerability:** User-provided inputs (`WorldName`, `ServerName`) were sanitized using `Path.GetInvalidFileNameChars()` to create directory paths. This allows path traversal since `\` is not considered invalid on Linux systems, allowing a user to inject traversals when interpreted by a Windows client or server. Furthermore, valid but dangerous directory components like `.` and `..` were not stripped.
**Learning:** `Path.GetInvalidFileNameChars()` does not provide identical security boundaries across all OS platforms (specifically regarding backslashes on POSIX systems) and it inherently allows dots (`.`). When sanitizing user input intended for path creation, explicit stripping of slashes (`/`, `\`) and dots (`.`) is required to ensure consistent cross-platform security.
**Prevention:** Always explicitly replace or strip directory navigation characters (`/`, `\`, `.`) from user input when constructing file paths, even before applying system-specific invalid character arrays.
## 2024-10-27 - Missing HTTP Request Timeouts
**Vulnerability:** The `HttpWebRequest` used for sending messages to Discord webhooks lacked timeout configurations (`Timeout` and `ReadWriteTimeout`).
**Learning:** If an external API endpoint hangs indefinitely without closing the connection, threads can become blocked, potentially leading to thread starvation and a Denial of Service (DoS) vulnerability.
**Prevention:** Always configure explicit timeouts when making external HTTP requests, especially in background tasks, to ensure resources are released promptly if the endpoint fails to respond.
