import { DomainError, type DomainErrorCode } from "../../domain";
import HttpError from "./http-error";

const codeByStatus: Record<number, DomainErrorCode> = {
  400: "VALIDATION",
  401: "UNAUTHORIZED",
  403: "FORBIDDEN",
  404: "NOT_FOUND",
  409: "CONFLICT",
};

/**
 * The single place that turns a transport failure into a `DomainError`.
 * A 2xx `HttpError` means the body could not be read, and maps to `UNEXPECTED` like any unlisted status.
 * Anything that is not an `HttpError` means `fetch` itself rejected: there was no response.
 */
export default function mapHttpError(error: unknown): DomainError {
  if (error instanceof HttpError) {
    return new DomainError(codeByStatus[error.status] ?? "UNEXPECTED", detailOf(error));
  }

  return new DomainError("NETWORK", error instanceof Error ? error.message : String(error));
}

function detailOf(error: HttpError): string {
  const body = error.body;

  if (
    typeof body === "object" &&
    body !== null &&
    "detail" in body &&
    typeof body.detail === "string"
  ) {
    return body.detail;
  }

  return error.message;
}
