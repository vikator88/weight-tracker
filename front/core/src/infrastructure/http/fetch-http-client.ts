import type { HttpClientInterface, HttpRequest } from "../../application";
import type FetchHttpClientOptions from "./fetch-http-client-options";
import HttpError from "./http-error";

/** HttpClient backed by the fetch API, available in both browsers and React Native. */
export default class FetchHttpClient implements HttpClientInterface {
  constructor(private readonly options: FetchHttpClientOptions) {}

  async request<T>({ method, path, body, headers }: HttpRequest): Promise<T> {
    const doFetch = this.options.fetch ?? fetch;
    const token = await this.options.getAccessToken?.();

    const response = await doFetch(new URL(path, this.options.baseUrl).toString(), {
      method,
      headers: {
        Accept: "application/json",
        ...(body !== undefined && { "Content-Type": "application/json" }),
        ...(token && { Authorization: `Bearer ${token}` }),
        ...headers,
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });

    const text = await response.text();
    const parsed = parseJson(text);

    if (!response.ok) {
      throw new HttpError(response.status, parsed.ok ? parsed.value : text);
    }

    if (!parsed.ok) {
      throw new HttpError(response.status, text);
    }

    return parsed.value as T;
  }
}

function parseJson(text: string): { ok: true; value: unknown } | { ok: false } {
  if (!text) {
    return { ok: true, value: undefined };
  }

  try {
    return { ok: true, value: JSON.parse(text) };
  } catch {
    return { ok: false };
  }
}
