import { Component, output } from '@angular/core';

@Component({
  selector: 'app-header',
  imports: [],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class Header {
  onAddProduct = output<void>();
  onExport = output<void>();

  createPost() {
    this.onAddProduct.emit();
  }

  exportXlsx() {
    this.onExport.emit();
  }
}
