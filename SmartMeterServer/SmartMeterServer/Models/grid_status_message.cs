namespace SmartMeterServer.Models
{
    public record GridStatusMessage(
        string Type,
        string SchemaVersion,
        string Status,
        string ClientAction,
        string Title,
        string Message,
        DateTime RaisedAtUtc
    );
}
