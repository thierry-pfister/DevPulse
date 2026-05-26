using DevPulse.Domain.Episodes;

namespace DevPulse.Infrastructure.Notifications;

public interface IEmailNotifier
{
    Task SendDraftReadyAsync(Episode episode);
    Task SendPublishedAsync(Episode episode);
}
