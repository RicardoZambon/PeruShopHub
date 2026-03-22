import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-categorias',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="categorias-page">
      <h1>Categorias</h1>
      <p>Página de categorias (em desenvolvimento por outro workstream)</p>
    </div>
  `,
  styles: [`
    .categorias-page {
      padding: var(--space-6);
    }
    h1 {
      font-size: var(--text-2xl);
      font-weight: 600;
      color: var(--neutral-900);
      margin: 0 0 var(--space-4) 0;
    }
    p {
      color: var(--neutral-600);
    }
  `],
})
export class CategoriasComponent {}
