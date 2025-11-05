import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Product } from '../../models/generated/models/product';
import { ProductsService } from '../../models/generated/api/products.service';
import { CreateProductDto } from '../../models/generated/models/createProductDto';
import { UpdateProductDto } from '../../models/generated/models/updateProductDto';

@Component({
  selector: 'app-product-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule],
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

  constructor(private fb: FormBuilder, private svc: ProductsService) {
    // reinitialize form in constructor to satisfy strict initialization order
    this.form = this.fb.nonNullable.group({
      name: ['', [Validators.required, Validators.maxLength(120)]],
      description: ['', [Validators.maxLength(1000)]],
      price: [0, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      category: ['', Validators.required],
      imageUrl: ['', [Validators.required]],
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['product']) {
      if (this.product) {
        const { id, ...rest } = this.product;
        this.form.reset(rest);
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
    if (this.product) {
      const payload: UpdateProductDto = dto as UpdateProductDto;
      this.svc
        .apiProductsIdPut({ id: String(this.product.id), updateProductDto: payload })
        .subscribe({
          next: (p: Product) => {
            this.updated.emit(p);
            this.submitting.set(false);
          },
          error: (err: any) => {
            this.error.set(err?.message ?? 'Failed to update');
            this.submitting.set(false);
          },
        });
    } else {
      const payload: CreateProductDto = dto as CreateProductDto;
      this.svc.apiProductsPost({ createProductDto: payload }).subscribe({
        next: (p: Product) => {
          this.created.emit(p);
          this.submitting.set(false);
        },
        error: (err: any) => {
          this.error.set(err?.message ?? 'Failed to create');
          this.submitting.set(false);
        },
      });
    }
  }
}
