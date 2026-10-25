# OxBox proof of concept

OxBox runs an unattended box where hospital departments swap an empty oxygen cylinder for a full
one by scanning its barcode. It records every swap for invoicing and warns maintenance when a
department is running out of full cylinders.

This proof of concept contains only what the client's request for quotation asks for. You don't
need the real box to try it: a **simulator** stands in for the box's control unit and gives you a
web page where you "scan" barcodes, so you can test everything on one PC.

- [docs/requirements.md](docs/requirements.md): the client's request (English translation)
- [docs/scope.md](docs/scope.md): which feature meets which requirement, and what was left out
- [docs/development.md](docs/development.md): code structure, tests and coding standards
- [docs/deployment.md](docs/deployment.md): installing the demo on another Windows PC, without Docker or the .NET SDK

## Before you start: use Windows, not WSL

These instructions are for **Windows 10 or 11**. Run every command in **PowerShell** or
**Command Prompt** (Start menu → type "PowerShell"; Windows Terminal works too).

Do **not** use a WSL or Ubuntu terminal. Linux there has its own `dotnet`, which does not use the
Windows one you install below.

## 1. Install the prerequisites (once)

Install these on Windows:

| Tool | Why | Get it |
|---|---|---|
| .NET SDK 9 (9.0.300 or newer) | Builds and runs the application | https://dotnet.microsoft.com/download/dotnet/9.0 |
| Git | Downloads the code | https://git-scm.com/downloads |

To check that they work, open a new PowerShell window (so it sees the newly installed tools) and
run `dotnet --version`, which should print 9.0.300 or higher.

Nothing else is needed. The database is SQLite, which is built into the server: it is a single file
that the server creates by itself, `src/OxBox.Web/data/oxbox_poc.db`.

## 2. Get the code

```powershell
git clone https://github.com/Zoranis/OX-Point-Hospital oxbox-poc
```
```powershell
cd oxbox-poc
```

Run every command below from this folder.

## 3. Start the system

You need **two PowerShell windows**, each in the `oxbox-poc` folder. Both commands keep running;
leave their windows open.

**Window 1: the box simulator.** Wait until it prints `Now listening on: http://localhost:5080`.

```powershell
dotnet run --project tools/OxBox.BuSimulator
```

**Window 2: the OxBox server.** The first start takes a minute: it builds the code, creates the
database file, a box with 30 positions and demo data. Wait until it prints
`Now listening on: http://localhost:5029`. It may open a browser tab by itself.

```powershell
dotnet run --project src/OxBox.Web --launch-profile http
```

## 4. Open the three pages

Put them side by side; they update live.

| Page | Address | What it is |
|---|---|---|
| Simulator | http://localhost:5080 | Stands in for the box: the barcode reader, the 30 locks and the lamps |
| Box display | http://localhost:5029/kiosk/BOX-01 | The small screen on the box that tells the user what happened (Czech; add `?lang=en` for English) |
| Web app | http://localhost:5029 | Administration for the hospital: overview, cylinders, alerts, reports |

Sign in to the web app with one of these demo accounts:

| Role | E-mail | Password |
|---|---|---|
| Administrator (sees and changes everything) | `admin@oxbox.local` | `OxBox-Demo-Admin-1` |
| Read-only user for Interní klinika only | `viewer.interna@oxbox.local` | `OxBox-Demo-Viewer-1` |

## 5. Try it

The demo data has three departments (cost centres): **Interní klinika** and **Chirurgická klinika**
each have 3 full cylinders in the box, and **ARO** has only 1. To scan, click a barcode button on
the simulator page or type a code into its scan field. Open doors turn yellow on the simulator;
click one to close it.

1. **A normal exchange.** Scan **O2-INT-04**, an empty cylinder of Interní klinika. Slot 8 opens
   for the empty cylinder and slot 1 for a full one. The display shows both slot numbers and the
   lamp turns green. In the web app, **Transactions** shows the return and the dispense.
2. **The three errors.** Each turns the lamp red and the display explains why; nothing opens.
   - Scan **O2-INT-04** again: the cylinder is already in the box.
   - Scan **NEZNAMY-999**: the code is not registered.
   - Scan **O2-ARO-02** (succeeds, and uses ARO's only full cylinder), then **O2-ARO-03**: there is
     no full cylinder left for ARO. The web app's **Dashboard** now shows a stock alert.
3. **Restocking by a technician.** Scan **SERVIS-001** (technician Jan Novák). Every slot holding
   an empty cylinder opens so they can be taken out. Scan **O2-ARO-04**, a refilled ARO
   cylinder: a free slot opens for it and it is recorded as full. Scan **SERVIS-001** again to
   finish (otherwise service mode ends after 10 minutes without a scan).
4. **A broken lock.** On the simulator, right-click **Slot 2** to jam its lock (it turns red),
   then scan **O2-INT-05**. Its empty slot opens, but slot 2, which holds the full cylinder, does
   not answer. After about 10 seconds (the server tries three times) the display shows "The full
   cylinder could not be released" for a few seconds and asks the user to contact maintenance.
   Right-click slot 2 again to free it.
5. **The web app.** As the administrator, look at:
   - **Dashboard**: every slot in the box, the stock per department and the active alerts;
   - **Cylinders**: register a new barcode, deactivate one, or open one to see its full history;
   - **Transactions**: every return and dispense, with time, box, slot and flag;
   - **Alerts**: who receives alerts (for all departments or one), and every alert sent;
   - **Settings**: the full (9201) and empty (8441) flags, and the low-stock threshold;
   - **Cost centres**: a department's own threshold, if it should differ from the default;
   - **Reports**: cylinders dispensed per department and period, with CSV export.

   Then sign in as the read-only user: they see only Interní klinika and cannot change anything.

## 6. Stop it and start again

- Stop the simulator and the server with **Ctrl+C** in their windows.
- All data stays in `src/OxBox.Web/data/` and is there again on the next start.
- **Start over with fresh demo data:** with the server stopped, delete the database files and start
  the server again; it recreates everything.

  ```powershell
  Remove-Item src/OxBox.Web/data/oxbox_poc.db*
  ```

## If something goes wrong

| Problem | Fix |
|---|---|
| `Remove-Item` says the database file is in use | The server is still running. Stop it with **Ctrl+C**, then try again. |
| `address already in use` for port 5029, 5080 or 3030 | Another program uses that port, maybe a second copy of the server or simulator. Close it. |
| The box display says the box is offline | Start the simulator; the server reconnects to it within a few seconds. |
| A `SDK not found` or `global.json` error | Install the .NET 9 SDK (9.0.300 or newer). |

**Alert e-mails** are sent to a mail server on `localhost:2525`. Without one, the alerts still
appear in the web app and their e-mails wait in an outbox; the server's window then shows
warnings that an e-mail failed and will be retried, which you can ignore. To see the e-mails, install
and run a test mail server, then open its inbox at http://localhost:3000:

```powershell
dotnet tool install -g Rnwood.Smtp4dev
```
```powershell
smtp4dev --urls=http://localhost:3000 --smtpport=2525
```

SMS are only written to the server's log until an SMS provider is chosen.

## Run the automated tests

```powershell
dotnet test
```

The database tests each create a temporary SQLite file and delete it afterwards. To run only the
fast unit tests, use `dotnet test --filter "Category!=Integration"`.
