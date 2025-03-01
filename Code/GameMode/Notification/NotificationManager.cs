namespace KOTH.Notification;

/// <summary>
/// Manages the queue and display of notifications in the game.
/// NOTE - only one Notification will show at a time, even if its in different zones
/// </summary>
public class NotificationManager: SingletonComponent<NotificationManager>
{
	// Queue of notifications
	public Queue<Notification> NotificationQueue { get; set; } = new();

	// Notification - Time Display Ends
	public KeyValuePair<Notification, DateTime>? CurrentNotifications { get; set; } = null;

	/// <summary>
	/// Adds a new notification to the notification queue.
	/// </summary>
	/// <param name="notification">The notification to be added to the queue.</param>
	public void AddNotification(Notification notification)
	{
		NotificationQueue.Enqueue(notification);
	}

	/// <summary>
	/// Retrieves the currently active notification if one exists.
	/// </summary>
	/// <returns>
	/// The currently active notification or null if no notification is active.
	/// </returns>
	public Notification? GetNotification()
	{
		if (CurrentNotifications == null) return null;

		return CurrentNotifications.Value.Key;
	}


	/// <summary>
	/// Updates the state of the notification manager, including processing the notification queue
	/// and managing the display timing of the current notification.
	/// </summary>
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
