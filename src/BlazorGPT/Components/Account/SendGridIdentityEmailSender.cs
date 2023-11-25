using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BlazorGPT.Components.Account;

public class SendGridIdentityEmailSender : IEmailSender<IdentityUser>
{
    private readonly IEmailSender _emailSender;

    public SendGridIdentityEmailSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        _emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        _emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        _emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
}

