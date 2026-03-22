import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';

type Period = 'hoje' | '7dias' | '30dias' | 'personalizado';

interface KpiData {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

const MOCK_DATA: Record<Exclude<Period, 'personalizado'>, KpiData[]> = {
  hoje: [
    { label: 'Vendas', value: '12', change: 8.2, changeLabel: 'vs ontem' },
    { label: 'Receita Bruta', value: 'R$ 2.150,00', change: 5.4, changeLabel: 'vs ontem' },
    { label: 'Lucro Líquido', value: 'R$ 486,30', change: -3.8, changeLabel: 'vs ontem' },
    { label: 'Margem Média', value: '22,6%', change: -0.9, changeLabel: 'vs ontem' },
  ],
  '7dias': [
    { label: 'Vendas', value: '78', change: 12.1, changeLabel: 'vs semana anterior' },
    { label: 'Receita Bruta', value: 'R$ 11.280,00', change: 10.3, changeLabel: 'vs semana anterior' },
    { label: 'Lucro Líquido', value: 'R$ 2.640,20', change: -1.5, changeLabel: 'vs semana anterior' },
    { label: 'Margem Média', value: '23,4%', change: -0.8, changeLabel: 'vs semana anterior' },
  ],
  '30dias': [
    { label: 'Vendas', value: '127', change: 15.3, changeLabel: 'vs mês anterior' },
    { label: 'Receita Bruta', value: 'R$ 18.432,00', change: 8.7, changeLabel: 'vs mês anterior' },
    { label: 'Lucro Líquido', value: 'R$ 4.210,50', change: -2.1, changeLabel: 'vs mês anterior' },
    { label: 'Margem Média', value: '22,8%', change: -1.3, changeLabel: 'vs mês anterior' },
  ],
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, SkeletonComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  activePeriod = signal<Period>('30dias');
  loading = signal(false);
  showDateRange = signal(false);

  periods: { key: Period; label: string }[] = [
    { key: 'hoje', label: 'Hoje' },
    { key: '7dias', label: '7 dias' },
    { key: '30dias', label: '30 dias' },
    { key: 'personalizado', label: 'Personalizado' },
  ];

  kpis = computed<KpiData[]>(() => {
    const period = this.activePeriod();
    if (period === 'personalizado') return MOCK_DATA['30dias'];
    return MOCK_DATA[period];
  });

  selectPeriod(period: Period): void {
    if (period === 'personalizado') {
      this.showDateRange.update(v => !v);
      if (!this.showDateRange()) return;
    } else {
      this.showDateRange.set(false);
    }

    this.loading.set(true);
    this.activePeriod.set(period);

    // Simulate loading
    setTimeout(() => this.loading.set(false), 600);
  }
}
