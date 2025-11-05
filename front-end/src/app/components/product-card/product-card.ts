import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { Product } from '../../models/generated/models/product';
import { Photo } from '../../models/generated/models/photo';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [MatCardModule, MatButtonModule],
  templateUrl: './product-card.html',
  styleUrl: './product-card.scss',
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;
  @Output() edit = new EventEmitter<Product>();
  @Output() delete = new EventEmitter<Product>();

  onEdit() {
    this.edit.emit(this.product);
  }

  onDelete() {
    this.delete.emit(this.product);
  }

  get priceLabel(): string {
    const price = this.product.price ?? 0;
    return `$${price.toFixed(2)}`;
  }

  get imgUrl(): string {
    const direct = 'http://localhost:8080/uploads/thumbs/' + (this.product.id ?? '1') + '.jpg';
    if (direct) return direct;

    return '';
  }

  get stockStatus(): 'low' | 'in' | 'out' {
    const stock = this.product.stock ?? 0;
    if (stock <= 0) return 'out';
    if (stock < 5) return 'low';
    return 'in';
  }
}
