import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';

/**
 * Verbose debug interceptor that logs requests, responses, and errors.
 * Also avoids forcing Content-Type for multipart/Blob uploads.
 */
export const debugInterceptor: HttpInterceptorFn = (req, next) => {
  try {
    console.log('Debug interceptor - Request:', {
      method: req.method,
      url: req.url,
      headers: req.headers.keys().reduce((acc, key) => {
        acc[key] = req.headers.get(key);
        return acc;
      }, {} as Record<string, string | null>),
      bodyType: describeBody(req.body),
      body: req.body,
    });
  } catch (e) {
    console.error('Debug interceptor - Failed to log request', e);
  }

  // Don't force JSON Content-Type for FormData/Blob so boundaries are set correctly
  const isFormData = typeof FormData !== 'undefined' && req.body instanceof FormData;
  const isBlob = typeof Blob !== 'undefined' && req.body instanceof Blob;

  let modifiedReq = req;
  if (
    req.method.toUpperCase() === 'POST' &&
    !req.headers.has('Content-Type') &&
    !isFormData &&
    !isBlob
  ) {
    modifiedReq = req.clone({
      setHeaders: {
        'Content-Type': 'application/json',
      },
    });
    console.log('Debug interceptor - Added Content-Type: application/json to POST');
  }

  return next(modifiedReq).pipe(
    tap((event) => {
      if (event instanceof HttpResponse) {
        try {
          console.log('Debug interceptor - Response:', {
            url: modifiedReq.url,
            method: modifiedReq.method,
            status: event.status,
            statusText: event.statusText,
            headers: Array.from(event.headers.keys()).reduce((acc, key) => {
              acc[key] = event.headers.get(key);
              return acc;
            }, {} as Record<string, string | null>),
            bodyType: describeBody(event.body),
            body: event.body,
          });
        } catch (e) {
          console.error('Debug interceptor - Failed to log response', e);
        }
      }
    }),
    catchError((error: any) => {
      if (error instanceof HttpErrorResponse) {
        try {
          console.error('Debug interceptor - Response Error:', {
            url: modifiedReq.url,
            method: modifiedReq.method,
            status: error.status,
            statusText: error.statusText,
            message: error.message,
            error: error.error,
          });
        } catch (e) {
          console.error('Debug interceptor - Failed to log error', e);
        }
      } else {
        console.error('Debug interceptor - Unknown error:', error);
      }
      return throwError(() => error);
    })
  );
};

// Helpers
import { catchError, tap } from 'rxjs/operators';
import { throwError } from 'rxjs';

function describeBody(body: any): string {
  if (body == null) return 'null';
  if (body instanceof FormData) return 'FormData';
  if (body instanceof Blob) return `Blob(${body.type || 'unknown'})`;
  if (Array.isArray(body)) return `Array(${body.length})`;
  if (typeof body === 'object') return 'Object';
  return typeof body;
}

// (Removed tapSafe/catchErrorSafe to preserve proper typing)
