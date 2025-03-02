namespace KOTH.Notification;

/// <summary>
/// Represents a notification with customizable message, duration, display zone,
/// optional custom Razor template, color, and CSS style.
/// </summary>
/// <param name="Message">The message text of the notification.</param>
/// <param name="Duration">The duration (in seconds) for which the notification will be displayed.</param>
/// <param name="Zone">The display zone of the notification on the screen.</param>
/// <param name="Color">The color code of the notification text. Defaults to white (#FFFFFF).</param>
/// <param name="Css">Optional parameter to provide custom CSS style for the notification. Defaults to an empty string.</param>
/// <param name="Image">Optional parameter to provide an image path to the notification. Defaults to null.</param>
public struct Notification()
{
	public string Message { get; init; } = "";

	public int Duration { get; init; } = 0;
	public NotificationZone Zone { get; init; } = NotificationZone.TopCenter;
	public string Color { get; init; } = "#FFFFFF";
	public string Css { get; init; } = "";

	public string Image { get; init; } = null;
};
