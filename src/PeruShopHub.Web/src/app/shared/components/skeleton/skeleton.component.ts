import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type SkeletonType = 'text' | 'rect' | 'circle';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="skeleton"
      [ngClass]="'skeleton--' + type"
      [style.width]="width"
      [style.height]="computedHeight"
    ></div>
  `,
  styleUrl: './skeleton.component.scss',
})
export class SkeletonComponent {
  @Input() type: SkeletonType = 'text';
  @Input() width = '100%';
  @Input() height?: string;

  get computedHeight(): string {
    if (this.height) return this.height;
    switch (this.type) {
      case 'text': return '16px';
      case 'rect': return '100px';
      case 'circle': return this.width;
    }
  }
}
