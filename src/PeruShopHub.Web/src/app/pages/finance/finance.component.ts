import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { forkJoin } from 'rxjs';
import { formatBrl as formatBrlUtil } from '../../shared/utils';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { TabPanelsComponent, TabPanelDirective } from '../../shared/components/tab-panels/tab-panels.component';
import type { TabItem } from '../../shared/components/tab-bar/tab-bar.component';
import { MarginBadgeComponent } from '../../shared/components/margin-badge/margin-badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { ToastService } from '../../services/toast.service';
import { FinanceService } from '../../services/finance.service';
import type {
  KpiCard,
  ChartDataPoint,
  SkuProfitability,
  ReconciliationRow,
  AbcProduct,
} from '../../models/api.models';

type Period = 'hoje' | '7dias' | '30dias' | 'personalizado';
type FinanceTab = 'resumo' | 'lucratividade' | 'conciliacao' | 'curva-abc';

interface KpiData {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

type SortField = 'vendas' | 'receita' | 'cmv' | 'comissoes' | 'frete' | 'impostos' | 'lucro' | 'margem';
type SortDir = 'asc' | 'desc';

@Component({
  selector: 'app-finance',
  standalone: true,
  imports: [CommonModule, FormsModule, KpiCardComponent, SkeletonComponent, BadgeComponent, PageHeaderComponent, ButtonComponent, TabPanelsComponent, TabPanelDirective, MarginBadgeComponent, BaseChartDirective],
  templateUrl: './finance.component.html',
  styleUrl: './finance.component.scss',
})
export class FinanceComponent implements OnInit {
  private readonly financeService = inject(FinanceService);
  private readonly toastService = inject(ToastService);

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

  tabItems: TabItem[] = this.tabs.map(t => ({ key: t.key, label: t.label }));

  kpis = signal<KpiData[]>([]);

  // Bar chart data
  barChartData = signal<ChartData<'bar'>>({
    labels: [],
    datasets: [
      {
        label: 'Receita Bruta',
        data: [],
        backgroundColor: '#1A237E',
        borderRadius: 3,
        barPercentage: 0.7,
        categoryPercentage: 0.8,
      },
      {
        label: 'Lucro Líquido',
        data: [],
        backgroundColor: '#2E7D32',
        borderRadius: 3,
        barPercentage: 0.7,
        categoryPercentage: 0.8,
      },
    ],
  });

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
  marginChartData = signal<ChartData<'line'>>({
    labels: [],
    datasets: [
      {
        label: 'Margem %',
        data: [],
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
        data: [],
        borderColor: '#EF5350',
        borderDash: [8, 4],
        borderWidth: 1.5,
        pointRadius: 0,
        pointHoverRadius: 0,
        fill: false,
      },
    ],
  });

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

  // SKU Profitability tab data
  private skuData = signal<SkuProfitability[]>([]);
  skuLoading = signal(false);
  skuSearch = signal('');
  skuMinMargin = signal<number | null>(null);
  skuMaxMargin = signal<number | null>(null);
  refreshing = signal(false);

  skuSortField = signal<SortField>('lucro');
  skuSortDir = signal<SortDir>('desc');

  sortedSkuData = computed(() => {
    const field = this.skuSortField();
    const dir = this.skuSortDir();
    return [...this.skuData()].sort((a, b) => {
      const aVal = a[field];
      const bVal = b[field];
      return dir === 'asc' ? aVal - bVal : bVal - aVal;
    });
  });

  sortSku(field: SortField): void {
    if (this.skuSortField() === field) {
      this.skuSortDir.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.skuSortField.set(field);
      this.skuSortDir.set('desc');
    }
  }

  getMarginVariant(margem: number): BadgeVariant {
    if (margem >= 20) return 'success';
    if (margem >= 10) return 'warning';
    return 'danger';
  }

  getMarginBarWidth(margem: number): number {
    return Math.min(Math.max(Math.abs(margem), 0), 60);
  }

  formatBrl = formatBrlUtil;

