import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, type LucideIconData } from 'lucide-angular';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
})
export class ButtonComponent {
  @Input() variant: 'accent' | 'ghost' | 'outline' | 'danger' | 'icon' = 'accent';
  @Input() size: 'sm' | 'md' = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() icon?: LucideIconData;
  @Input() iconOnly = false;
  @Input() type: 'button' | 'submit' = 'button';
  @Input() ariaLabel?: string;

  get isDisabled(): boolean {
    return this.disabled || this.loading;
  }
}
