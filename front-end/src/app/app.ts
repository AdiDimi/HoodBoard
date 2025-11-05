import { Component, signal, ViewChild } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { Header } from './components/header/header';
import { SearchBar } from './components/search-bar/search-bar';
import { ProductBoardComponent } from './components/product-board/product-board';
import { ProductFormModalComponent } from './components/product-form-modal/product-form-modal';
import { ProductsService } from './models/generated/api/products.service';
import { Product } from './models/generated/models/product';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from './components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-root',
  imports: [
    Header,
    SearchBar,
    ProductBoardComponent,
    ProductFormModalComponent,
    MatDialogModule,
    ConfirmDialogComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  @ViewChild(ProductBoardComponent) productBoard!: ProductBoardComponent;

  protected readonly title = signal('hood-board');
  query = signal('');
  showCreateForm = signal(false);
  editingProduct = signal<Product | null>(null);

  constructor(private productsService: ProductsService, private dialog: MatDialog) {}

  get showForm(): boolean {
    const shouldShow = this.showCreateForm() || this.editingProduct() !== null;
    console.log('App showForm getter:', {
      showCreateForm: this.showCreateForm(),
      editingProduct: this.editingProduct(),
      shouldShow,
    });
    return shouldShow;
  }

  get currentEditProduct(): Product | null {
    const p = this.editingProduct();
    console.log('App currentEditProduct getter:', p);
    return p;
  }

  onSearch(query: string) {
    this.query.set(query);
  }

  onAddProduct() {
    this.editingProduct.set(null); // Clear any edit mode
    this.showCreateForm.set(true);
  }

  onEditProduct(product: Product) {
    if (!product) {
      console.log('Product not found for editing');
      return;
    }
    this.showCreateForm.set(false);
    // Fetch the latest product to populate form and capture validator header (ETag/NTag)
    const id = String(product.id);
    this.productsService.apiProductsIdGet({ id }).subscribe({
      next: (fresh: Product) => {
        this.editingProduct.set(fresh);
      },
      error: (err: any) => {
        console.warn('Failed to GET product before edit; falling back to list item', err);
        this.editingProduct.set(product);
      },
    });
  }

  onProductCreated(newProd: Product) {
    this.showCreateForm.set(false);
    this.productBoard.onPageChange(1);
  }

  onProductUpdated(updated: Product) {
    this.editingProduct.set(null);
    this.productBoard.onPageChange(this.productBoard.currentPage());
  }

  onDeleteProduct(product: Product) {
    const data: ConfirmDialogData = {
      title: 'Delete product',
      message: `Are you sure you want to delete "${product.name}"? This cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
    };
    this.dialog
      .open(ConfirmDialogComponent, { data, disableClose: true })
      .afterClosed()
      .subscribe((confirmed: boolean) => {
        if (confirmed !== true) return;
        this.productsService.apiProductsIdDelete({ id: String(product.id) }).subscribe({
          next: () => {
            this.productBoard.onPageChange(this.productBoard.currentPage());
          },
          error: (err: any) => {
            console.error('Error deleting product:', err);
            alert('Failed to delete product: ' + (err?.message ?? 'Unknown error'));
          },
        });
      });
  }

  onCancelForm() {
    this.showCreateForm.set(false);
    this.editingProduct.set(null);
  }

  exportXlsx() {
    // Backend now returns CSV. Request response so we can access headers and filename.
    this.productsService
      .apiProductsExportGet('response', false, {
        // Force blob responseType in generated client (anything not text/json becomes 'blob')
        httpHeaderAccept: 'application/octet-stream' as any,
      })
      .subscribe({
        next: (resp: HttpResponse<any>) => {
          let blob: Blob | null = resp.body as Blob;
          // If generator delivered text due to server headers, wrap as CSV blob
          if (blob && typeof (blob as any) === 'string') {
            blob = new Blob([blob as any], { type: 'text/csv;charset=utf-8' });
          }
          if (!blob) {
            // Fallback: create empty CSV to avoid no-op click
            blob = new Blob([''], { type: 'text/csv;charset=utf-8' });
          }
          const cd =
            resp.headers.get('Content-Disposition') ||
            resp.headers.get('content-disposition') ||
            '';
          let filename = 'products.csv';
          try {
            const matchUtf8 = cd.match(/filename\*=UTF-8''([^;]+)/i);
            const matchSimple = cd.match(/filename="?([^;"]+)"?/i);
            if (matchUtf8 && matchUtf8[1]) {
              filename = decodeURIComponent(matchUtf8[1]);
            } else if (matchSimple && matchSimple[1]) {
              filename = matchSimple[1];
            }
            filename = filename.replace(/['"]/g, '');
          } catch {}

          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = filename || 'products.csv';
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err: any) => alert('Export failed: ' + (err?.message ?? 'Unknown error')),
      });
  }
}
