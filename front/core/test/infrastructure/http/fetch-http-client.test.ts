import { describe, expect, it, vi } from "vitest";
import { FetchHttpClient, HttpError } from "../../../src/infrastructure";

const baseUrl = "http://api.test";

function fakeFetch(status: number, body: string) {
  return vi.fn<typeof fetch>().mockResolvedValue(new Response(body || null, { status }));
}

describe("FetchHttpClient", () => {
  it("sends the bearer token when one exists", async () => {
    // Arrange
    const fetch = fakeFetch(200, "{}");
    const client = new FetchHttpClient({ baseUrl, fetch, getAccessToken: async () => "jwt" });

    // Act
    await client.request({ method: "GET", path: "/workouts" });

    // Assert
    const [, init] = fetch.mock.calls[0]!;
    expect(init?.headers).toMatchObject({ Authorization: "Bearer jwt" });
  });

  it("omits the authorization header when there is no token", async () => {
    // Arrange
    const fetch = fakeFetch(200, "{}");
    const client = new FetchHttpClient({ baseUrl, fetch, getAccessToken: async () => null });

    // Act
    await client.request({ method: "GET", path: "/workouts" });

    // Assert
    const [, init] = fetch.mock.calls[0]!;
    expect(init?.headers).not.toHaveProperty("Authorization");
  });

  it("sends the body as JSON", async () => {
    // Arrange
    const fetch = fakeFetch(200, "{}");
    const client = new FetchHttpClient({ baseUrl, fetch });

    // Act
    await client.request({ method: "POST", path: "/auth/login", body: { email: "a@b.c" } });

    // Assert
    const [url, init] = fetch.mock.calls[0]!;
    expect(url).toBe("http://api.test/auth/login");
    expect(init).toMatchObject({
      method: "POST",
      body: '{"email":"a@b.c"}',
      headers: { "Content-Type": "application/json" },
    });
  });

  it("returns the parsed response", async () => {
    // Arrange
    const client = new FetchHttpClient({ baseUrl, fetch: fakeFetch(200, '{"id":"1"}') });

    // Act
    const result = await client.request<{ id: string }>({ method: "GET", path: "/workouts/1" });

    // Assert
    expect(result).toEqual({ id: "1" });
  });

  it("throws an HttpError with the parsed body on a non-2xx response", async () => {
    // Arrange
    const client = new FetchHttpClient({
      baseUrl,
      fetch: fakeFetch(404, '{"detail":"Not found"}'),
    });

    // Act
    const act = client.request({ method: "GET", path: "/workouts/1" });

    // Assert
    await expect(act).rejects.toEqual(new HttpError(404, { detail: "Not found" }));
  });

  it("throws an HttpError when a 2xx body is not JSON", async () => {
    // Arrange
    const client = new FetchHttpClient({ baseUrl, fetch: fakeFetch(200, "not json") });

    // Act
    const act = client.request({ method: "GET", path: "/workouts" });

    // Assert
    await expect(act).rejects.toMatchObject({ status: 200, body: "not json" });
  });
});
