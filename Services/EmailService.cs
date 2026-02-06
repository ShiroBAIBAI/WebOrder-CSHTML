using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace Demo.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) => _config = config;

    public void SendEmail(string toEmail, string subject, string htmlBody, byte[]? attachment = null, string? filename = null)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"] ?? "smtp.gmail.com";
        var user = smtp["User"];               
        var pass = smtp["Pass"];              
        var port = int.TryParse(smtp["Port"], out var p) ? p : 587;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp["FromName"] ?? "Sharkz Food",
                                            smtp["FromEmail"] ?? user));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var body = new BodyBuilder { HtmlBody = htmlBody };
        if (attachment != null && !string.IsNullOrEmpty(filename))
            body.Attachments.Add(filename!, attachment);
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();

        client.CheckCertificateRevocation = false;
        client.ServerCertificateValidationCallback = (s, cert, chain, errors) =>
        {
            if (!host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
                return errors == SslPolicyErrors.None;

            if (errors == SslPolicyErrors.None) return true;

            if (errors == SslPolicyErrors.RemoteCertificateChainErrors && chain != null)
            {
                var ignorable = true;
                foreach (var st in chain.ChainStatus)
                {
                    if (st.Status == X509ChainStatusFlags.RevocationStatusUnknown ||
                        st.Status == X509ChainStatusFlags.OfflineRevocation) 
                        continue;

                    ignorable = false;
                    break;
                }
                return ignorable;
            }

            return false;
        };

        try
        {
            client.Connect(host, port, SecureSocketOptions.StartTls);
        }
        catch (SslHandshakeException)
        {
            client.Disconnect(true);
            client.Connect(host, 465, SecureSocketOptions.SslOnConnect);
        }

        if (!string.IsNullOrEmpty(user))
            client.Authenticate(user, pass);

        client.Send(message);
        client.Disconnect(true);
    }
}
