import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';

// Import generated models from OpenAPI
import { Ad } from '../models/generated/models/ad';
import { Comment } from '../models/generated/models/comment';
import { Contact } from '../models/generated/models/contact';
import { Location } from '../models/generated/models/location';
import { Photo } from '../models/generated/models/photo';
import { CreateAdDto } from '../models/generated/models/createAdDto';
import { UpdateAdDto } from '../models/generated/models/updateAdDto';
import { CreateCommentDto } from '../models/generated/models/createCommentDto';

// Manual interface definitions removed - now using generated models from OpenAPI

export interface ApiListResponse<T> {
  data?: T[] | null;
  meta?: {
    totalCount?: number;
    page?: number;
    pageSize?: number;
    totalPages?: number;
  };
  links?: any;
}

// In dev, requests will be proxied to the backend via proxy.conf.json
const DEFAULT_BASE_URL = '';

@Injectable({ providedIn: 'root' })
export class AdsService {
  private http = inject(HttpClient);
  constructor() {}

  private baseUrl = DEFAULT_BASE_URL;

  searchAds(params: {
    q?: string;
    category?: string;
    minPrice?: number;
    maxPrice?: number;
    lat?: number;
    lng?: number;
    radiusKm?: number;
    page?: number;
    pageSize?: number;
    sort?: string;
  }): Observable<ApiListResponse<Ad>> {
    let httpParams = new HttpParams();
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== null && v !== '') {
        httpParams = httpParams.set(k, String(v));
      }
    }

    return this.http
      .get<ApiListResponse<Ad>>(`${this.baseUrl}/api/ads`, {
        params: httpParams,
        observe: 'response',
      })
      .pipe(
        map((response: HttpResponse<ApiListResponse<Ad>>) => {
          const data = response.body?.data || [];
          const totalCount = parseInt(response.headers.get('X-Total-Count') || '0');
          const page = parseInt(response.headers.get('X-Page') || '1');
          const pageSize = parseInt(response.headers.get('X-Page-Size') || '20');

          return {
            data,
            meta: {
              totalCount,
              page,
              pageSize,
              totalPages: Math.ceil(totalCount / pageSize),
            },
          };
        })
      );
  }

  getComments(adId: string): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.baseUrl}/api/ads/${encodeURIComponent(adId)}/comments`);
  }

  postComment(adId: string, payload: { authorName: string; text: string }): Observable<Comment> {
    const url = `${this.baseUrl}/api/ads/${encodeURIComponent(adId)}/comments`;
    console.log('AdsService.postComment - URL:', url);
    console.log('AdsService.postComment - payload:', payload);

    return this.http.post<Comment>(url, payload);
  }

  createAd(payload: {
    title: string;
    body: string;
    category?: string;
    price?: number;
    contact?: {
      name?: string;
      email?: string;
      phone?: string;
    };
    location?: {
      address?: string;
      lat?: number;
      lng?: number;
    };
    tags?: string[];
  }): Observable<Ad> {
    const url = `${this.baseUrl}/api/ads`;
    console.log('AdsService.createAd - URL:', url);
    console.log('AdsService.createAd - payload:', payload);

    return this.http.post<Ad>(url, payload);
  }

  updateAd(
    adId: string,
    payload: {
      title: string;
      body: string;
      category?: string;
      price?: number;
      contact?: {
        name?: string;
        email?: string;
        phone?: string;
      };
      location?: {
        address?: string;
        lat?: number;
        lng?: number;
      };
      tags?: string[];
    }
  ): Observable<Ad> {
    const url = `${this.baseUrl}/api/ads/${encodeURIComponent(adId)}`;
    console.log('AdsService.updateAd - URL:', url);
    console.log('AdsService.updateAd - payload:', payload);

    return this.http.put<Ad>(url, payload);
  }

  deleteAd(adId: string): Observable<void> {
    const url = `${this.baseUrl}/api/ads/${encodeURIComponent(adId)}`;
    console.log('AdsService.deleteAd - URL:', url);

    return this.http.delete<void>(url);
  }
}
