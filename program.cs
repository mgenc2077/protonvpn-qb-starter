using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

// Optional: pass a timeout seconds, default 300 seconds = 5 minutes
var timeoutSeconds = 300;
var argsList = args.ToList();
var startupMode = argsList.Remove("--startup");
var targetAppPath = argsList.Count > 0 ? argsList[0] : @"C:\Program Files\qBittorrent\qbittorrent.exe";

var deadline = DateTimeOffset.Now.AddSeconds(timeoutSeconds);
var startupCutoff = DateTimeOffset.Now.AddMinutes(-3);

var listener = UserNotificationListener.Current;

var access = await listener.RequestAccessAsync();
if (access != UserNotificationListenerAccessStatus.Allowed)
{
    Console.Error.WriteLine($"Notification access not allowed: {access}");
    Console.Error.WriteLine("Enable notification access for this app (or run once interactively to grant permission).");
    return 2;
}

// Poll until we find a matching ProtonVPN toast or timeout
while (DateTimeOffset.Now < deadline)
{
    // Get recent toast notifications
    var notifs = await listener.GetNotificationsAsync(NotificationKinds.Toast);
    
    // Filter by time if in startup mode
    var relevantNotifs = startupMode 
        ? notifs.Where(n => n.CreationTime >= startupCutoff).ToList() 
        : (IReadOnlyList<UserNotification>)notifs;

    // Find latest ProtonVPN notification that contains "Active Port Number: <port>"
    var match = FindLatestProtonVpnPort(relevantNotifs);
    if (match is not null)
    {
        Console.WriteLine(match.Value.Port);
        try
        {
            Console.WriteLine($"Launching {targetAppPath} with port {match.Value.Port}...");
            Process.Start(targetAppPath, $"--torrenting-port={match.Value.Port}");
            Console.WriteLine("Process started successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start process: {ex.Message}");
            return 1;
        }

        return 0;
    }

    await Task.Delay(750);
}

Console.Error.WriteLine("Timed out waiting for ProtonVPN port notification.");
return 1;

static (int Port, DateTimeOffset Created)? FindLatestProtonVpnPort(IReadOnlyList<UserNotification> notifs)
{
    // Regex for: Active Port Number: 12345
    var re = new Regex(@"Active\s*Port\s*Number:\s*(\d{1,5})", RegexOptions.IgnoreCase);

    // Walk newest -> oldest
    foreach (var un in notifs.OrderByDescending(n => n.CreationTime))
    {
        if (!LooksLikeProtonVpn(un))
            continue;

        var allText = ExtractAllToastText(un.Notification);
        if (string.IsNullOrWhiteSpace(allText))
            continue;

        var m = re.Match(allText);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var port))
            return (port, un.CreationTime);
    }

    return null;
}

static bool LooksLikeProtonVpn(UserNotification un)
{
    // Best-effort identification:
    // - AppUserModelId is the most reliable when present
    // - Display name varies by localization / packaging
    var aumid = un.AppInfo?.AppUserModelId ?? "";
    var display = un.AppInfo?.DisplayInfo?.DisplayName ?? "";

    if (aumid.Contains("Proton", StringComparison.OrdinalIgnoreCase) ||
        aumid.Contains("ProtonVPN", StringComparison.OrdinalIgnoreCase))
        return true;

    if (display.Contains("Proton", StringComparison.OrdinalIgnoreCase) ||
        display.Contains("ProtonVPN", StringComparison.OrdinalIgnoreCase))
        return true;

    return false;
}

static string ExtractAllToastText(Notification notif)
{
    try
    {
        var visual = notif.Visual;
        if (visual is null) return "";

        // Most toasts use ToastGeneric binding
        var binding = visual.GetBinding(KnownNotificationBindings.ToastGeneric);
        if (binding is null) return "";

        var texts = binding.GetTextElements();
        if (texts is null || texts.Count == 0) return "";

        return string.Join("\n", texts.Select(t => t?.Text ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
    }
    catch
    {
        return "";
    }
}
