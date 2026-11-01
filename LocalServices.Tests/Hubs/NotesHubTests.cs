using LocalServices.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace LocalServices.Tests.Hubs;

public class NotesHubTests
{
    [Fact]
    public async Task UpdateDocument_BroadcastsToOthers_AndUpdatesLatestDocument()
    {
        var hub = new NotesHub();
        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockClients.Setup(c => c.Others).Returns(mockClientProxy.Object);
        hub.Clients = mockClients.Object;

        var samplePayload = "{\"title\":\"Test Doc\",\"blocks\":[]}";

        await hub.UpdateDocument(samplePayload);

        mockClientProxy.Verify(
            c => c.SendCoreAsync("ReceiveDocumentUpdate", It.Is<object[]>(o => (string)o[0] == samplePayload), default),
            Times.Once);

        var latestDoc = await hub.GetLatestDocument();
        Assert.Equal(samplePayload, latestDoc);
    }

    [Fact]
    public async Task GetLatestDocument_ReturnsLatestUpdatedDocument()
    {
        var hub = new NotesHub();
        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Others).Returns(mockClientProxy.Object);
        hub.Clients = mockClients.Object;

        var docJson = "{\"title\":\"Sync Title\",\"blocks\":[{\"id\":\"b1\",\"content\":\"hello\"}]}";
        await hub.UpdateDocument(docJson);

        var result = await hub.GetLatestDocument();
        Assert.Equal(docJson, result);
    }

    [Fact]
    public async Task OnConnectedAsync_SendsLatestDocumentToCaller_WhenDocumentExists()
    {
        var hub = new NotesHub();
        var mockClients = new Mock<IHubCallerClients>();
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        var mockOthersProxy = new Mock<IClientProxy>();

        mockClients.Setup(c => c.Caller).Returns(mockCallerProxy.Object);
        mockClients.Setup(c => c.Others).Returns(mockOthersProxy.Object);
        hub.Clients = mockClients.Object;

        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns("conn_123");
        hub.Context = contextMock.Object;

        var testDoc = "{\"title\":\"Initial Note\",\"blocks\":[]}";
        await hub.UpdateDocument(testDoc);

        await hub.OnConnectedAsync();

        mockCallerProxy.Verify(
            c => c.SendCoreAsync("ReceiveDocumentUpdate", It.Is<object[]>(o => (string)o[0] == testDoc), default),
            Times.Once);
    }
}

