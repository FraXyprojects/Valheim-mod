## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2023-10-27 - [Path Traversal bypass on non-Windows platforms]
**Vulnerability:** `SanitizeFileName` relied solely on `Path.GetInvalidFileNameChars()`, which on Linux/Mono environments does not include directory separators like `\`. This allowed path traversal sequences like `..\` to bypass sanitization. Furthermore, exact matches for `.` and `..` were not handled.
**Learning:** `Path.GetInvalidFileNameChars()` is platform-dependent and insufficient on its own for robust filename sanitization, especially when processing user-controlled inputs that might span platforms (e.g., cross-platform game clients/servers).
**Prevention:** Always explicitly check for and sanitize directory traversal characters (`..`) and all possible directory separators (`/`, `\`), regardless of the runtime's operating system, before relying on framework-specific `GetInvalidFileNameChars()` checks. Ensure that edge cases like isolated `.` or `..` are also rejected or transformed safely.
