import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-variant-manager',
  standalone: true,
  imports: [CommonModule],
  template: `<p>Variant manager placeholder</p>`,
  styles: [``],
})
export class VariantManagerComponent {
  @Input() categoryId: string | null = null;
  @Input() productSku: string = 'PROD';
  @Input() productId: string = '';
}
