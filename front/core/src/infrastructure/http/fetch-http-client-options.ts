export default interface FetchHttpClientOptions {
  baseUrl: string;
  /** Returns the current JWT, if any. Each platform decides where the token lives. */
  getAccessToken?: () => Promise<string | null>;
  fetch?: typeof fetch;
}
