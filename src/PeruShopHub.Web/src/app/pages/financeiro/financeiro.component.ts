import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { ToastService } from '../../services/toast.service';

type Period = 'hoje' | '7dias' | '30dias' | 'personalizado';
type FinanceTab = 'resumo' | 'lucratividade' | 'conciliacao' | 'curva-abc';

interface KpiData {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

const MOCK_DATA: Record<Exclude<Period, 'personalizado'>, KpiData[]> = {
  hoje: [
    { label: 'Receita Bruta', value: 'R$ 2.150,00', change: 5.4, changeLabel: 'vs ontem' },
    { label: 'Total Custos', value: 'R$ 1.580,00', change: 3.2, changeLabel: 'vs ontem', invertColors: true },
    { label: 'Lucro Líquido', value: 'R$ 570,00', change: -3.8, changeLabel: 'vs ontem' },
    { label: 'Margem Média', value: '26,5%', change: -0.9, changeLabel: 'vs ontem' },
    { label: 'Ticket Médio', value: 'R$ 179,17', change: 2.1, changeLabel: 'vs ontem' },
  ],
  '7dias': [
    { label: 'Receita Bruta', value: 'R$ 11.280,00', change: 10.3, changeLabel: 'vs semana anterior' },
    { label: 'Total Custos', value: 'R$ 8.340,00', change: 7.8, changeLabel: 'vs semana anterior', invertColors: true },
    { label: 'Lucro Líquido', value: 'R$ 2.940,00', change: -1.5, changeLabel: 'vs semana anterior' },
    { label: 'Margem Média', value: '26,1%', change: -0.8, changeLabel: 'vs semana anterior' },
    { label: 'Ticket Médio', value: 'R$ 162,30', change: 4.2, changeLabel: 'vs semana anterior' },
  ],
  '30dias': [
    { label: 'Receita Bruta', value: 'R$ 48.650,00', change: 8.7, changeLabel: 'vs mês anterior' },
    { label: 'Total Custos', value: 'R$ 35.920,00', change: 6.1, changeLabel: 'vs mês anterior', invertColors: true },
    { label: 'Lucro Líquido', value: 'R$ 12.730,00', change: -2.1, changeLabel: 'vs mês anterior' },
    { label: 'Margem Média', value: '26,2%', change: -1.3, changeLabel: 'vs mês anterior' },
    { label: 'Ticket Médio', value: 'R$ 171,30', change: 3.5, changeLabel: 'vs mês anterior' },
  ],
};

function generateBarChartData(): { labels: string[]; revenue: number[]; profit: number[] } {
  const labels: string[] = [];
  const revenue: number[] = [];
  const profit: number[] = [];
  const now = new Date();

  for (let i = 29; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);
    labels.push(date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }));

    const baseRevenue = 1200 + Math.random() * 800;
    revenue.push(Math.round(baseRevenue));
    profit.push(Math.round(baseRevenue * (0.18 + Math.random() * 0.14)));
  }

  return { labels, revenue, profit };
}

function generateMarginChartData(): { labels: string[]; margin: number[] } {
  const labels: string[] = [];
  const margin: number[] = [];
  const now = new Date();

  for (let i = 29; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);
    labels.push(date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }));
    margin.push(Math.round((18 + Math.random() * 16 - 4) * 10) / 10);
  }

  return { labels, margin };
}

@Component({
  selector: 'app-financeiro',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, SkeletonComponent, BaseChartDirective],
  templateUrl: './financeiro.component.html',
  styleUrl: './financeiro.component.scss',
})
export class FinanceiroComponent {
  activePeriod = signal<Period>('30dias');
  activeTab = signal<FinanceTab>('resumo');
  loading = signal(false);
  showDateRange = signal(false);

  periods: { key: Period; label: string }[] = [
    { key: 'hoje', label: 'Hoje' },
    { key: '7dias', label: '7 dias' },
    { key: '30dias', label: '30 dias' },
    { key: 'personalizado', label: 'Personalizado' },
  ];

  tabs: { key: FinanceTab; label: string }[] = [
    { key: 'resumo', label: 'Resumo' },
    { key: 'lucratividade', label: 'Lucratividade por SKU' },
    { key: 'conciliacao', label: 'Conciliação' },
    { key: 'curva-abc', label: 'Curva ABC' },
  ];

  kpis = computed<KpiData[]>(() => {
    const period = this.activePeriod();
    if (period === 'personalizado') return MOCK_DATA['30dias'];
    return MOCK_DATA[period];
  });

  // Bar chart data
  private mockBarData = generateBarChartData();

  barChartData: ChartData<'bar'> = {
    labels: this.mockBarData.labels,
    datasets: [
      {
        label: 'Receita Bruta',
        data: this.mockBarData.revenue,
        backgroundColor: '#1A237E',
        borderRadius: 3,
        barPercentage: 0.7,
        categoryPercentage: 0.8,
      },
      {
        label: 'Lucro Líquido',
        data: this.mockBarData.profit,
        backgroundColor: '#2E7D32',
        borderRadius: 3,
        barPercentage: 0.7,
        categoryPercentage: 0.8,
      },
    ],
  };

  barChartOptions: ChartConfiguration<'bar'>['options'] = {
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
          pointStyle: 'rect',
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

  // Margin line chart data
  private mockMarginData = generateMarginChartData();

  marginChartData: ChartData<'line'> = {
    labels: this.mockMarginData.labels,
    datasets: [
      {
        label: 'Margem %',
        data: this.mockMarginData.margin,
        borderColor: '#1A237E',
        backgroundColor: 'rgba(26, 35, 126, 0.08)',
        fill: true,
        tension: 0.3,
        pointRadius: 2,
        pointHoverRadius: 5,
        borderWidth: 2,
      },
      {
        label: 'Meta (15%)',
        data: Array(30).fill(15),
        borderColor: '#EF5350',
        borderDash: [8, 4],
        borderWidth: 1.5,
        pointRadius: 0,
        pointHoverRadius: 0,
        fill: false,
      },
    ],
  };

  marginChartOptions: ChartConfiguration<'line'>['options'] = {
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
            return `${ctx.dataset.label}: ${value.toFixed(1)}%`;
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
        min: 0,
        max: 40,
        grid: { color: 'rgba(0, 0, 0, 0.06)' },
        ticks: {
          font: { family: 'Roboto Mono', size: 11 },
          callback: (value) => `${value}%`,
          stepSize: 5,
        },
      },
    },
  };

  constructor(private toastService: ToastService) {}

  selectPeriod(period: Period): void {
    if (period === 'personalizado') {
      this.showDateRange.update(v => !v);
      if (!this.showDateRange()) return;
    } else {
      this.showDateRange.set(false);
    }

    this.loading.set(true);
    this.activePeriod.set(period);
    setTimeout(() => this.loading.set(false), 600);
  }

  selectTab(tab: FinanceTab): void {
    this.activeTab.set(tab);
  }

  onExport(type: 'pdf' | 'excel'): void {
    this.toastService.show(
      `Exportação ${type === 'pdf' ? 'PDF' : 'Excel'} em desenvolvimento`,
      'info'
    );
  }
}
