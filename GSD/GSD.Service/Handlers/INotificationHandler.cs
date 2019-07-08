using GSD.Common.NamedPipes;

namespace GSD.Service.Handlers
{
    public interface INotificationHandler
    {
        void SendNotification(int sessionId, NamedPipeMessages.Notification.Request request);
    }
}
