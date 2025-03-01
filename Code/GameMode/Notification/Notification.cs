namespace KOTH.Notification;

public record Notification(string Message, int Duration, NotificationZone Zone, string CustomRazor = null, string Color = "#FFFFFF", string Css = "");
