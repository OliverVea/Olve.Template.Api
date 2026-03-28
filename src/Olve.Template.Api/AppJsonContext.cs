using System.Text.Json.Serialization;
using Olve.Results;

namespace Olve.Template.Api;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Result<string>))]
[JsonSerializable(typeof(ResultProblem[]))]
internal partial class AppJsonContext : JsonSerializerContext;
