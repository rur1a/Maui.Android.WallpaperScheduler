namespace MAUI.Application.Abstractions;

public interface IUserNotificationService
{
	Task ShowShortMessageAsync(string message, CancellationToken cancellationToken = default);
}
