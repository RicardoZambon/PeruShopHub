import { HttpParams } from '@angular/common/http';

/**
 * Build HttpParams from a plain object, skipping null/undefined values.
 * Eliminates the repeated `if (x) httpParams = httpParams.set(...)` pattern.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function buildHttpParams(params: any): HttpParams {
  let httpParams = new HttpParams();
  if (!params || typeof params !== 'object') return httpParams;
  for (const [key, value] of Object.entries(params)) {
    if (value != null && value !== '') {
      httpParams = httpParams.set(key, String(value));
    }
  }
  return httpParams;
}
