import { Injectable } from '@angular/core';

export interface CachedResponse {
  etag: string;
  data: any;
  timestamp: number;
}

@Injectable({
  providedIn: 'root',
})
export class ETagCacheService {
  private cache = new Map<string, CachedResponse>();

  // TTL for cached responses (5 minutes default)
  private readonly TTL = 5 * 60 * 1000;

  /**
   * Get cached ETag for a URL
   */
  getETag(url: string): string | null {
    const cached = this.cache.get(url);
    if (cached && this.isValidCache(cached)) {
      return cached.etag;
    }
    return null;
  }

  /**
   * Get cached response data for 304 handling
   */
  getCachedData(url: string): any | null {
    const cached = this.cache.get(url);
    if (cached && this.isValidCache(cached)) {
      return cached.data;
    }
    return null;
  }

  /**
   * Store ETag and response data
   */
  setCachedResponse(url: string, etag: string, data: any): void {
    this.cache.set(url, {
      etag,
      data,
      timestamp: Date.now(),
    });
  }

  /**
   * Update ETag for a URL (for PUT responses)
   */
  updateETag(url: string, etag: string): void {
    const existing = this.cache.get(url);
    if (existing) {
      existing.etag = etag;
      existing.timestamp = Date.now();
    } else {
      this.cache.set(url, {
        etag,
        data: null,
        timestamp: Date.now(),
      });
    }
  }

  /**
   * Clear cache entry
   */
  clearCache(url: string): void {
    this.cache.delete(url);
  }

  /**
   * Clear all cache entries
   */
  clearAllCache(): void {
    this.cache.clear();
  }

  private isValidCache(cached: CachedResponse): boolean {
    return Date.now() - cached.timestamp < this.TTL;
  }
}
