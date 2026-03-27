import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, type LucideIconData } from 'lucide-angular';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, ButtonComponent],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
  @Input() backRoute?: string;
  @Input() actionLabel?: string;
  @Input() actionIcon?: LucideIconData;
  @Output() action = new EventEmitter<void>();
  @Output() backClick = new EventEmitter<void>();

  readonly arrowLeftIcon = ArrowLeft;

  get showBack(): boolean {
    return !!this.backRoute || this.backClick.observed;
  }
}
