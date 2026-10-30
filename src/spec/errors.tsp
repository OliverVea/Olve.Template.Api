import "@typespec/http";

using Http;

namespace Service;

/** One problem of a failed request. A failure answers an array of them. */
model Problem {
  message: string;
  tags: string[];
  severity: int32;
  isRetryable: boolean;
  source: string | null;
  exceptionSummary: string | null;
}

@error
model BadRequest {
  @statusCode status: 400;
  @body problems: Problem[];
}

@error
model NotFound {
  @statusCode status: 404;
  @body problems: Problem[];
}
