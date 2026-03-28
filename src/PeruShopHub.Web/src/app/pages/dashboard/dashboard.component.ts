import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, Plugin } from 'chart.js';
import { forkJoin } from 'rxjs';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent, MarginBadgeComponent, PageSkeletonComponent } from '../../shared/components';
import { DashboardService } from '../../services/dashboard.service';
import type { KpiCard, ProductRanking, PendingAction, ChartDataPoint, CostBreakdownItem } from '../../models/api.models';
import { formatBrl as formatBrlUtil } from '../../shared/utils';

type Period = 'hoje' | '7dias' | '30dias' | 'estemes' | 'personalizado';

interface KpiData {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

const COST_COLORS = [
  '#5C6BC0',
  '#42A5F5',
  '#66BB6A',
  '#FFA726',
  '#EF5350',
  '#AB47BC',
  '#26C6DA',
  '#BDBDBD',
];

const PERIOD_LABELS: Record<string, string> = {
  'hoje': 'vs dia anterior',
  '7dias': 'vs 7 dias anteriores',
  '30dias': 'vs 30 dias anteriores',
  'estemes': 'vs mês anterior',
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, BaseChartDirective, EmptyStateComponent, PageHeaderComponent, MarginBadgeComponent, PageSkeletonComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly router = inject(Router);

  activePeriod = signal<Period>('30dias');
  loading = signal(false);
  showDateRange = signal(false);

  periods: { key: Period; label: string }[] = [
    { key: 'hoje', label: 'Hoje' },
    { key: '7dias', label: '7 dias' },
    { key: '30dias', label: '30 dias' },
    { key: 'estemes', label: 'Este mês' },
    { key: 'personalizado', label: 'Personalizado' },
  ];

  kpis = signal<KpiData[]>([]);

  // Chart data
  lineChartData = signal<ChartData<'line'>>({
    labels: [],
    datasets: [
      {
        label: 'Receita Bruta',
        data: [],
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
        data: [],
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
  });

  // Donut chart data
  donutChartData = signal<ChartData<'doughnut'>>({
    labels: [],
    datasets: [
      {
        data: [],
        backgroundColor: COST_COLORS,
        borderWidth: 0,
        hoverOffset: 6,
      },
    ],
  });

  donutTotal = signal(0);

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

      const formatted = this.donutTotal().toLocaleString('pt-BR', {
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
  topProfitable = signal<ProductRanking[]>([]);
  leastProfitable = signal<ProductRanking[]>([]);
  pendingActions = signal<PendingAction[]>([]);

  ngOnInit(): void {
    this.loadData();
  }

  selectPeriod(period: Period): void {
    if (period === 'personalizado') {
      this.showDateRange.update(v => !v);
      if (!this.showDateRange()) return;
    } else {
      this.showDateRange.set(false);
    }

    this.activePeriod.set(period);
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    const period = this.activePeriod();
    const effectivePeriod = period === 'personalizado' ? '30dias' : period;
    const days = effectivePeriod === 'hoje' ? 1 : effectivePeriod === '7dias' ? 7 : 30;

    forkJoin({
      summary: this.dashboardService.getSummary(effectivePeriod),
      chart: this.dashboardService.getRevenueProfit(days),
      costs: this.dashboardService.getCostBreakdown(effectivePeriod),
      top: this.dashboardService.getTopProducts(5, effectivePeriod),
      least: this.dashboardService.getLeastProfitable(5, effectivePeriod),
      pending: this.dashboardService.getPendingActions(),
    }).subscribe({
      next: ({ summary, chart, costs, top, least, pending }) => {
        this.mapKpis(summary.kpis, effectivePeriod);
        this.updateLineChart(chart);
        this.updateDonutChart(costs);
        this.topProfitable.set(top);
        this.leastProfitable.set(least);
        this.pendingActions.set(pending);
        this.hasData.set(summary.kpis.length > 0 || top.length > 0);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  private mapKpis(kpis: KpiCard[], period: string): void {
    const changeLabel = PERIOD_LABELS[period] ?? `vs período anterior`;
    const costKpis = ['Total Custos'];
    this.kpis.set(
      kpis.map(kpi => ({
        label: kpi.title,
        value: this.formatKpiValue(kpi),
        change: kpi.changePercent ?? 0,
        changeLabel,
        invertColors: costKpis.includes(kpi.title),
      }))
    );
  }

  private formatKpiValue(kpi: KpiCard): string {
    const numValue = parseFloat(kpi.value);
    if (isNaN(numValue)) return kpi.value;
    if (kpi.title === 'Vendas') return kpi.value;
    if (kpi.title === 'Margem Media') return `${numValue.toFixed(1)}%`;
    return numValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  private updateLineChart(data: ChartDataPoint[]): void {
    this.lineChartData.set({
      labels: data.map(d => d.label),
      datasets: [
        {
          label: 'Receita Bruta',
          data: data.map(d => d.value),
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
          data: data.map(d => d.secondaryValue ?? 0),
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
    });
  }

  private updateDonutChart(costs: CostBreakdownItem[]): void {
    const total = costs.reduce((sum, c) => sum + c.total, 0);
    this.donutTotal.set(total);
    this.donutChartData.set({
      labels: costs.map(c => c.category),
      datasets: [
        {
          data: costs.map(c => c.total),
          backgroundColor: costs.map((c, i) => c.color ?? COST_COLORS[i % COST_COLORS.length]),
          borderWidth: 0,
          hoverOffset: 6,
        },
      ],
    });
  }

  formatBrl = formatBrlUtil;

  onProductClick(id: string): void {
    this.router.navigate(['/produtos', id]);
  }

  onConnectMarketplace(): void {
    this.router.navigate(['/configuracoes']);
  }

  getPendingActionVariant(action: PendingAction): string {
    switch (action.type) {
      case 'question': return 'accent';
      case 'order': return 'warning';
      case 'stock_alert': return 'danger';
      default: return 'accent';
    }
  }

  toggleEmptyState(): void {
    this.hasData.update(v => !v);
  }
}
