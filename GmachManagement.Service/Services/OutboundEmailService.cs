using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace GmachManagement.Service.Services;

public sealed class OutboundEmailService(IConfiguration configuration)
{
    public async Task SendAsync(string recipient,string subject,string body,CancellationToken ct)
    {
        var host=configuration["Email:SmtpHost"];
        var from=configuration["Email:From"];
        var username=configuration["Email:Username"];
        var password=configuration["Email:Password"];
        if(string.IsNullOrWhiteSpace(host)||string.IsNullOrWhiteSpace(from)||
           string.IsNullOrWhiteSpace(username)||string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Email delivery is not configured.");
        using var mail=new MailMessage(from,recipient){Subject=subject,Body=body};
        using var smtp=new SmtpClient(host,int.TryParse(configuration["Email:SmtpPort"],out var port)?port:587)
        {EnableSsl=true,Credentials=new NetworkCredential(username,password)};
        await smtp.SendMailAsync(mail,ct);
    }
}