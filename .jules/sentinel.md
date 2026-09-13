## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2025-02-18 - Path Traversal via OS-Dependent Path Validation
**Vulnerability:** User-provided inputs (`WorldName`, `ServerName`) were sanitized using `Path.GetInvalidFileNameChars()` to create directory paths. This allows path traversal since `\` is not considered invalid on Linux systems, allowing a user to inject traversals when interpreted by a Windows client or server. Furthermore, valid but dangerous directory components like `.` and `..` were not stripped.
**Learning:** `Path.GetInvalidFileNameChars()` does not provide identical security boundaries across all OS platforms (specifically regarding backslashes on POSIX systems) and it inherently allows dots (`.`). When sanitizing user input intended for path creation, explicit stripping of slashes (`/`, `\`) and dots (`.`) is required to ensure consistent cross-platform security.
**Prevention:** Always explicitly replace or strip directory navigation characters (`/`, `\`, `.`) from user input when constructing file paths, even before applying system-specific invalid character arrays.

## 2025-02-23 - SSRF Bypass via HTTP Redirects and DoS via Missing Timeouts
**Vulnerability:** Even though the Discord webhook URL was validated for scheme and host, `HttpWebRequest` follows redirects by default (`AllowAutoRedirect = true`). This could allow an SSRF bypass if the allowed domain has an open redirect. Additionally, missing `Timeout` and `ReadWriteTimeout` could lead to thread exhaustion (DoS) if the endpoint hangs.
**Learning:** URL validation is not enough if the HTTP client automatically follows redirects to unvalidated destinations. Unbounded network calls can hang the application or thread pool.
**Prevention:** Disable `AllowAutoRedirect` on `HttpWebRequest` for webhooks and always set explicit, reasonable timeouts (`Timeout`, `ReadWriteTimeout`).
