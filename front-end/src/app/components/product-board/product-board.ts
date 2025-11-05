import { Component, effect, input, output, signal } from '@angular/core';
import { Product } from '../../models/generated/models/product';
import { ProductsService } from '../../models/generated/api/products.service';
import { ProductCardComponent } from '../product-card/product-card';
import { PaginationComponent } from '../pagination/pagination';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-product-board',
  standalone: true,
  imports: [
    ProductCardComponent,
    PaginationComponent,
    FormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatOptionModule,
  ],
  templateUrl: './product-board.html',
  styleUrl: './product-board.scss',
})
export class ProductBoardComponent {
  query = input<string>('');
  onEdit = output<Product>();
  onDelete = output<Product>();

  loading = signal(false);
  error = signal<string | null>(null);
  products = signal<Product[]>([]);

  // UI filters
  category = signal<string>('All Categories');
  sort = signal<string>('');

  // Pagination
  currentPage = signal(1);
  totalPages = signal(1);
  totalItems = signal(0);
  pageSize = 12;

  categories = signal<string[]>([]);

  constructor(private productsSvc: ProductsService) {
    effect(() => {
      // When query changes reset to page 1
      const q = this.query();
      this.currentPage.set(1);
      this.fetch({ q, page: 1 });
    });
  }

  getProductsForParent() {
    return this.products();
  }

  private fetch({ q, page = 1 }: { q?: string; page?: number }) {
    this.loading.set(true);
    this.error.set(null);

    const category = this.category();
    const sort = this.sort();
    const apiCategory = category && category !== 'All Categories' ? category : undefined;

    this.productsSvc
      .apiProductsGet({ q, category: apiCategory, sort, page, pageSize: this.pageSize })
      .subscribe({
        next: (res: {
          data?: Product[] | null;
          meta?: { totalCount?: number; totalPages?: number } | null;
        }) => {
          let data = res.data ?? [];
          const qstr = (this.query() || '').toLowerCase().trim();
          if (qstr) {
            data = data.filter(
              (p: Product) =>
                (p.name ?? '').toLowerCase().includes(qstr) ||
                (p.category ?? '').toLowerCase().includes(qstr)
            );
          }
          // Optional client-side sort if API doesn't apply it
          // Category filter fallback
          const currentCat = this.category();
          if (currentCat && currentCat !== 'All Categories') {
            data = data.filter((p: Product) => p.category === currentCat);
          }

          const s = this.sort();
          if (s) {
            const [field, dir] = s.split(':');
            data = [...data].sort((a: Product, b: Product) => {
              const av = (a as any)[field];
              const bv = (b as any)[field];
              if (av === bv) return 0;
              const cmp = av > bv ? 1 : -1;
              return dir === 'desc' ? -cmp : cmp;
            });
          }
          this.products.set(data);
          this.currentPage.set(page);

          // derive categories for filter once
          if (!this.categories().length) {
            const set = new Set<string>();
            data.forEach((p: Product) => set.add(p.category ?? ''));
            this.categories.set(['All Categories', ...Array.from(set).sort()]);
          }

          if (res.meta) {
            this.totalItems.set(res.meta.totalCount ?? data.length);
            this.totalPages.set(res.meta.totalPages ?? 1);
          } else {
            const totalCount = data.length;
            this.totalItems.set(totalCount);
            this.totalPages.set(Math.max(1, Math.ceil(totalCount / this.pageSize)));
          }
          this.loading.set(false);
        },
        error: (err: any) => {
          this.error.set(err?.message ?? 'Failed to load products');
          this.loading.set(false);
        },
      });
  }

  onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.fetch({ q: this.query(), page });
    }
  }

  onCategoryChange() {
    this.currentPage.set(1);
    this.fetch({ q: this.query(), page: 1 });
  }

  onSortChange() {
    this.currentPage.set(1);
    this.fetch({ q: this.query(), page: 1 });
  }
}
