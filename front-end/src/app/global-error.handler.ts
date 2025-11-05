import { ErrorHandler, Injectable } from '@angular/core';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: any): void {
    // Centralized error logging with full stack
    try {
      console.error('GlobalErrorHandler - Caught error:', error);
      if (error instanceof Error) {
        console.error('GlobalErrorHandler - Stack:', error.stack);
        // Heuristic to surface common null.length cases
        if (error.message && error.message.includes("reading 'length'")) {
          console.warn(
            'Hint: A template or code path is reading .length on a null/undefined value.'
          );
        }
      }
    } catch (e) {
      // Ensure we never throw from the error handler
      // eslint-disable-next-line no-console
      console.error('GlobalErrorHandler - Failed to log error', e);
    }
  }
}
