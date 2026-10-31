using Olve.Results;
using Olve.Template.Api.Configuration;
using Olve.TypeSpec;
using Olve.Utilities.Ids;
using Olve.Utilities.Stores;

namespace Olve.Template.Api.Messages;

// The template's one end-to-end CRUD feature: one handler class per contract operation, over an
// EntityStore<Message>. The store holds the domain Message; the wire shape is Contract.Message.
internal static class MessageMapping
{
    public static Contract.Message ToContract(this Message message) => new() { Id = message.Id.Value.Value, Text = message.Text };

    public static Id<Message> ToMessageId(this Guid id) => new(new Id(id));
}

[OperationHandler]
public sealed class ListMessagesHandler(EntityStore<Message> store) : Contract.IMessagesListHandler
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public Task<Contract.MessagesListResponse> HandleAsync(Contract.MessagesListRequest request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page ?? 1, 1);
        var pageSize = Math.Clamp(request.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var all = store.List();
        return Task.FromResult<Contract.MessagesListResponse>(new Contract.MessagePage
        {
            Items = [.. all.Skip((page - 1) * pageSize).Take(pageSize).Select(m => m.ToContract())],
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = all.Count,
            TotalPages = (int)Math.Ceiling(all.Count / (double)pageSize),
            HasNextPage = page * pageSize < all.Count,
        });
    }
}

/// <summary>Creates a <see cref="Message"/> with a freshly generated <see cref="Id{T}"/>.</summary>
[OperationHandler]
public sealed class CreateMessageHandler(EntityStore<Message> store) : Contract.IMessagesCreateHandler
{
    public Task<Contract.MessagesCreateResponse> HandleAsync(Contract.MessagesCreateRequest request, CancellationToken cancellationToken)
    {
        if (MessageRequestValidator.Validate(request.Body).TryPickProblems(out var problems))
        {
            return Task.FromResult<Contract.MessagesCreateResponse>(new Contract.MessagesCreateResponse.BadRequest(problems.ToContract()));
        }

        var message = new Message(Id.New<Message>(), request.Body.Text);
        store.Set(message);
        return Task.FromResult<Contract.MessagesCreateResponse>(message.ToContract());
    }
}

/// <summary>Updates an existing <see cref="Message"/>, or answers the store's not-found (404).</summary>
[OperationHandler]
public sealed class UpdateMessageHandler(EntityStore<Message> store) : Contract.IMessagesUpdateHandler
{
    public Task<Contract.MessagesUpdateResponse> HandleAsync(Contract.MessagesUpdateRequest request, CancellationToken cancellationToken)
    {
        if (MessageRequestValidator.Validate(request.Body).TryPickProblems(out var invalid))
        {
            return Task.FromResult<Contract.MessagesUpdateResponse>(new Contract.MessagesUpdateResponse.BadRequest(invalid.ToContract()));
        }

        var id = request.Id.ToMessageId();
        if (store.Mutate(id, message => message with { Text = request.Body.Text }).TryPickProblems(out var problems))
        {
            return Task.FromResult<Contract.MessagesUpdateResponse>(new Contract.MessagesUpdateResponse.NotFound(problems.ToContract()));
        }

        store.TryGet(id, out var updated);
        return Task.FromResult<Contract.MessagesUpdateResponse>(updated!.ToContract());
    }
}

/// <summary>Deletes a <see cref="Message"/> by id.</summary>
[OperationHandler]
public sealed class DeleteMessageHandler(EntityStore<Message> store) : Contract.IMessagesDeleteHandler
{
    public Task<Contract.MessagesDeleteResponse> HandleAsync(Contract.MessagesDeleteRequest request, CancellationToken cancellationToken)
    {
        if (store.Delete(request.Id.ToMessageId()).WasNotFound)
        {
            ResultProblem[] notFound = [new NotFoundProblem("Message with id '{0}' was not found.", request.Id)];
            return Task.FromResult<Contract.MessagesDeleteResponse>(new Contract.MessagesDeleteResponse.NotFound(notFound.ToContract()));
        }

        return Task.FromResult<Contract.MessagesDeleteResponse>(new Contract.MessagesDeleteResponse.Ok());
    }
}

/// <summary>The rule the contract's constraints can't state: a message isn't only whitespace.</summary>
public static class MessageRequestValidator
{
    public static Result Validate(Contract.MessageRequest request) =>
        string.IsNullOrWhiteSpace(request.Text) ? new ResultProblem("'text' cannot be empty.") : Result.Success();
}
