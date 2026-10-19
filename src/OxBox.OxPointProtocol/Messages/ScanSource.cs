namespace OxBox.OxPointProtocol.Messages;

public enum ScanSource
{
    /// <summary><c>{"QR": "..."}</c>, also expected for the barcode reader (open question #1).</summary>
    QrOrBarcode,

    /// <summary><c>{"RFID": ...}</c></summary>
    Rfid,

    /// <summary><c>{"CODE": ...}</c></summary>
    Keypad,
}
