namespace KOTH.Notification;

public class NotificationManager: SingletonComponent<NotificationManager>
{
	// Queue of notifications
	public Queue<Notification> NotificationQueue { get; set; } = new();

	// Notification - Time Display Ends
	public KeyValuePair<Notification, DateTime>? CurrentNotifications { get; set; } = null;

	public void AddNotification(Notification notification)
	{
		NotificationQueue.Enqueue(notification);
	}

	public Notification? GetNotification()
	{
		if (CurrentNotifications == null) return null;

		return CurrentNotifications.Value.Key;
	}


	protected override void OnUpdate()
	{
        if (CurrentNotifications != null && CurrentNotifications.Value.Value <= DateTime.Now)
		{
			CurrentNotifications = null;
		}

		if (CurrentNotifications == null && NotificationQueue.Count > 0)
		{
			var notification = NotificationQueue.Dequeue();
			CurrentNotifications = new KeyValuePair<Notification, DateTime>(notification, DateTime.Now.AddSeconds(notification.Duration));
		}
	}
}
