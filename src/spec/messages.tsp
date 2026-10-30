import "@typespec/http";
import "./errors.tsp";

using Http;

namespace Service;

model Message {
  @format("uuid") id: string;
  text: string;
}

/** The body for creating or updating a message. */
model MessageRequest {
  @minLength(1) @maxLength(280) text: string;
}

/** One page of messages; `pageNumber` is 1-based. */
model MessagePage {
  items: Message[];
  pageNumber: int32;
  pageSize: int32;
  totalCount: int32;
  totalPages: int32;
  hasNextPage: boolean;
}

@route("/api/messages")
interface Messages {
  /** A page of messages, oldest first: `page` from 1 (default), `pageSize` 1 to 100 (default 20). */
  @get list(@query page?: int32, @query pageSize?: int32): MessagePage;

  @post create(@body body: MessageRequest): Message | BadRequest;

  @route("{id}")
  @put update(@format("uuid") @path id: string, @body body: MessageRequest): Message | BadRequest | NotFound;

  @route("{id}")
  @delete delete(@format("uuid") @path id: string): OkResponse | NotFound;
}
