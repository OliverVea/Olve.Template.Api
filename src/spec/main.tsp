// The API contract: one file per area. Every operation needs a bearer token unless the service
// marks its route anonymous (ApiConfiguration.MapApi).
import "@typespec/http";
import "./errors.tsp";
import "./instance.tsp";
import "./messages.tsp";

using Http;

@service(#{ title: "Olve.Template.Api" })
@useAuth(BearerAuth)
namespace Service;
