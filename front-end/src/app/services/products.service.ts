import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiListResponse, Product } from '../models/product';

// In dev, requests are proxied via proxy.conf.json
const BASE_URL = '';

export interface GetProductsParams {
  q?: string;
  category?: string;
  sort?: string; // e.g. price:asc, price:desc
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private http = inject(HttpClient);

  getProducts(params: GetProductsParams = {}): Observable<ApiListResponse<Product>> {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') {
        httpParams = httpParams.set(k, String(v));
      }
    });

    return this.http
      .get<Product[] | ApiListResponse<Product>>(`${BASE_URL}/api/products`, { params: httpParams })
      .pipe(
        map((res: Product[] | ApiListResponse<Product>): ApiListResponse<Product> => {
          if (Array.isArray(res)) {
            // Backend returned all products without meta -> client-side meta
            const page = params.page ?? 1;
            const pageSize = params.pageSize ?? res.length;
            const totalCount = res.length;
            return {
              data: res,
              meta: {
                totalCount,
                page,
                pageSize,
                totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
              },
            };
          }
          return res as ApiListResponse<Product>;
        })
      );
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${BASE_URL}/api/products/${id}`);
  }

  addProduct(payload: Omit<Product, 'id'>): Observable<Product> {
    return this.http.post<Product>(`${BASE_URL}/api/products`, payload);
  }

  updateProduct(id: number, payload: Omit<Product, 'id'>): Observable<Product> {
    return this.http.put<Product>(`${BASE_URL}/api/products/${id}`, payload);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/api/products/${id}`);
  }

  exportXlsx(): Observable<Blob> {
    return this.http.get(`${BASE_URL}/api/products/export`, {
      responseType: 'blob',
    });
  }
}
