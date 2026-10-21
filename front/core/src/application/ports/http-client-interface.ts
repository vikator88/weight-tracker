import type { HttpRequest } from "./http-request";

/** Transport used by repository implementations. Implementations throw `HttpError` on a non-2xx response. */
export default interface HttpClientInterface {
  request<T>(request: HttpRequest): Promise<T>;
}
