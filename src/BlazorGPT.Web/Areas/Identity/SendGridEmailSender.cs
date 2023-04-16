using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BlazorGPT.Web.Areas.Identity;

public class SendGridEmailSender : IEmailSender
{
    private readonly IConfiguration configuration;
    private readonly ILogger logger;

    public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        string sendGridApiKey = configuration["SendGridApiKey"];
        if (string.IsNullOrEmpty(sendGridApiKey))
        {
            throw new Exception("The 'SendGridApiKey' is not configured");
        }

        var client = new SendGridClient(sendGridApiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress("magols@live.com", "ChatGPT Conversations"),
            Subject = subject,
            PlainTextContent = message,
            HtmlContent = message
        };
        msg.AddTo(new EmailAddress(toEmail));

        var response = await client.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Email queued successfully");
        }
        else
        {
            logger.LogError("Failed to send email");
            // Adding more information related to the failed email could be helpful in debugging failure,
            // but be careful about logging PII, as it increases the chance of leaking PII
        }
    }
}