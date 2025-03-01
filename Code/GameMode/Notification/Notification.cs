namespace KOTH.Notification;

/// <summary>
/// Represents a notification with customizable message, duration, display zone,
/// optional custom Razor template, color, and CSS style.
/// </summary>
/// <param name="Message">The message text of the notification.</param>
/// <param name="Duration">The duration (in seconds) for which the notification will be displayed.</param>
/// <param name="Zone">The display zone of the notification on the screen.</param>
/// <param name="CustomRazor">Optional parameter to specify a custom Razor template for the notification.</param>
/// <param name="Color">The color code of the notification text. Defaults to white (#FFFFFF).</param>
/// <param name="Css">Optional parameter to provide custom CSS style for the notification. Defaults to an empty string.</param>
public record Notification(
	string Message,
	int Duration,
	NotificationZone Zone,
	string CustomRazor = null,
	string Color = "#FFFFFF",
	string Css = "");
