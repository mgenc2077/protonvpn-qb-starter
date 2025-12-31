# protonvpn-qb-starter

A simple utility to detect the active listening port from ProtonVPN toast notifications and automatically launch a torrent client (or any other application) with the detected port configured. You can add PortGrabber.exe to the Task Scheduler to run at startup for a headless start (I am currently using it this way).

![protonvpn-qb-starter](https://github.com/mgenc2077/protonvpn-qb-starter/blob/main/screenshot.png?raw=true)

## How it Works

1.  Polls Windows Toast Notifications for a "ProtonVPN" notification containing "Active Port Number".
2.  Extracts the port number.
3.  Launches the specified application executable with the port passed as an argument.

## Usage

### Default Behavior
If run without arguments, the application attempts to launch **qBittorrent** at the default path:
`C:\Program Files\qBittorrent\qbittorrent.exe`

It passes the argument: `--torrenting-port=<PORT>`

### Custom Application
You can specify a different executable path as a command-line argument:

```powershell
PortGrabber.exe "C:\Path\To\Your\Application.exe"
```

## Building

Prerequisites:
-   .NET 8.0 SDK or later

Current Directory:
```powershell
dotnet build
```

The output executable (e.g., `PortGrabber.exe`) will be in the `bin/Debug/net8.0-windows/` (or `Release`) folder.

## Requirements

-   Windows 10/11
-   ProtonVPN (with notifications enabled)
