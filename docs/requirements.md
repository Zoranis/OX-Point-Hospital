# Request for quotation: dispensing box for oxygen pressure cylinders

English translation of the client's request (`box_poptávka_v2.docx`). The proof of concept is
built against this document; [scope.md](scope.md) maps each requirement to the feature that meets
it.

## Notes on the basic concept

The basic concept (a box with 30 positions, cylinder tracking by barcode and cost centre,
empty↔full exchange, low-stock alerts, a web application for administration and invoicing) is
functionally sound and matches systems commonly used for dispensing technical and medical gases:

1. **Locking of positions**: positions must be individually lockable; otherwise it is impossible
   to enforce "same cost centre" or reliably track the full/empty status.
2. **Who changes the cylinder status back to "full"**: the barcode on the cylinder is fixed, so
   the full/empty status must be tracked by the system; a procedure needs to be defined for
   maintenance/the gas supplier when restocking.
3. **Feedback signalling**: there is no keypad, but without at least basic signalling
   (LED/buzzer/display) the user cannot tell whether a position unlocked and, if not, why.
4. **Assignment of positions to cost centres**: recommended to be handled in software/dynamically,
   not by a fixed division of compartments.
5. **The "no full cylinder available for the cost centre" state**: needs to be handled
   separately; it is more critical than just a threshold on the number of empty cylinders in the
   box.
6. **Alert threshold setting**: recommended to be tied to the number of remaining full cylinders
   per cost centre, not to the total number of empty cylinders in the box.
7. **Service access**: restocking full cylinders requires separate access (a key/technician
   barcode) outside normal user operation.
8. **Number and location of boxes**: affects whether the web application needs central records
   across multiple devices. We want to start with one box, but there may later be more of them in
   different locations.

## Purpose and principle of operation

We are requesting a dispensing box for exchanging oxygen pressure cylinders between the individual
hospital departments/cost centres and technical management (maintenance/the gas supplier). The box
is used for:

- unattended exchange of an empty cylinder for a full one, always within the same cost centre,
- recording which cost centre took how many full cylinders (basis for invoicing),
- automatically alerting maintenance when the stock of full cylinders is running low.

The box has no keypad or other input for manual entry; the only input is a barcode reader. Both
dispensing and returning a cylinder are done exclusively by scanning.

## Technical design of the box

- 30 separate positions for pressure cylinders.
- The assignment of a position to a cost centre is done in software (dynamic), not fixed by the
  design: any position may at a given moment contain a cylinder of any cost centre; the control
  unit tracks the occupancy and status of each position individually.
- Control unit with a barcode reader (scanner), without a keypad.
- Signalling elements (see below): at minimum an LED/indicator light and an audible signal,
  optionally a small information display (status display only, no input).
- The design and dimensions of the compartments must fit the cylinder used, CONOXIA® GO2X LIV,
  2 l volume (height approx. 510 mm, width/Ø approx. 110 mm); the cylinder may be stored lying
  down in the compartment.
- Separate service access for technicians outside normal user operation (see the safety
  requirements).

## Cylinder identification and full/empty status tracking

- Each cylinder has a permanent barcode (sticker/label) that uniquely identifies it throughout its
  service life. The barcode is assigned in software to the cost centre the cylinder belongs to;
  each cylinder always belongs to exactly one cost centre.
- If a scanned barcode is not registered in the system, the box must not perform any action (it
  must not unlock any position); unregistered codes cannot be processed.
- The current status of the cylinder (full/empty) and its location (in the box / at the cost
  centre) are tracked by the central system, not by the barcode itself. The code is a permanent
  identifier carrying the cost centre information. The validity of the code and its assignment to
  a cost centre are verified in the system registry.
- Depending on the cylinder status, the system keeps a flag (cost centre number): full = 9201
  (hospital campus), empty = 8441 (energy department). The flag is set automatically by the system
  on every scan and is not entered manually; both numbers must be changeable in the web
  application.
- The "full" status is set back when the box is restocked (see the process below): when inserting
  the refilled cylinders into the box, the supplier/technician scans each cylinder, which sets its
  status to "full", sets the flag to 9201 and assigns a position.
- The system keeps a history (log) of all status changes with a timestamp.

## Process for returning an empty cylinder and dispensing a full one

1. The user comes to the box with an empty cylinder and scans its barcode.
2. The system verifies: the code is registered → the cylinder's cost centre is determined from the
   code → the cylinder is not already recorded as returned (empty in the box) → at least one full
   cylinder is available for that cost centre.
3. If everything checks out: the box marks a free position for the return and unlocks it (the user
   places the empty cylinder in it, and the system records it as "empty" with flag 8441 at that
   position); at the same time, it unlocks a position with a full cylinder of the same cost
   centre, which the user takes.
4. If any condition is not met (invalid code, cylinder already recorded as returned, no full
   cylinder available, although this situation should not occur), the box unlocks nothing and
   signals the corresponding error (see signalling).
