import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
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

function generateMockChartData(): { labels: string[]; revenue: number[]; profit: number[] } {
  const labels: string[] = [];
  const revenue: number[] = [];
  const profit: number[] = [];
  const now = new Date();

  for (let i = 29; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);
    labels.push(date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }));

    const baseRevenue = 500 + Math.random() * 400;
    revenue.push(Math.round(baseRevenue * 100) / 100);
    profit.push(Math.round(baseRevenue * (0.18 + Math.random() * 0.12) * 100) / 100);
  }

  return { labels, revenue, profit };
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, SkeletonComponent, BaseChartDirective],
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

  // Chart data
  private mockChart = generateMockChartData();

  lineChartData: ChartData<'line'> = {
    labels: this.mockChart.labels,
    datasets: [
      {
        label: 'Receita Bruta',
        data: this.mockChart.revenue,
        borderColor: '#1A237E',
        backgroundColor: 'rgba(26, 35, 126, 0.1)',
        fill: true,
        tension: 0.3,
        pointRadius: 2,
        pointHoverRadius: 5,
        borderWidth: 2,
      },
      {
        label: 'Lucro Líquido',
        data: this.mockChart.profit,
        borderColor: '#2E7D32',
        backgroundColor: 'rgba(46, 125, 50, 0.1)',
        fill: true,
        tension: 0.3,
        borderDash: [6, 3],
        pointRadius: 2,
        pointHoverRadius: 5,
        borderWidth: 2,
      },
    ],
  };

  lineChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index',
      intersect: false,
    },
    plugins: {
      legend: {
        position: 'top',
        align: 'start',
        labels: {
          usePointStyle: true,
          pointStyle: 'line',
          padding: 16,
          font: { family: 'Inter', size: 13 },
        },
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleFont: { family: 'Inter', size: 13 },
        bodyFont: { family: 'Roboto Mono', size: 12 },
        padding: 12,
        callbacks: {
          label: (ctx) => {
            const value = ctx.parsed.y ?? 0;
            const formatted = value.toLocaleString('pt-BR', {
              style: 'currency',
              currency: 'BRL',
            });
            return `${ctx.dataset.label}: ${formatted}`;
          },
        },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        ticks: {
          font: { family: 'Inter', size: 11 },
          maxTicksLimit: 10,
        },
      },
      y: {
        grid: { color: 'rgba(0, 0, 0, 0.06)' },
        ticks: {
          font: { family: 'Roboto Mono', size: 11 },
          callback: (value) => `R$ ${Number(value).toLocaleString('pt-BR')}`,
        },
      },
    },
  };

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