  // Conciliação tab data
  conciliacaoLoading = signal(true);
  reconciliationData = signal<ReconciliationRow[]>([]);

  getConciliacaoStatusVariant(status: string): BadgeVariant {
    return status === 'OK' ? 'success' : 'warning';
  }

  // Curva ABC tab data
  abcLoading = signal(true);
  abcData = signal<AbcProduct[]>([]);

  abcMixedChartData = signal<ChartData>({
    labels: [],
    datasets: [],
  });

  abcChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'x',
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
            if (ctx.datasetIndex === 0) {
              const value = (ctx.parsed.y ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
              return `Lucro: ${value}`;
            }
            return `% Acumulado: ${ctx.parsed.y}%`;
          },
        },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        ticks: { font: { family: 'Roboto Mono', size: 11 } },
      },
      y: {
        position: 'left',
        grid: { color: 'rgba(0, 0, 0, 0.06)' },
        ticks: {
          font: { family: 'Roboto Mono', size: 11 },
          callback: (value) => `R$ ${Number(value).toLocaleString('pt-BR')}`,
        },
      },
      y1: {
        position: 'right',
        min: 0,
        max: 100,
        grid: { display: false },
        ticks: {
          font: { family: 'Roboto Mono', size: 11 },
          callback: (value) => `${value}%`,
          stepSize: 20,
        },
      },
    },
  };

  getAbcVariant(classificacao: string): BadgeVariant {
    if (classificacao === 'A') return 'success';
    if (classificacao === 'B') return 'warning';
    return 'danger';
  }

  ngOnInit(): void {
    this.loadSummaryData();
    this.loadSkuData();
    this.loadReconciliation();
    this.loadAbcCurve();
  }

  selectPeriod(period: Period): void {
    if (period === 'personalizado') {
      this.showDateRange.update(v => !v);
      if (!this.showDateRange()) return;
    } else {
      this.showDateRange.set(false);
    }

    this.activePeriod.set(period);
    this.loadSummaryData();
    this.loadSkuData();
  }

  onExport(type: 'pdf' | 'excel'): void {
    if (type === 'excel') {
      this.toastService.show('Exportação Excel em desenvolvimento', 'info');
      return;
    }

    const now = new Date();
    const period = this.activePeriod();
    const days = period === 'hoje' ? 1 : period === '7dias' ? 7 : 30;
    const dateFrom = new Date(now.getTime() - days * 86400000).toISOString().split('T')[0];
    const dateTo = now.toISOString().split('T')[0];

    this.financeService.exportProfitabilityPdf(dateFrom, dateTo).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `lucratividade_${dateFrom}_${dateTo}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.toastService.show('Erro ao gerar PDF', 'danger');
      },
    });
  }

  onSkuSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.skuSearch.set(value);
    this.loadSkuData();
  }

  onMinMarginChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.skuMinMargin.set(value ? parseFloat(value) : null);
    this.loadSkuData();
  }

  onMaxMarginChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.skuMaxMargin.set(value ? parseFloat(value) : null);
    this.loadSkuData();
  }

  refreshSkuProfitability(): void {
    this.refreshing.set(true);
    this.financeService.refreshSkuProfitability().subscribe({
      next: () => {
        this.toastService.show('Dados de lucratividade atualizados', 'success');
        this.refreshing.set(false);
        this.loadSkuData();
      },
      error: () => {
        this.toastService.show('Erro ao atualizar dados', 'danger');
        this.refreshing.set(false);
      },
    });
  }

  private loadSummaryData(): void {
    this.loading.set(true);
    const period = this.activePeriod();
    const effectivePeriod = period === 'personalizado' ? '30dias' : period;
    const days = effectivePeriod === 'hoje' ? 1 : effectivePeriod === '7dias' ? 7 : 30;

    forkJoin({
      kpis: this.financeService.getSummary(effectivePeriod),
      revenueProfit: this.financeService.getRevenueProfit(days),
      margin: this.financeService.getMarginChart(days),
    }).subscribe({
      next: ({ kpis, revenueProfit, margin }) => {
        this.kpis.set(kpis);
        this.updateBarChart(revenueProfit);
        this.updateMarginChart(margin);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  private loadSkuData(): void {
    this.skuLoading.set(true);
    const period = this.activePeriod();
    const effectivePeriod = period === 'personalizado' ? '30dias' : period;

    const params: Record<string, any> = { period: effectivePeriod };
    const search = this.skuSearch();
    if (search) params['search'] = search;
    const minMargin = this.skuMinMargin();
    if (minMargin !== null) params['minMargin'] = minMargin;
    const maxMargin = this.skuMaxMargin();
    if (maxMargin !== null) params['maxMargin'] = maxMargin;

    this.financeService.getSkuProfitability(params).subscribe({
      next: (data) => {
        this.skuData.set(data);
        this.skuLoading.set(false);
      },
      error: () => {
        this.skuLoading.set(false);
      },
    });
  }

  private loadReconciliation(): void {
    this.conciliacaoLoading.set(true);
    const currentYear = new Date().getFullYear();

    this.financeService.getReconciliation(currentYear).subscribe({
      next: (data) => {
        this.reconciliationData.set(data);
        this.conciliacaoLoading.set(false);
      },
      error: () => {
        this.conciliacaoLoading.set(false);
      },
    });
  }

  private loadAbcCurve(): void {
    this.abcLoading.set(true);

    this.financeService.getAbcCurve().subscribe({
      next: (data) => {
        this.abcData.set(data);
        this.updateAbcChart(data);
        this.abcLoading.set(false);
      },
      error: () => {
        this.abcLoading.set(false);
      },
    });
  }

  private updateBarChart(data: ChartDataPoint[]): void {
    this.barChartData.set({
      labels: data.map(d => d.label),
      datasets: [
        {
          label: 'Receita Bruta',
          data: data.map(d => d.value1),
          backgroundColor: '#1A237E',
          borderRadius: 3,
          barPercentage: 0.7,
          categoryPercentage: 0.8,
        },
        {
          label: 'Lucro Líquido',
          data: data.map(d => d.value2 ?? 0),
          backgroundColor: '#2E7D32',
          borderRadius: 3,
          barPercentage: 0.7,
          categoryPercentage: 0.8,
        },
      ],
    });
  }

  private updateMarginChart(data: ChartDataPoint[]): void {
    this.marginChartData.set({
      labels: data.map(d => d.label),
      datasets: [
        {
          label: 'Margem %',
          data: data.map(d => d.value1),
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
          data: Array(data.length).fill(15),
          borderColor: '#EF5350',
          borderDash: [8, 4],
          borderWidth: 1.5,
          pointRadius: 0,
          pointHoverRadius: 0,
          fill: false,
        },
      ],
    });
  }

  private updateAbcChart(products: AbcProduct[]): void {
    const totalLucro = products.reduce((sum, p) => sum + p.lucro, 0);
    let cumulative = 0;
    const cumulativeData = products.map(p => {
      cumulative += p.lucro;
      return Math.round((cumulative / totalLucro) * 1000) / 10;
    });

    this.abcMixedChartData.set({
      labels: products.map(p => p.sku),
      datasets: [
        {
          type: 'bar',
          label: 'Lucro (R$)',
          data: products.map(p => p.lucro),
          backgroundColor: products.map(p =>
            p.classificacao === 'A' ? '#2E7D32' : p.classificacao === 'B' ? '#F9A825' : '#EF5350'
          ),
          borderRadius: 3,
          barPercentage: 0.7,
          yAxisID: 'y',
          order: 2,
        },
        {
          type: 'line',
          label: '% Acumulado',
          data: cumulativeData,
          borderColor: '#1A237E',
          backgroundColor: 'rgba(26, 35, 126, 0.08)',
          borderWidth: 2,
          pointRadius: 4,
          pointBackgroundColor: '#1A237E',
          fill: false,
          tension: 0.3,
          yAxisID: 'y1',
          order: 1,
        },
      ],
    });
  }
}
