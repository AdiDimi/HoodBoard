import { Component, input, effect, signal, output } from '@angular/core';
import { PostCard } from '../post-card/post-card';
import { PaginationComponent } from '../pagination/pagination';
import { AdsService } from '../../services/ads.service';
import { Ad } from '../../models/generated/models/ad';

@Component({
  selector: 'app-post-board',
  imports: [PostCard, PaginationComponent],
  templateUrl: './post-board.html',
  styleUrl: './post-board.scss',
})
export class PostBoard {
  // Input as a signal so effects react when the parent updates it on Go
  query = input<string>('');

  onEditPost = output<string>(); // Emit ad ID for editing
  onDeletePost = output<string>(); // Emit ad ID for deletion

  loading = signal(false);
  error = signal<string | null>(null);
  ads = signal<Ad[]>([]);

  // Pagination state
  currentPage = signal(1);
  totalPages = signal(1);
  totalItems = signal(0);
  pageSize = 12;

  // Expose ads for parent components
  getAds() {
    return this.ads();
  }

  constructor(private adsService: AdsService) {
    effect(() => {
      const q = this.query();
      this.currentPage.set(1); // Reset to first page when query changes
      this.fetch({ q, page: 1 });
    });
  }

  private fetch({ q, page = 1 }: { q?: string; page?: number }) {
    this.loading.set(true);
    this.error.set(null);
    this.adsService
      .searchAds({
        q,
        page,
        pageSize: this.pageSize,
        sort: 'createdAt:desc',
      })
      .subscribe({
        next: (res) => {
          this.ads.set(res.data ?? []);
          this.currentPage.set(page);

          // Extract pagination metadata from response
          if (res.meta) {
            this.totalItems.set(res.meta.totalCount || 0);
            this.totalPages.set(res.meta.totalPages || 1);
          } else {
            // Fallback if no meta information
            const totalCount = res.data?.length || 0;
            this.totalItems.set(totalCount);
            this.totalPages.set(Math.max(1, Math.ceil(totalCount / this.pageSize)));
          }

          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.message ?? 'Failed to load ads');
          this.loading.set(false);
        },
      });
  }

  refreshData() {
    this.fetch({ q: this.query(), page: this.currentPage() });
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.fetch({ q: this.query(), page });
    }
  }

  nextPage() {
    this.goToPage(this.currentPage() + 1);
  }

  previousPage() {
    this.goToPage(this.currentPage() - 1);
  }

  onEditPostClicked(adId: string) {
    console.log('PostBoard onEditPostClicked:', adId);
    this.onEditPost.emit(adId);
  }

  onDeletePostClicked(adId: string) {
    console.log('PostBoard onDeletePostClicked:', adId);
    this.onDeletePost.emit(adId);
  }

  onPageChange(page: number) {
    this.goToPage(page);
  }

  dirFor(ad: Ad): 'ltr' | 'rtl' {
    const text = `${ad.title ?? ''} ${ad.body ?? ''}`;
    // Basic check for Hebrew/Arabic ranges without using Unicode property escapes
    const rtlRegex = /[\u0590-\u05FF\u0600-\u06FF]/;
    return rtlRegex.test(text) ? 'rtl' : 'ltr';
  }
}
