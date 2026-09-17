using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;

namespace ERPServer.Application.Mail;

/// <summary>
/// Mail sunucusu yapılandırılmadığında gerçek göndericinin yerine geçer. Mail
/// gönderemeyen bir kurulumda demo doğrulaması zaten kapalı kalıyor; buradaki
/// amaç, çağrının patlamak yerine sessizce düşmesi.
/// </summary>
internal sealed class NullEmailSender(ILogger<NullEmailSender> logger) : ISender
{
    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        logger.LogInformation(
            "SMTP yapılandırılmadı; {Recipients} adresine gidecek \"{Subject}\" başlıklı mail düşürüldü.",
            string.Join(", ", email.Data.ToAddresses.Select(a => a.EmailAddress)),
            email.Data.Subject);

        return new SendResponse();
    }

    public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null) =>
        Task.FromResult(Send(email, token));
}
