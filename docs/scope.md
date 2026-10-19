# Scope of the proof of concept

The proof of concept contains only the features that the request for quotation
([requirements.md](requirements.md)) asks for. It is a cut-down copy of a fuller OxBox solution:
about 9,000 lines of application code against about 15,600 in the full version.

## What it does

| Requirement in the RFQ | How the proof of concept meets it |
|---|---|
| 30 lockable positions, assigned to cost centres in software | Any position can hold any cost centre's cylinder; the system tracks each one |
| The only input is a barcode reader; unregistered codes open nothing | Every scan goes through `ExchangeRules` or `ServiceRules` first |
| Full/empty status and location tracked centrally, flags 9201/8441 set automatically and editable | Flags are recorded on every movement and can be changed on the **Settings** page |
| History of every status change | **Cylinders → (barcode)** lists each movement with time, box, slot and flag |
| Exchange: return an empty, get a full one of the same cost centre, all logged | Both slots unlock together; both movements are recorded |
| Distinct errors: unknown code, already returned, no full cylinder | Red lamp + buzzer, and the reason on the status display |
| Service mode with a technician's code: empties out, fulls in, end by code or timeout | Scan the service code; it ends with the code again or after 10 minutes without a scan |
| Stock alerts per cost centre, threshold globally or per cost centre, immediately at none and at the last cylinder | Three levels: low stock, last cylinder, no cylinder. E-mail through an outbox; SMS are logged until a provider is chosen |
| Recipient lists, also per cost centre | **Alerts** page |
| Web app: register, assign and deactivate barcodes; overview of all positions in all boxes; thresholds; recipients; transaction history; admin and read-only roles per cost centre | **Dashboard**, **Cylinders**, **Cost centres**, **Alerts**, **Transactions**, **Users**, **Boxes** |
| Reports: dispensed per cost centre and period (filterable by box) as CSV; cylinders per cost centre; full on campus (9201); collected under 8441 | **Reports** page, with CSV export |

The status display (CZ / EN) is the only thing kept that the RFQ calls optional. Without it the box
could not tell users which of the three errors happened.

## How an exchange works

1. A user scans an empty cylinder.
2. The checks run in the RFQ's order: the code is registered, the cylinder is not already in the
   box, it is at its department, its cost centre has a full cylinder here, and there is a free slot.
3. The slot for the empty cylinder unlocks, and the return is recorded (empty, flag 8441).
4. The full cylinder's slot unlocks, and the dispense is recorded (full, flag 9201). This is what
   is invoiced.
5. The display shows both slot numbers, and the green lamp lights.

If the first lock does not open, nothing is recorded. If the second one does not open, the return
stays recorded and the display asks the user to contact maintenance.

## What was left out, and what that means

| Left out | Consequence |
|---|---|
| Door watching | A movement counts once its lock opens. If a user walks away, or puts the empty cylinder into the full one's slot, the records do not notice. |
| Step-by-step saved sessions and restart recovery | A crash between unlocking and saving can leave an unlock unrecorded. A service visit is held in memory, so a restart ends it; movements already made stay recorded. |
| Trying another slot when a lock fails | The user sees an error and contacts maintenance. |
| Multi-server lease | **Run only one server.** Nothing stops a second server from driving the same box. |
| OPEN ALL handling and inventory check | The BU's OPEN ALL and box-door notifications are only logged. After OPEN ALL the records may not match the box. |
| Alerts for faults, open doors, an offline box | Only the three stock alerts exist. A box that is offline shows as "Offline" on the dashboard. |
| Excel export, audit log of admin actions, guided tours, kiosk walkthrough, technician hints | Not needed by the RFQ. |
| Account self-service (2FA, password reset by e-mail) | Administrators create accounts and reset passwords on the **Users** page. |
| Deployment pieces (Windows service, reverse proxy, health checks, database-stored sign-in keys) | Run it with `dotnet run` for now. |
