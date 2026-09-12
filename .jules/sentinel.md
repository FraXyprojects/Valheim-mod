## 2026-09-09 - [Server-Side Request Forgery (SSRF) in Discord Webhook]
**Vulnerability:** The Discord webhook URL provided in the configuration was used directly in `HttpWebRequest` without any validation.
**Learning:** Any user-configurable URL that the server/client sends requests to must be validated to prevent SSRF or unauthorized data exfiltration to arbitrary endpoints.
**Prevention:** Always validate configurable webhook URLs by parsing them as `Uri`, checking the scheme (HTTPS), and enforcing allowed hosts (e.g., `discord.com`, `discordapp.com`).

## 2024-10-25 - [Discord Webhook Markdown Injection and Mention Abuse]
**Vulnerability:** User-controlled text in the Discord webhook report could contain Markdown backticks (```) that break the formatting block, allowing attackers to mention `@everyone` or `@here`.
**Learning:** Whenever injecting user-controlled data into a Discord webhook text payload, you must sanitize Markdown structural characters (like backticks) and explicitly disable unwanted mentions via the `allowed_mentions` JSON property.
**Prevention:** Sanitize backticks in the input strings by replacing them (e.g., with single quotes) and include `"allowed_mentions": { "parse": [] }` in the webhook payload.
