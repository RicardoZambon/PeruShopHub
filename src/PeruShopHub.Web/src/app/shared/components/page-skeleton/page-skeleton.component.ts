import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-page-skeleton',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './page-skeleton.component.html',
  styleUrl: './page-skeleton.component.scss',
})
export class PageSkeletonComponent {
  @Input() type: 'list' | 'detail' | 'kpi-grid' = 'list';
  @Input() rows = 6;

  get rowArray(): number[] {
    return Array.from({ length: this.rows }, (_, i) => i);
  }
}
