import { Component, Input, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipService } from '../../../services/tooltip.service';

@Component({
  selector: 'app-contextual-tooltip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './contextual-tooltip.component.html',
  styleUrl: './contextual-tooltip.component.scss',
})
export class ContextualTooltipComponent implements OnInit {
  @Input({ required: true }) tooltipId!: string;
  @Input({ required: true }) message!: string;

  private readonly tooltipService = inject(TooltipService);
  visible = signal(false);

  ngOnInit(): void {
    this.visible.set(!this.tooltipService.isDismissed(this.tooltipId));
  }

  dismiss(): void {
    this.tooltipService.dismiss(this.tooltipId);
    this.visible.set(false);
  }
}
