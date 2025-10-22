import { HttpInterceptorFn } from '@angular/common/http';

export const debugInterceptor: HttpInterceptorFn = (req, next) => {
  console.log('Debug interceptor - Request:', {
    method: req.method,
    url: req.url,
    headers: req.headers.keys().reduce((acc, key) => {
      acc[key] = req.headers.get(key);
      return acc;
    }, {} as Record<string, string | null>),
    body: req.body,
  });

  // Add Content-Type header for POST requests if not already present
  let modifiedReq = req;
  if (req.method === 'POST' && !req.headers.has('Content-Type')) {
    modifiedReq = req.clone({
      setHeaders: {
        'Content-Type': 'application/json',
      },
    });
    console.log('Added Content-Type header to POST request');
  }

  return next(modifiedReq);
};
