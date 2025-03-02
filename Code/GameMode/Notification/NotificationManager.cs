namespace KOTH.Notification;

/// <summary>
/// Manages the queue and display of notifications in the game.
/// NOTE - only one Notification will show at a time, even if its in different zones
/// </summary>
public class NotificationManager: SingletonComponent<NotificationManager>
{
	// Queue of notifications
	public Queue<FNotification> NotificationQueue { get; set; } = new();

	// Notification - Time Display Ends
	public KeyValuePair<FNotification, DateTime>? CurrentNotifications { get; set; } = null;

	/// <summary>
	/// Adds a new notification to the notification queue.
	/// </summary>
	/// <param name="Notification">The notification to be added to the queue.</param>
	public void AddNotification(FNotification Notification)
	{
		NotificationQueue.Enqueue(Notification);
	}

	/// <summary>
	/// Retrieves the currently active notification if one exists.
	/// </summary>
	/// <returns>
	/// The currently active notification or null if no notification is active.
	/// </returns>
	public FNotification? GetNotification()
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
			CurrentNotifications = new KeyValuePair<FNotification, DateTime>(notification, DateTime.Now.AddSeconds(notification.Duration));
		}
	}
}
