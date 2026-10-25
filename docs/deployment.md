# Installing the OxBox demo on another PC

This package runs the whole proof of concept on one Windows PC, **without Docker and without the
.NET SDK**: the OxBox server, the BU simulator that stands in for the box, and the database (a
file inside the server folder). Other PCs on the network open the web app and the box display in
a browser.

It is a demo installation: it contains demo data and demo accounts, and the simulator. Do not use
it with a real box.

## What the target PC needs

- Windows 10 or 11, or Windows Server 2016 or newer, 64-bit.
- About 300 MB of disk space.
- Administrator rights once, to install the services.
- Free ports 5029 (web app), 5080 and 3030 (simulator).

Nothing else needs installing: the package contains the .NET runtime.

## 1. Build the package (on your development PC)

From the `oxbox-poc` folder:

```powershell
powershell -ExecutionPolicy Bypass -File deploy/publish.ps1
```

It creates `artifacts/OxBox-win-x64.zip`. Copy that file to the target PC, for example with a USB
stick or a network share.

## 2. Install it (on the target PC)

1. Unpack the zip to the folder where OxBox should stay, for example `C:\OxBox`. The services run
   the programs from this folder, and the database is created in `C:\OxBox\server\data`.
2. Open PowerShell **as administrator**: Start menu → type "PowerShell" → right-click →
   **Run as administrator**.
3. Run the install script:

   ```powershell
   powershell -ExecutionPolicy Bypass -File C:\OxBox\install-service.ps1
   ```

   It installs two Windows services, **OxBox server** and **OxBox BU simulator**, and starts them.
   It also adds a firewall rule that lets other PCs reach port 5029. The services start
   with Windows, run without anyone signed in, and restart by themselves after a crash.

On the first start the server creates the database, a box with 30 positions, the demo data and the
demo accounts. This takes a few seconds.

## 3. Open it

| Page | Address | Where |
|---|---|---|
| Web app | `http://<pc-name>:5029` | Any PC on the network |
| Box display | `http://<pc-name>:5029/kiosk/BOX-01` | Any PC on the network (add `?lang=en` for English) |
| Simulator | `http://localhost:5080` | The target PC only |

`<pc-name>` is the target PC's name or IP address; the install script prints it. On the target PC
itself, `http://localhost:5029` works too.

Demo accounts:

| Role | E-mail | Password |
|---|---|---|
| Administrator | `admin@oxbox.local` | `OxBox-Demo-Admin-1` |
| Read-only user for Interní klinika | `viewer.interna@oxbox.local` | `OxBox-Demo-Viewer-1` |

The demo walk-through in the main README (section 5, "Try it") works the same way here.

The web app uses plain HTTP, so passwords cross the network unencrypted. That is fine for a demo
on a trusted network, but the demo passwords must not be reused anywhere.

## Settings

The server's settings are in `server\appsettings.Production.json`: the demo box, the demo accounts,
the mail server for alert e-mails (`localhost:2525`) and the port (`Urls`). After changing them,
restart the service:

```powershell
Restart-Service OxBox
```

The accounts and the box are only created when the database is empty, so changing them later has
no effect on an existing database; change passwords and add users on the web app's **Users** page.

Without a mail server, alert e-mails wait in the outbox and the server keeps retrying them; the
alerts still show in the web app.

## Day-to-day

- **Stop and start:** `Stop-Service OxBox, OxBoxSimulator` and `Start-Service OxBoxSimulator, OxBox`
  in an administrator PowerShell, or use the Services app (`services.msc`).
- **Logs:** the services write to the Windows Event Viewer, under **Windows Logs → Application**,
  with the source `OxBox.Web` or `OxBox.BuSimulator`. For a closer look, stop the service and run
  the server in a PowerShell window, where it prints everything. Start it from its own folder, or
  it will not find its settings and data: `cd C:\OxBox\server`, then `.\OxBox.Web.exe`.
- **Back up the data:** stop the OxBox service, copy the `server\data` folder, and start the
  service again. The folder holds the database and the keys for sign-ins.
- **Start over with fresh demo data:** stop the OxBox service, delete the `server\data` folder, and
  start the service again.

## Updating to a new version

1. Build a new package and copy it to the target PC.
2. Stop the services: `Stop-Service OxBox, OxBoxSimulator`.
3. Copy the `server\data` folder somewhere safe.
4. Replace the contents of `C:\OxBox` with the new package, then put the `server\data` folder
   back. If you changed `server\appsettings.Production.json`, put your copy back as well.
5. Run `install-service.ps1` again. It updates and starts the services. On start the server
   updates the database to the new version by itself.

## Uninstalling

In an administrator PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File C:\OxBox\uninstall-service.ps1
```

It removes both services and the firewall rule and keeps the folder with its data. Delete
`C:\OxBox` afterwards if the data is no longer needed.

## If something goes wrong

| Problem | Fix |
|---|---|
| `Run this script as administrator` | Open PowerShell with **Run as administrator** and run the script again. |
| `running scripts is disabled on this system` | Start the script with `powershell -ExecutionPolicy Bypass -File ...` as shown above. |
| Other PCs cannot open the web app, but `http://localhost:5029` on the target PC works | The network blocks port 5029, or a firewall other than Windows Firewall does. Ask the network administrator to allow TCP 5029 to this PC. |
| The service stops right after starting | Look in Event Viewer (see **Logs** above). A common cause is another program using port 5029, 5080 or 3030. |
| The box display says the box is offline | The simulator service is not running: `Start-Service OxBoxSimulator`. |
| Everyone is signed out after an update | The `server\data` folder was not put back, so the sign-in keys are new. Put back the folder from step 3. |
| Everyone is signed out after moving the data to another PC | Expected: the sign-in keys are encrypted for the PC that made them, so the new PC makes new ones. Sign in again; no data is lost. |
