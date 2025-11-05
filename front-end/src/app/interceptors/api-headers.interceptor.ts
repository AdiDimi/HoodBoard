import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

/**
 * HTTP Interceptor that adds:
 * 1. Content-Type: application/json for all requests with body
 * 2. Idempotency-Key header for state-changing requests (POST/PUT/PATCH/DELETE)
 */
export const apiHeadersInterceptor: HttpInterceptorFn = (req, next) => {
  const stateChangingMethods = ['POST', 'PUT', 'PATCH', 'DELETE'];
  const isStateChanging = stateChangingMethods.includes(req.method.toUpperCase());

  let modifiedRequest = req;

  // Add Content-Type: application/json if request has body and no explicit Content-Type
  // Skip for FormData (multipart) or Blob uploads so the browser sets proper boundaries
  const isFormData = typeof FormData !== 'undefined' && req.body instanceof FormData;
  const isBlob = typeof Blob !== 'undefined' && req.body instanceof Blob;
  if (req.body && !req.headers.has('Content-Type') && !isFormData && !isBlob) {
    modifiedRequest = modifiedRequest.clone({
      setHeaders: {
        'Content-Type': 'application/json',
      },
    });
  }

  // Add Idempotency-Key for state-changing requests
  if (isStateChanging && !req.headers.has('Idempotency-Key')) {
    const idempotencyKey = generateIdempotencyKey();
    modifiedRequest = modifiedRequest.clone({
      headers: modifiedRequest.headers.set('Idempotency-Key', idempotencyKey),
    });
  }

  return next(modifiedRequest);
};

/**
 * Generate a unique idempotency key using timestamp + random string
 */
function generateIdempotencyKey(): string {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 15);
  return `${timestamp}-${random}`;
}
