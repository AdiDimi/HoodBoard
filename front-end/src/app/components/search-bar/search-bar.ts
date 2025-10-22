import { Component, EventEmitter, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search-bar',
  imports: [FormsModule],
  templateUrl: './search-bar.html',
  styleUrl: './search-bar.scss',
})
export class SearchBar {
  query = '';
  @Output() search = new EventEmitter<string>();

  emitSearch() {
    this.search.emit(this.query.trim());
  }
}
