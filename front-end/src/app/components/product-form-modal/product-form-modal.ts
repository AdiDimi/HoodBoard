import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  signal,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Product } from '../../models/generated/models/product';
import { ProductsService } from '../../models/generated/api/products.service';
import { CreateProductDto } from '../../models/generated/models/createProductDto';
import { UpdateProductDto } from '../../models/generated/models/updateProductDto';

@Component({
  selector: 'app-product-form-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './product-form-modal.html',
  styleUrl: './product-form-modal.scss',
})
export class ProductFormModalComponent implements OnChanges {
  @Input() product: Product | null = null; // edit mode if provided
  @Output() close = new EventEmitter<void>();
  @Output() created = new EventEmitter<Product>();
  @Output() updated = new EventEmitter<Product>();

  submitting = signal(false);
  error = signal<string | null>(null);

  form: any;

  selectedFiles: File[] | null = [];

  constructor(private fb: FormBuilder, private svc: ProductsService, private http: HttpClient) {
    // reinitialize form in constructor to satisfy strict initialization order
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(120)]],
      description: ['', [Validators.maxLength(1000)]],
      price: [0, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      category: ['', Validators.required],
      imageUrl: [''],
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['product']) {
      if (this.product) {
        const { id, ...rest } = this.product;
        // Sanitize nulls coming from API to avoid template/.length issues
        const safe = {
          name: (rest as any).name ?? '',
          description: (rest as any).description ?? '',
          price: (rest as any).price ?? 0,
          stock: (rest as any).stock ?? 0,
          category: (rest as any).category ?? '',
          imageUrl: (rest as any).imageUrl ?? '',
        };
        this.form.reset(safe);
      } else {
        this.form.reset({
          name: '',
          description: '',
          price: 0,
          stock: 0,
          category: '',
          imageUrl: '',
        });
      }
    }
  }

  get title(): string {
    return this.product ? 'Edit Product' : 'Add Product';
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    const dto = this.form.getRawValue();
    console.log('ProductFormModal.submit - DTO before normalize:', dto, {
      imageUrlType: typeof dto?.imageUrl,
      imageUrlValue: dto?.imageUrl,
    });
    // Normalize imageUrl: if empty/whitespace, omit it from the payload so backend doesn't get "imageUrl: ''"
    if (dto && typeof dto.imageUrl === 'string' && dto.imageUrl.trim() === '') {
      // Prefer undefined (JSON.stringify omits undefined) rather than empty string
      dto.imageUrl = undefined as any; // omit on wire
    }
    console.log('ProductFormModal.submit - DTO after normalize:', dto);
    if (this.product) {
      const payload: UpdateProductDto = dto as UpdateProductDto;
      this.svc
        .apiProductsIdPut({ id: String(this.product.id), updateProductDto: payload })
        .subscribe({
          next: async () => {
            try {
              const finalProduct = await this.afterSaveUpload(String(this.product!.id));
              this.updated.emit(finalProduct);
            } catch (e: any) {
              this.error.set(e?.message ?? 'Image upload failed');
            } finally {
              this.submitting.set(false);
            }
          },
          error: (err: any) => {
            this.error.set(err?.message ?? 'Failed to update');
            this.submitting.set(false);
          },
        });
    } else {
      const payload: CreateProductDto = dto as CreateProductDto;
      this.svc.apiProductsPost({ createProductDto: payload }).subscribe({
        next: async (p: Product) => {
          try {
            const finalProduct = await this.afterSaveUpload(String(p.id));
            this.created.emit(finalProduct);
          } catch (e: any) {
            this.error.set(e?.message ?? 'Image upload failed');
          } finally {
            this.submitting.set(false);
          }
        },
        error: (err: any) => {
          this.error.set(err?.message ?? 'Failed to create');
          this.submitting.set(false);
        },
      });
    }
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    this.selectedFiles = files;
  }

  private async afterSaveUpload(id: string): Promise<Product> {
    // If no files selected, just refetch the product to sync read-only fields
    if (!this.selectedFiles || this.selectedFiles.length === 0) {
      return (await this.svc.apiProductsIdGet({ id }).toPromise()) as Product;
    }

    // 1) Upload images (multipart/form-data). Our interceptors won't set JSON content type for FormData
    const form = new FormData();
    for (const f of this.selectedFiles) {
      form.append('files', f, f.name);
    }
    await this.http.post(`/api/products/${id}/photos`, form).toPromise();

    // 2) Update imageUrl to `${productId}.${ext}` based on the first selected file
    try {
      const first = this.selectedFiles[0];
      const ext = this.getExtensionFromFile(first);
      const imageUrl = `${id}.${ext}`;
      await this.svc.apiProductsIdPut({ id, updateProductDto: { imageUrl } }).toPromise();
    } catch {
      // Non-fatal: continue even if imageUrl update fails
    }

    // 3) Refetch the product to get updated photos/imageUrl
    const updated = await this.svc.apiProductsIdGet({ id }).toPromise();
    return updated as Product;
  }

  private getExtensionFromFile(file: File): string {
    const name = file?.name || '';
    const dot = name.lastIndexOf('.');
    if (dot !== -1 && dot < name.length - 1) {
      const ext = name.substring(dot + 1).toLowerCase();
      if (/^[a-z0-9]+$/.test(ext)) {
        return ext;
      }
    }
    const mime = (file?.type || '').toLowerCase();
    if (mime.includes('png')) return 'png';
    if (mime.includes('jpeg') || mime.includes('jpg')) return 'jpg';
    if (mime.includes('webp')) return 'webp';
    if (mime.includes('gif')) return 'gif';
    return 'jpg';
  }
}
