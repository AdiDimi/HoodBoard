import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ETagCacheService } from '../services/etag-cache.service';

/**
 * HTTP Interceptor that implements:
 * - ETag/If-Match for PUT requests
 * - If-None-Match for GET requests with 304 handling
 * - X-Request-ID correlation header
 */
export const httpCachingInterceptor: HttpInterceptorFn = (req, next) => {
  const etagCache = inject(ETagCacheService);

  // Generate unique request ID for correlation
  const requestId = generateUUID();

  let modifiedRequest = req.clone({
    setHeaders: {
      'X-Request-ID': requestId,
    },
  });

  const method = req.method.toUpperCase();
  const url = req.url;

  // Handle ETag caching based on HTTP method
  if (method === 'GET') {
    // Add If-None-Match for GET requests
    const cachedETag = etagCache.getETag(url);
    if (cachedETag) {
      modifiedRequest = modifiedRequest.clone({
        setHeaders: {
          ...modifiedRequest.headers,
          'If-None-Match': cachedETag,
        },
      });
    }
  } else if (method === 'PUT') {
    // Add If-Match for PUT requests
    const cachedETag = etagCache.getETag(url);
    if (cachedETag) {
      modifiedRequest = modifiedRequest.clone({
        setHeaders: {
          ...modifiedRequest.headers,
          'If-Match': cachedETag,
        },
      });
    }
  }

  return next(modifiedRequest).pipe(
    tap((event) => {
      if (event instanceof HttpResponse) {
        const etag = event.headers.get('ETag');

        // Don't handle 304 in tap - it's already handled by the server
        // Just cache ETags for successful responses
        if (etag && event.status === 200) {
          if (method === 'GET' && event.body) {
            etagCache.setCachedResponse(url, etag, event.body);
          } else if (['PUT', 'POST', 'PATCH'].includes(method)) {
            // Update ETag cache for state-changing operations
            etagCache.updateETag(url, etag);
          }
        }
      }
    })
  );
};

/**
 * Generate a UUID v4 for request correlation
 */
function generateUUID(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
