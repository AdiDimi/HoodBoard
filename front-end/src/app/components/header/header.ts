import { Component, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatMenuModule],
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
