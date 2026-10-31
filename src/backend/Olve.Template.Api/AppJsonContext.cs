using System.Text.Json.Serialization;
using Olve.Template.Api.Messages;
using Olve.Utilities.Ids;

namespace Olve.Template.Api;

// Source-generated JSON for AOT: the persisted store snapshot (the API's JSON context is generated
// from the contract). Id<Message> is registered explicitly so its closed generic is statically
// reachable here (the Id<T> converter is reflection-based — see docs/DESIGN.md §1.4a).
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(IReadOnlyList<Message>))]
[JsonSerializable(typeof(Id<Message>))]
internal partial class AppJsonContext : JsonSerializerContext;
