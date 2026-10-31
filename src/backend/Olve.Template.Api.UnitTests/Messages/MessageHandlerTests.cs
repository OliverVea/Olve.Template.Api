using Olve.Template.Api.Contract;
using Olve.Template.Api.Messages;
using Olve.Utilities.Ids;
using Olve.Utilities.Stores;
using Message = Olve.Template.Api.Messages.Message;

namespace Olve.Template.Api.UnitTests.Messages;

public class MessageHandlerTests
{
    private static Message Seed(EntityStore<Message> store, string text)
    {
        var message = new Message(Id.New<Message>(), text);
        store.Set(message);
        return message;
    }

    private static MessageRequest Body(string text) => new() { Text = text };

    [Test]
    public async Task List_PagesFromOne()
    {
        var store = new EntityStore<Message>([]);
        Seed(store, "a");
        Seed(store, "b");
        Seed(store, "c");
        var handler = new ListMessagesHandler(store);

        var result = await handler.HandleAsync(new MessagesListRequest(1, 2), CancellationToken.None);

        var page = ((MessagesListResponse.Ok)result).Body;
        await Assert.That(page.Items.Count).IsEqualTo(2);
        await Assert.That(page.TotalPages).IsEqualTo(2);
        await Assert.That(page.HasNextPage).IsTrue();
    }

    [Test]
    public async Task Create_StoresMessageWithGeneratedId()
    {
        var store = new EntityStore<Message>([]);
        var handler = new CreateMessageHandler(store);

        var result = await handler.HandleAsync(new MessagesCreateRequest(Body("hello")), CancellationToken.None);

        await Assert.That(((MessagesCreateResponse.Ok)result).Body.Text).IsEqualTo("hello");
        await Assert.That(store.List().Count).IsEqualTo(1);
    }

    [Test]
    public async Task Create_WhitespaceText_AnswersBadRequest()
    {
        var store = new EntityStore<Message>([]);
        var handler = new CreateMessageHandler(store);

        var result = await handler.HandleAsync(new MessagesCreateRequest(Body("   ")), CancellationToken.None);

        await Assert.That(result).IsTypeOf<MessagesCreateResponse.BadRequest>();
        await Assert.That(store.List().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Update_ExistingMessage_ChangesText()
    {
        var store = new EntityStore<Message>([]);
        var existing = Seed(store, "before");
        var handler = new UpdateMessageHandler(store);

        var result = await handler.HandleAsync(new MessagesUpdateRequest(existing.Id.Value.Value, Body("after")), CancellationToken.None);

        await Assert.That(((MessagesUpdateResponse.Ok)result).Body.Text).IsEqualTo("after");
    }

    [Test]
    public async Task Update_MissingMessage_AnswersNotFound()
    {
        var store = new EntityStore<Message>([]);
        var handler = new UpdateMessageHandler(store);

        var result = await handler.HandleAsync(new MessagesUpdateRequest(Guid.NewGuid(), Body("x")), CancellationToken.None);

        await Assert.That(result).IsTypeOf<MessagesUpdateResponse.NotFound>();
    }

    [Test]
    public async Task Delete_ExistingMessage_Succeeds()
    {
        var store = new EntityStore<Message>([]);
        var existing = Seed(store, "bye");
        var handler = new DeleteMessageHandler(store);

        var result = await handler.HandleAsync(new MessagesDeleteRequest(existing.Id.Value.Value), CancellationToken.None);

        await Assert.That(result).IsTypeOf<MessagesDeleteResponse.Ok>();
        await Assert.That(store.Contains(existing.Id)).IsFalse();
    }

    [Test]
    public async Task Delete_MissingMessage_AnswersNotFound()
    {
        var store = new EntityStore<Message>([]);
        var handler = new DeleteMessageHandler(store);

        var result = await handler.HandleAsync(new MessagesDeleteRequest(Guid.NewGuid()), CancellationToken.None);

        await Assert.That(result).IsTypeOf<MessagesDeleteResponse.NotFound>();
    }
}
