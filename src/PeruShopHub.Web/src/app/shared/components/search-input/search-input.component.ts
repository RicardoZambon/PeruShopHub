import { Component, Input, Output, EventEmitter } from '@angular/core';
import { LucideAngularModule, Search } from 'lucide-angular';

@Component({
  selector: 'app-search-input',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './search-input.component.html',
  styleUrl: './search-input.component.scss',
})
export class SearchInputComponent {
  @Input() placeholder = 'Buscar...';
  @Input() value = '';
  @Output() valueChange = new EventEmitter<string>();

  readonly searchIcon = Search;

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.valueChange.emit(input.value);
  }
}
