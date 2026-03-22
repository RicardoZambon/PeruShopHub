import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, Plugin } from 'chart.js';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

type Period = 'hoje' | '7dias' | '30dias' | 'personalizado';

interface KpiData {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

interface ProductRow {
  id: number;
  name: string;
  sales: number;
  revenue: number;
  profit: number;
  margin: number;
}

interface PendingAction {
  label: string;
  count: number;
  variant: 'accent' | 'warning' | 'danger';
}

const MOCK_TOP_PROFITABLE: ProductRow[] = [
  { id: 1, name: 'Fone Bluetooth TWS Pro', sales: 42, revenue: 5880, profit: 1764, margin: 30.0 },
  { id: 2, name: 'Capinha iPhone 15 Silicone', sales: 89, revenue: 2670, profit: 748, margin: 28.0 },
  { id: 3, name: 'Carregador USB-C 65W GaN', sales: 35, revenue: 3150, profit: 819, margin: 26.0 },
  { id: 4, name: 'Película Vidro Samsung S24', sales: 67, revenue: 1340, profit: 335, margin: 25.0 },
  { id: 5, name: 'Suporte Celular Veicular', sales: 28, revenue: 1680, profit: 370, margin: 22.0 },
];

const MOCK_LEAST_PROFITABLE: ProductRow[] = [
  { id: 6, name: 'Hub USB-C 7 em 1', sales: 12, revenue: 1440, profit: -86, margin: -6.0 },
  { id: 7, name: 'Cabo HDMI 2.1 3m', sales: 31, revenue: 930, profit: -28, margin: -3.0 },
  { id: 8, name: 'Mouse Pad Gamer RGB XXL', sales: 18, revenue: 720, profit: 22, margin: 3.0 },
  { id: 9, name: 'Adaptador USB-C para P2', sales: 45, revenue: 675, profit: 47, margin: 7.0 },
  { id: 10, name: 'Fita LED RGB 5m WiFi', sales: 15, revenue: 1050, profit: 95, margin: 9.0 },
];

const MOCK_PENDING_ACTIONS: PendingAction[] = [
  { label: 'Perguntas sem resposta', count: 8, variant: 'accent' },
  { label: 'Pedidos pendentes', count: 3, variant: 'warning' },
  { label: 'Alertas', count: 2, variant: 'danger' },
];

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
  imports: [CommonModule, KpiCardComponent, SkeletonComponent, BaseChartDirective, EmptyStateComponent],
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

  // Donut chart data
  donutChartData: ChartData<'doughnut'> = {
    labels: [
      'Comissão ML',
      'Frete',
      'Custo Produto',
      'Embalagem',
      'Impostos',
      'Armazenagem',
      'Advertising',
      'Outros',
    ],
    datasets: [
      {
        data: [2850, 2100, 3200, 680, 1450, 520, 980, 340],
        backgroundColor: [
          '#5C6BC0',
          '#42A5F5',
          '#66BB6A',
          '#FFA726',
          '#EF5350',
          '#AB47BC',
          '#26C6DA',
          '#BDBDBD',
        ],
        borderWidth: 0,
        hoverOffset: 6,
      },
    ],
  };

  donutTotal = (this.donutChartData.datasets[0].data as number[]).reduce(
    (sum, v) => sum + v,
    0
  );

  donutCenterTextPlugin: Plugin<'doughnut'> = {
    id: 'donutCenterText',
    afterDraw: (chart) => {
      const { ctx, width, height } = chart;
      ctx.save();
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';

      const centerX = width / 2;
      const centerY = height / 2;

      ctx.font = '500 12px Inter';
      ctx.fillStyle = '#757575';
      ctx.fillText('Total Custos', centerX, centerY - 12);

      const formatted = this.donutTotal.toLocaleString('pt-BR', {
        style: 'currency',
        currency: 'BRL',
      });
      ctx.font = '700 18px Roboto Mono';
      ctx.fillStyle = '#212121';
      ctx.fillText(formatted, centerX, centerY + 12);

      ctx.restore();
    },
  };

  donutChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          usePointStyle: true,
          pointStyle: 'circle',
          padding: 12,
          font: { family: 'Inter', size: 12 },
          generateLabels: (chart) => {
            const data = chart.data;
            const dataset = data.datasets[0];
            const total = (dataset.data as number[]).reduce(
              (sum, v) => sum + v,
              0
            );
            return (data.labels ?? []).map((label, i) => ({
              text: `${label}  ${(
                ((dataset.data[i] as number) / total) *
                100
              ).toFixed(0)}%`,
              fillStyle: (dataset.backgroundColor as string[])[i],
              strokeStyle: 'transparent',
              index: i,
              hidden: false,
            }));
          },
        },
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleFont: { family: 'Inter', size: 13 },
        bodyFont: { family: 'Roboto Mono', size: 12 },
        padding: 12,
        callbacks: {
          label: (ctx) => {
            const value = ctx.parsed ?? 0;
            const total = (ctx.dataset.data as number[]).reduce(
              (sum, v) => sum + v,
              0
            );
            const pct = ((value / total) * 100).toFixed(1);
            const formatted = value.toLocaleString('pt-BR', {
              style: 'currency',
              currency: 'BRL',
            });
            return `${ctx.label}: ${formatted} (${pct}%)`;
          },
        },
      },
    },
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

  // Top products and pending actions
  hasData = signal(true);
  topProfitable = MOCK_TOP_PROFITABLE;
  leastProfitable = MOCK_LEAST_PROFITABLE;
  pendingActions = MOCK_PENDING_ACTIONS;

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

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  getMarginClass(margin: number): string {
    if (margin >= 20) return 'margin--green';
    if (margin >= 10) return 'margin--yellow';
    return 'margin--red';
  }

  onProductClick(id: number): void {
    console.log('Navigate to product:', id);
  }

  onConnectMarketplace(): void {
    console.log('Navigate to marketplace connection');
  }

  toggleEmptyState(): void {
    this.hasData.update(v => !v);
  }
}
