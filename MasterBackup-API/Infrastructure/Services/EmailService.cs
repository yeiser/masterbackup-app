using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;

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

    public async Task SendTwoFactorCodeAsync(ApplicationUser user, string code)
    {
        var subject = "Tu código de verificación de seguridad 2FA";
        var templateData = new Dictionary<string, string>
        {
            { "USERNAME", $"{user.FirstName} {user.LastName}" },
            { "CODIGO", code }
        };

        await SendEmailAsync(user, subject, null, "4533",  templateData);
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user, string resetToken)
    {
        var encodedToken = Uri.EscapeDataString(resetToken);
        var resetUrl = $"{_configuration["AppUrl"]}/reset-password?token={encodedToken}";
        var subject = "Recupera tu contraseña de manera segura";
        var templateData = new Dictionary<string, string>
        {
            { "USERNAME", $"{user.FirstName} {user.LastName}" },
            { "URL", resetUrl }
        };

        await SendEmailAsync(user, subject, null, "4539", templateData);
    }

    public async Task SendInvitationEmailAsync(ApplicationUser user, string invitationToken, string inviterName)
    {
        var invitationUrl = $"{_configuration["AppUrl"]}/accept-invitation?token={invitationToken}";
        var subject = "¡Has sido invitado!";
        var htmlBody = $@"
            <h2>Invitación al Equipo</h2>
            <p>{inviterName} te ha invitado a unirte a su equipo.</p>
            <p>Haz clic en el enlace de abajo para aceptar la invitación y crear tu cuenta:</p>
            <p><a href='{invitationUrl}'>Aceptar Invitación</a></p>
            <p>Esta invitación expirará en 7 días.</p>
        ";

        await SendEmailAsync(user, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(ApplicationUser user)
    {
        var subject = "Bienvenido a MasterBackup!";
        var templateData = new Dictionary<string, string>
        {
            { "USERNAME", $"{user.FirstName} {user.LastName}" }
        };

        await SendEmailAsync(user, subject, null, "4530", templateData);
    }

    private async Task SendEmailAsync(ApplicationUser user, string subject, string? htmlBody, string templateId = null, Dictionary<string, string> templateData = null)
    {
        try
        {
            var apiKey = _configuration["Maileroo:ApiKey"];
            var fromEmail = _configuration["Maileroo:FromEmail"];
            var fromName = _configuration["Maileroo:FromName"];

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var emailPayload = new
            {
                from = new { address = fromEmail, display_name = fromName },
                to = new[] { new { address = user.Email , display_name = user.FirstName } },
                subject = subject,
                template_id = templateId,
                template_data = templateData
            };

            var content = new StringContent(
                JsonSerializer.Serialize(emailPayload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://smtp.maileroo.com/api/v2/emails/template", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send email to {emailPayload.to[0].address}. Status: {response.StatusCode}, Error: {errorContent}");
                throw new Exception($"Failed to send email: {errorContent}");
            }

            _logger.LogInformation($"Email sent successfully to {emailPayload.to[0].address}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending email");
            throw;
        }
    }
}
