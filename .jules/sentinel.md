## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).
