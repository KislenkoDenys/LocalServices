using Microsoft.AspNetCore.SignalR;

namespace LocalServices.Hubs;

public class NotesHub : Hub
{
    private static string? _latestDocumentJson;
    private static readonly object _syncLock = new();

    public async Task UpdateDocument(string documentJson)
    {
        lock (_syncLock)
        {
            _latestDocumentJson = documentJson;
        }

        await Clients.Others.SendAsync("ReceiveDocumentUpdate", documentJson);
    }

    public Task<string?> GetLatestDocument()
    {
        lock (_syncLock)
        {
            return Task.FromResult(_latestDocumentJson);
        }
    }

    public override async Task OnConnectedAsync()
    {
        string? doc;
        lock (_syncLock)
        {
            doc = _latestDocumentJson;
        }

        if (!string.IsNullOrEmpty(doc))
        {
            await Clients.Caller.SendAsync("ReceiveDocumentUpdate", doc);
        }

        await base.OnConnectedAsync();
    }
}

