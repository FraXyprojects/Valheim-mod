using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.Discord
{
    public sealed class DiscordWebhookClient
    {
        private const int DiscordMessageLimit = 1900;

        public void SendPlainTextAsync(string webhookUrl, string reportText)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(reportText))
            {
                return;
            }

            // Security: Validate the webhook URL to prevent SSRF
            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out Uri uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !(uri.Host.Equals("discord.com", StringComparison.OrdinalIgnoreCase) ||
                  uri.Host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase)))
            {
                ChronicleLogger.Warning("Invalid Discord webhook URL. URL must use HTTPS and point to discord.com or discordapp.com.");
                return;
            }

            Task.Run(() => Send(webhookUrl, reportText));
        }

        private static void Send(string webhookUrl, string reportText)
        {
            try
            {
                string content = reportText.Length > DiscordMessageLimit
                    ? reportText.Substring(0, DiscordMessageLimit) + "\n\n[Report zkracen kvuli limitu Discord zpravy.]"
                    : reportText;

                string payload = JsonConvert.SerializeObject(new { content = "```text\n" + content + "\n```" });
                byte[] data = Encoding.UTF8.GetBytes(payload);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webhookUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    ChronicleLogger.Verbose($"Discord webhook response: {(int)response.StatusCode} {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Discord webhook failed: {ex.Message}");
            }
        }
    }
}
