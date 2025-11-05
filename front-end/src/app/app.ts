import { Component, signal, ViewChild } from '@angular/core';
import { Header } from './components/header/header';
import { SearchBar } from './components/search-bar/search-bar';
import { ProductBoardComponent } from './components/product-board/product-board';
import { ProductFormModalComponent } from './components/product-form-modal/product-form-modal';
import { ProductsService } from './models/generated/api/products.service';
import { Product } from './models/generated/models/product';

@Component({
  selector: 'app-root',
  imports: [Header, SearchBar, ProductBoardComponent, ProductFormModalComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  @ViewChild(ProductBoardComponent) productBoard!: ProductBoardComponent;

  protected readonly title = signal('hood-board');
  query = signal('');
  showCreateForm = signal(false);
  editingProduct = signal<Product | null>(null);

  constructor(private productsService: ProductsService) {}

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
    if (product) {
      this.showCreateForm.set(false);
      this.editingProduct.set(product);
    } else {
      console.log('Product not found for editing');
    }
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
    this.productsService.apiProductsIdDelete({ id: String(product.id) }).subscribe({
      next: () => {
        this.productBoard.onPageChange(this.productBoard.currentPage());
      },
      error: (err: any) => {
        console.error('Error deleting product:', err);
        alert('Failed to delete product: ' + (err?.message ?? 'Unknown error'));
      },
    });
  }

  onCancelForm() {
    this.showCreateForm.set(false);
    this.editingProduct.set(null);
  }

  exportXlsx() {
    this.productsService
      .apiProductsExportGet('body', false, {
        httpHeaderAccept:
          'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' as any,
      })
      .subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'products.xlsx';
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err: any) => alert('Export failed: ' + (err?.message ?? 'Unknown error')),
      });
  }
}
