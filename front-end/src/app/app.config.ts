import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { apiHeadersInterceptor } from './interceptors/api-headers.interceptor';
import { httpCachingInterceptor } from './interceptors/http-caching.interceptor';
import { notModifiedInterceptor } from './interceptors/not-modified.interceptor';
import { debugInterceptor } from './interceptors/debug.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withInterceptors([debugInterceptor])),
    // Temporarily disable complex interceptors to debug
    // withInterceptors([
    //   apiHeadersInterceptor,
    //   httpCachingInterceptor,
    //   notModifiedInterceptor
    // ]),
  ],
};
