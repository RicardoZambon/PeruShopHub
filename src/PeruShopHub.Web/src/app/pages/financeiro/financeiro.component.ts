import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
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

type SortField = 'vendas' | 'receita' | 'cmv' | 'comissoes' | 'frete' | 'impostos' | 'lucro' | 'margem';
type SortDir = 'asc' | 'desc';

interface ReconciliationRow {
  periodo: string;
  valorEsperado: number;
  valorDepositado: number;
  diferenca: number;
  status: 'OK' | 'Divergência';
}

interface AbcProduct {
  rank: number;
  produto: string;
  sku: string;
  lucro: number;
  percentLucro: number;
  classificacao: 'A' | 'B' | 'C';
}

interface SkuProfitability {
  sku: string;
  produto: string;
  vendas: number;
  receita: number;
  cmv: number;
  comissoes: number;
  frete: number;
  impostos: number;
  lucro: number;
  margem: number;
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
  imports: [CommonModule, KpiCardComponent, SkeletonComponent, BadgeComponent, BaseChartDirective],
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

  // SKU Profitability tab data
  private skuData: SkuProfitability[] = [
    { sku: 'PSH-001', produto: 'Fone Bluetooth TWS Pro', vendas: 89, receita: 8010.00, cmv: 3560.00, comissoes: 880.00, frete: 534.00, impostos: 480.60, lucro: 2555.40, margem: 31.9 },
    { sku: 'PSH-002', produto: 'Capa iPhone 15 Silicone', vendas: 215, receita: 6450.00, cmv: 1290.00, comissoes: 709.50, frete: 645.00, impostos: 387.00, lucro: 3418.50, margem: 53.0 },
    { sku: 'PSH-003', produto: 'Carregador USB-C 65W', vendas: 64, receita: 5760.00, cmv: 2880.00, comissoes: 633.60, frete: 460.80, impostos: 345.60, lucro: 1440.00, margem: 25.0 },
    { sku: 'PSH-004', produto: 'Suporte Notebook Alumínio', vendas: 31, receita: 4030.00, cmv: 2418.00, comissoes: 443.30, frete: 564.20, impostos: 241.80, lucro: 362.70, margem: 9.0 },
    { sku: 'PSH-005', produto: 'Película Galaxy S24 Ultra', vendas: 178, receita: 3560.00, cmv: 890.00, comissoes: 391.60, frete: 356.00, impostos: 213.60, lucro: 1708.80, margem: 48.0 },
    { sku: 'PSH-006', produto: 'Hub USB-C 7 em 1', vendas: 22, receita: 3960.00, cmv: 2376.00, comissoes: 435.60, frete: 396.00, impostos: 237.60, lucro: 514.80, margem: 13.0 },
    { sku: 'PSH-007', produto: 'Mouse Gamer RGB 12000DPI', vendas: 45, receita: 5850.00, cmv: 3510.00, comissoes: 643.50, frete: 877.50, impostos: 351.00, lucro: 468.00, margem: 8.0 },
    { sku: 'PSH-008', produto: 'Cabo HDMI 2.1 3m', vendas: 92, receita: 2760.00, cmv: 1840.00, comissoes: 303.60, frete: 414.00, impostos: 165.60, lucro: 36.80, margem: -1.3 },
  ];

  skuSortField = signal<SortField>('lucro');
  skuSortDir = signal<SortDir>('desc');

