using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendTwoFactorCodeAsync(string email, string code)
    {
        var subject = "Your Two-Factor Authentication Code";
        var htmlBody = $@"
            <h2>Two-Factor Authentication</h2>
            <p>Your verification code is: <strong>{code}</strong></p>
            <p>This code will expire in 10 minutes.</p>
            <p>If you didn't request this code, please ignore this email.</p>
        ";

        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        var resetUrl = $"{_configuration["AppUrl"]}/reset-password?token={resetToken}&email={email}";
        var subject = "Reset Your Password";
        var htmlBody = $@"
            <h2>Password Reset Request</h2>
            <p>You requested to reset your password.</p>
            <p>Click the link below to reset your password:</p>
            <p><a href='{resetUrl}'>Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendInvitationEmailAsync(string email, string invitationToken, string inviterName)
    {
        var invitationUrl = $"{_configuration["AppUrl"]}/accept-invitation?token={invitationToken}";
        var subject = "You've Been Invited!";
        var htmlBody = $@"
            <h2>Team Invitation</h2>
            <p>{inviterName} has invited you to join their team.</p>
            <p>Click the link below to accept the invitation and create your account:</p>
            <p><a href='{invitationUrl}'>Accept Invitation</a></p>
            <p>This invitation will expire in 7 days.</p>
        ";

        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var subject = "Welcome to Our Platform!";
        var htmlBody = $@"
            <h2>Welcome, {name}!</h2>
            <p>Thank you for joining our platform.</p>
            <p>We're excited to have you on board!</p>
        ";

        await SendEmailAsync(email, subject, htmlBody);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _configuration["Maileroo:ApiKey"];
            var fromEmail = _configuration["Maileroo:FromEmail"];
            var fromName = _configuration["Maileroo:FromName"];

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var emailPayload = new
            {
                from = new { email = fromEmail, name = fromName },
                to = new[] { new { email = to } },
                subject = subject,
                html = htmlBody
            };

            var content = new StringContent(
                JsonSerializer.Serialize(emailPayload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://smtp.maileroo.com/send", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send email to {to}. Status: {response.StatusCode}, Error: {errorContent}");
                throw new Exception($"Failed to send email: {errorContent}");
            }

            _logger.LogInformation($"Email sent successfully to {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending email to {to}");
            throw;
        }
    }
}
