import { HttpInterceptorFn, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ETagCacheService } from '../services/etag-cache.service';

/**
 * HTTP Interceptor specifically for handling 304 Not Modified responses
 * This must run after the caching interceptor
 */
export const notModifiedInterceptor: HttpInterceptorFn = (req, next) => {
  const etagCache = inject(ETagCacheService);

  return next(req).pipe(
    catchError((error) => {
      // Handle 304 as a special case since Angular treats it as an error
      if (error instanceof HttpErrorResponse && error.status === 304) {
        const cachedData = etagCache.getCachedData(req.url);
        if (cachedData) {
          // Return cached data as a successful response
          return of(
            new HttpResponse({
              body: cachedData,
              headers: error.headers,
              status: 200,
              statusText: 'OK (from cache)',
              url: req.url,
            })
          );
        }
      }
      // Re-throw other errors
      return throwError(() => error);
    })
  );
};
