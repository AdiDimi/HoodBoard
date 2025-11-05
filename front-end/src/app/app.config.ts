import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { apiHeadersInterceptor } from './interceptors/api-headers.interceptor';
import { BASE_PATH } from './models/generated/variables';
import { httpCachingInterceptor } from './interceptors/http-caching.interceptor';
import { notModifiedInterceptor } from './interceptors/not-modified.interceptor';
import { debugInterceptor } from './interceptors/debug.interceptor';
import { ErrorHandler } from '@angular/core';
import { GlobalErrorHandler } from './global-error.handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideClientHydration(withEventReplay()),
    provideHttpClient(
      withInterceptors([
        debugInterceptor,
        apiHeadersInterceptor,
        httpCachingInterceptor,
        notModifiedInterceptor,
      ])
    ),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    // Point generated API clients to relative base path so Angular proxy forwards to backend
    { provide: BASE_PATH, useValue: '' },
  ],
};
