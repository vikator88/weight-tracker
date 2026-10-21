/** Raised by `FetchHttpClient` on a non-2xx response or an unreadable body. Always caught by a repository. */
export default class HttpError extends Error {
  constructor(
    readonly status: number,
    readonly body: unknown,
  ) {
    super(`HTTP ${status}`);
    this.name = "HttpError";
  }
}