5. Every completed exchange is logged: cylinder barcode, cost centre, flag, action type
   (return/dispense), time, box ID and position.

## Service restocking (exchanging empty cylinders for full ones)

We recommend handling entry into service mode without adding an extra keypad or card reader, e.g.
with a physical key switch that the technician uses to switch the control unit into service mode
independently of the software and the network. Alternatively (or additionally), service mode can
be opened by scanning a dedicated service barcode/QR code with the same reader that users use; this
also records in the log which specific technician carried out the restocking.

1. The technician activates service mode (key / service code).
2. The box unlocks the positions with empty cylinders, and the technician removes them.
3. The technician places full cylinders in the freed positions and scans their barcodes. The
   system recognises the cost centre from the code and sets the status to "full" (flag 9201); just
   as in normal operation, an unregistered code is not processed.
4. Service mode is ended by switching the key back, or by an automatic timeout.

This is our proposed solution; we leave room for the manufacturer to propose its own service
access mechanism if it has a proven solution.

## Signalling and error states

Even without a keypad, the user needs feedback; otherwise they cannot tell whether the operation
succeeded and, if not, why. Recommended minimum scope:

- green indicator / short tone = position successfully unlocked,
- red indicator / different tone = error, distinguishing at least these states:
  - invalid/unregistered barcode,
  - cylinder is already recorded as returned (repeated scan),
  - no full cylinder is currently available for the cost centre,
- optionally a small display with a text description of the error state (display only, no input).

In addition to signalling on the box, the "no full cylinder available" state should immediately
(not only once a threshold is reached) trigger an alert to the administrator/maintenance, because
it means an interruption of the oxygen supply for that cost centre.

## Low stock alerts (e-mail/SMS)

- The threshold value for sending an alert must be configurable in the web application.
- We recommend tying the threshold to the number of remaining full cylinders for the given cost
  centre (not only to the total number of empty cylinders in the box); this better reflects the
  risk of a supply interruption for a specific department.
- It should be possible to set the threshold either for the whole box or separately for each cost
  centre.
- Alert recipients (e-mail/SMS) should be configurable as a list/group, possibly different for
  different cost centres.
- The "no full cylinders available for the cost centre" state (see signalling) should trigger an
  alert immediately, regardless of the configured threshold. However, it should never happen that
  a cost centre has no full cylinder at all: when the last cylinder for a cost centre is reached,
  an alert to restock cylinders should be sent.

## Web application for managing barcodes and cost centres

The web application (administration) should allow:

- registering cylinder barcodes and assigning them to a cost centre,
- deactivating a barcode (permanently removing a damaged/lost cylinder from the records),
- an overview of the current status of all positions in all boxes (full/empty/free, cost centre),
- setting alert thresholds (globally and per cost centre) and the list of e-mail/SMS recipients,
- transaction history (who/when/which cylinder/which cost centre/which action),
- role-based access control (administrator vs. read-only user for individual cost centres).

If there are multiple boxes, the application must aggregate the records centrally across all boxes
(each box has its own ID, and each transaction carries the box ID and position).

## Reporting and invoicing data for cost centres

- Export of an overview of the number of full cylinders dispensed per cost centre and selected
  period (e.g. a month) in a format suitable for invoicing (CSV/Excel).
- The overview should be filterable by cost centre, period and, if needed, individual box.
- Minimum required overviews (in addition to the export above): the current number of cylinders
  each cost centre has (at the cost centre / in the box),
- the number of full cylinders remaining on the hospital campus (flag 9201),
- the total number of cylinders collected under the energy department cost centre (flag 8441) for
  the selected period: the sum of cylinders consumed by all cost centres.
- We leave the form of the reports and the web application (scope of features, export method,
  manual or automatic periodic sending) to the manufacturer; please offer the standard reports and
  features the application supports.

## Box power supply

- The box is mains powered (230 V), without a solar panel or battery as the primary source.

## Safety and regulatory requirements

- Oxygen is an oxidising agent, so the design and location of the box should comply with the
  applicable safety and storage standards for oxygen pressure cylinders. The box must be ventilated
  for the entire time cylinders are stored in it, even when closed (the cylinders lie inside),
  maintain fire safety distances, and meet any other requirements for storing medical gases. We
  recommend asking the manufacturer to confirm compliance with the relevant standards (ČSN/EN) and
  for its recommendations on where to place the box.

## Box location and operating temperature

- The cylinder manufacturer specifies a permissible temperature range of -25 °C to +45 °C. The box
  (both its structure and all electronics: locks, reader, control unit, any display) should handle
  at least this range.
- The location of the box (outdoors or inside a building) has not been decided yet. We recommend
  designing the box so that it can also handle an outdoor location (an appropriate IP rating
  against moisture and dust, protection of the electronics against condensation and frost,
  possibly a temperature-controlled interior). The final location will then not constrain the
  design in any way, and the location can be decided later without having to modify the box.