  sortedSkuData = computed(() => {
    const field = this.skuSortField();
    const dir = this.skuSortDir();
    return [...this.skuData].sort((a, b) => {
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

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  // Conciliação tab data
  conciliacaoLoading = signal(true);
  reconciliationData: ReconciliationRow[] = [
    { periodo: 'Jan/2026', valorEsperado: 48650.00, valorDepositado: 48650.00, diferenca: 0, status: 'OK' },
    { periodo: 'Fev/2026', valorEsperado: 52340.00, valorDepositado: 51890.00, diferenca: -450.00, status: 'Divergência' },
    { periodo: 'Mar/2026', valorEsperado: 41200.00, valorDepositado: 41200.00, diferenca: 0, status: 'OK' },
    { periodo: 'Abr/2026', valorEsperado: 55780.00, valorDepositado: 55780.00, diferenca: 0, status: 'OK' },
    { periodo: 'Mai/2026', valorEsperado: 47900.00, valorDepositado: 47120.00, diferenca: -780.00, status: 'Divergência' },
    { periodo: 'Jun/2026', valorEsperado: 50100.00, valorDepositado: 50100.00, diferenca: 0, status: 'OK' },
  ];

  getConciliacaoStatusVariant(status: string): BadgeVariant {
    return status === 'OK' ? 'success' : 'warning';
  }

  // Curva ABC tab data
  abcLoading = signal(true);
  private abcProducts: AbcProduct[] = (() => {
    const products = [
      { produto: 'Capa iPhone 15 Silicone', sku: 'PSH-002', lucro: 3418.50 },
      { produto: 'Fone Bluetooth TWS Pro', sku: 'PSH-001', lucro: 2555.40 },
      { produto: 'Película Galaxy S24 Ultra', sku: 'PSH-005', lucro: 1708.80 },
      { produto: 'Carregador USB-C 65W', sku: 'PSH-003', lucro: 1440.00 },
      { produto: 'Hub USB-C 7 em 1', sku: 'PSH-006', lucro: 514.80 },
      { produto: 'Mouse Gamer RGB 12000DPI', sku: 'PSH-007', lucro: 468.00 },
      { produto: 'Suporte Notebook Alumínio', sku: 'PSH-004', lucro: 362.70 },
      { produto: 'Cabo HDMI 2.1 3m', sku: 'PSH-008', lucro: 36.80 },
      { produto: 'Teclado Mecânico Compact', sku: 'PSH-009', lucro: 289.50 },
      { produto: 'Webcam Full HD 1080p', sku: 'PSH-010', lucro: 198.30 },
    ].sort((a, b) => b.lucro - a.lucro);

    const totalLucro = products.reduce((sum, p) => sum + p.lucro, 0);
    let cumulative = 0;

    return products.map((p, i) => {
      const percentLucro = (p.lucro / totalLucro) * 100;
      cumulative += percentLucro;
      let classificacao: 'A' | 'B' | 'C';
      if (cumulative <= 80) classificacao = 'A';
      else if (cumulative <= 95) classificacao = 'B';
      else classificacao = 'C';

      return {
        rank: i + 1,
        produto: p.produto,
        sku: p.sku,
        lucro: p.lucro,
        percentLucro: Math.round(percentLucro * 10) / 10,
        classificacao,
      };
    });
  })();

  abcData = this.abcProducts;

  // ABC horizontal bar chart
  abcChartData: ChartData<'bar'> = {
    labels: this.abcProducts.map(p => p.produto),
    datasets: [
      {
        label: 'Lucro (R$)',
        data: this.abcProducts.map(p => p.lucro),
        backgroundColor: this.abcProducts.map(p =>
          p.classificacao === 'A' ? '#2E7D32' : p.classificacao === 'B' ? '#F9A825' : '#EF5350'
        ),
        borderRadius: 3,
        barPercentage: 0.7,
        yAxisID: 'y',
      },
    ],
  };

  // Cumulative percentage line overlay on top of bar chart
  private abcCumulativeData: number[] = (() => {
    const total = this.abcProducts.reduce((s, p) => s + p.lucro, 0);
    let cum = 0;
    return this.abcProducts.map(p => {
      cum += p.lucro;
      return Math.round((cum / total) * 1000) / 10;
    });
  })();

  abcMixedChartData: ChartData = {
    labels: this.abcProducts.map(p => p.sku),
    datasets: [
      {
        type: 'bar',
        label: 'Lucro (R$)',
        data: this.abcProducts.map(p => p.lucro),
        backgroundColor: this.abcProducts.map(p =>
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
        data: this.abcCumulativeData,
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
  };

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

  constructor(private toastService: ToastService) {
    // Simulate loading for conciliação and ABC tabs
    setTimeout(() => this.conciliacaoLoading.set(false), 600);
    setTimeout(() => this.abcLoading.set(false), 600);
  }

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
