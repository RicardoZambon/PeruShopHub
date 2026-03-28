import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, Edit } from 'lucide-angular';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import {
  KpiCardComponent,
  BadgeComponent,
  SelectDropdownComponent,
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
  PageHeaderComponent,
} from '../../shared/components';
import type { BadgeVariant, SelectOption, GridColumn } from '../../shared/components';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { ProductVariantService } from '../../services/product-variant.service';
import { ProductService } from '../../services/product.service';
import type { CostHistoryItem } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { PricingService } from '../../services/pricing.service';
import type { PriceCalculationResult, PricingRule } from '../../services/pricing.service';
import type { ProductVariant } from '../../models/product-variant.model';
import { formatBrl as formatBrlUtil, formatDateShort } from '../../shared/utils';

interface ProductDetail {
  id: string;
  name: string;
  sku: string;
  description: string | null;
  categoryId: string | null;
  categoryPath: string | null;
  supplier: string | null;
  price: number;
  purchaseCost: number;
  packagingCost: number;
  status: string;
  statusVariant: BadgeVariant;
  imageUrl: string | null;
  photoUrls: string[];
  weight: number;
  height: number;
  width: number;
  length: number;
  stock: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    LucideAngularModule,
    KpiCardComponent,
    BadgeComponent,
    BrlCurrencyPipe,
    SelectDropdownComponent,
    DataGridComponent,
    GridCellDirective,
    GridCardDirective,
    PageHeaderComponent,
    BaseChartDirective,
  ],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
})
export class ProductDetailComponent implements OnInit {
  readonly ArrowLeft = ArrowLeft;
  readonly Package = Package;
  readonly EditIcon = Edit;

  loading = signal(true);
  product = signal<ProductDetail | null>(null);
  costHistory = signal<CostHistoryItem[]>([]);
  costHistoryTotalCount = signal(0);
  variants = signal<ProductVariant[]>([]);

  // Analytics state
  analyticsDays = signal(30);
  analyticsLoading = signal(false);
  analyticsData = signal<{
    totalSales: number;
    totalRevenue: number;
    totalProfit: number;
    margin: number | null;
    salesChange: number | null;
    revenueChange: number | null;
    profitChange: number | null;
    marginChange: number | null;
  } | null>(null);

  // Recent orders state
  recentOrders = signal<Record<string, any>[]>([]);
  recentOrdersTotalCount = signal(0);
  recentOrdersLoading = signal(false);

  dateRangeOptions: SelectOption[] = [
    { value: '7', label: '7 dias' },
    { value: '30', label: '30 dias' },
    { value: '60', label: '60 dias' },
    { value: '90', label: '90 dias' },
    { value: '180', label: '180 dias' },
    { value: '365', label: '1 ano' },
  ];

  // Column definitions
  costHistoryColumns: GridColumn[] = [
    { key: 'date', label: 'Data' },
    { key: 'purchaseOrderId', label: 'Ref. Compra' },
    { key: 'quantity', label: 'Qtd', align: 'right' },
    { key: 'unitCostPaid', label: 'Custo Pago', align: 'right' },
    { key: 'previousCost', label: 'Custo Anterior', align: 'right' },
    { key: 'newCost', label: 'Novo Custo', align: 'right' },
    { key: 'reason', label: 'Motivo' },
  ];

  recentOrderColumns: GridColumn[] = [
    { key: 'orderId', label: 'Pedido' },
    { key: 'date', label: 'Data' },
    { key: 'quantity', label: 'Qtd', align: 'right' },
    { key: 'unitPrice', label: 'Preço Unitário', align: 'right' },
    { key: 'total', label: 'Total', align: 'right' },
    { key: 'profit', label: 'Lucro', align: 'right' },
  ];

  // Cost history line chart
  costChartConfig = computed<ChartConfiguration<'line'> | null>(() => {
    const history = this.costHistory();
    if (history.length === 0) return null;

    // Reverse to chronological order (API returns desc)
    const sorted = [...history].reverse();
    const labels = sorted.map(h => this.formatDate(h.date));
    const data = sorted.map(h => h.newCost);

    return {
      type: 'line',
      data: {
        labels,
        datasets: [{
          label: 'Custo Médio (R$)',
          data,
          borderColor: '#1A237E',
          backgroundColor: 'rgba(26, 35, 126, 0.1)',
          fill: true,
          tension: 0.3,
          pointBackgroundColor: '#1A237E',
          pointRadius: 4,
          pointHoverRadius: 6,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => `R$ ${(ctx.parsed.y ?? 0).toFixed(2).replace('.', ',')}`,
            },
          },
        },
        scales: {
          y: {
            beginAtZero: false,
            ticks: {
              callback: (value) => `R$ ${Number(value).toFixed(2).replace('.', ',')}`,
            },
          },
        },
      },
    };
  });

  statusVariant = computed<BadgeVariant>(() => {
    const p = this.product();
    if (!p) return 'neutral';
    return p.statusVariant;
  });

  // Estimated margin computed
  estimatedMargin = computed(() => {
    const p = this.product();
    if (!p || p.price <= 0) return null;
    return ((p.price - p.purchaseCost - p.packagingCost) / p.price) * 100;
  });

  marginClass = computed(() => {
    const m = this.estimatedMargin();
    if (m === null) return '';
    if (m >= 20) return 'value--positive';
    if (m >= 10) return 'value--warning';
    return 'value--negative';
  });

  // Stock KPIs
  stockKpis = computed(() => {
    const p = this.product();
    if (!p) return [];
    const stock = this.totalVariantStock() || p.stock;
    const avgCost = p.purchaseCost;
    const totalCost = stock * avgCost;
    return [
      { label: 'Estoque', value: `${stock} un.` },
      { label: 'Custo Médio', value: this.formatBrl(avgCost) },
      { label: 'Custo Total', value: this.formatBrl(totalCost) },
    ];
  });

  // Analytics KPIs
  analyticsKpis = computed(() => {
    const a = this.analyticsData();
    if (!a) return [];
    return [
      { label: 'Vendas', value: String(a.totalSales), change: a.salesChange ?? undefined, changeLabel: 'vs período anterior' },
      { label: 'Receita', value: this.formatBrl(a.totalRevenue), change: a.revenueChange ?? undefined, changeLabel: 'vs período anterior' },
      { label: 'Lucro', value: this.formatBrl(a.totalProfit), change: a.profitChange ?? undefined, changeLabel: 'vs período anterior' },
      { label: 'Margem', value: a.margin !== null ? `${a.margin.toFixed(1)}%` : '—', change: a.marginChange ?? undefined, changeLabel: 'vs período anterior' },
    ];
  });

  hasVariants = computed(() => {
    const v = this.variants();
    return v.length > 0 && !(v.length === 1 && Object.keys(v[0].attributes).length === 0);
  });

  variantFields = computed(() => {
    const v = this.variants();
    if (v.length === 0) return [];
    const first = v.find(var_ => Object.keys(var_.attributes).length > 0);
    return first ? Object.keys(first.attributes) : [];
  });

  totalVariantStock = computed(() => {
    return this.variants().reduce((sum, v) => sum + v.stock, 0);
  });

  priceRange = computed(() => {
    const prices = this.variants()
      .filter(v => v.price !== null)
      .map(v => v.price as number);
    if (prices.length === 0) return null;
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    if (min === max) return this.formatBrl(min);
    return `${this.formatBrl(min)} — ${this.formatBrl(max)}`;
  });

  hasNeedsReview = computed(() => {
    return this.variants().some(v => v.needsReview);
  });

  // Pricing calculator state
  pricingMargin = signal(20);
  pricingMarketplace = signal('mercadolivre');
  pricingListingType = signal<string | null>(null);
  pricingCalculating = signal(false);
  pricingResult = signal<PriceCalculationResult | null>(null);
  pricingRules = signal<PricingRule[]>([]);
  pricingSaving = signal(false);

  marketplaceOptions: SelectOption[] = [
    { value: 'mercadolivre', label: 'Mercado Livre' },
  ];

  listingTypeOptions: SelectOption[] = [
    { value: '', label: 'Padrão' },
    { value: 'classic', label: 'Clássico' },
    { value: 'premium', label: 'Premium' },
  ];

  pricingChartConfig = computed<ChartConfiguration<'bar'> | null>(() => {
    const result = this.pricingResult();
    if (!result) return null;

    const breakdown = result.costBreakdown;
    return {
      type: 'bar',
      data: {
        labels: breakdown.map(c => c.label),
        datasets: [{
          data: breakdown.map(c => c.amount),
          backgroundColor: breakdown.map(c => c.color),
          borderRadius: 4,
          barPercentage: 0.7,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: 'y',
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const val = ctx.parsed.x ?? 0;
                const pct = breakdown[ctx.dataIndex]?.percentage ?? 0;
                return `R$ ${val.toFixed(2).replace('.', ',')} (${pct.toFixed(1)}%)`;
              },
            },
          },
        },
        scales: {
          x: {
            beginAtZero: true,
            ticks: {
              callback: (value) => `R$ ${Number(value).toFixed(0)}`,
            },
          },
        },
      },
    };
  });

  private productId = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private variantService: ProductVariantService,
    private productService: ProductService,
    private categoryService: CategoryService,
    private pricingService: PricingService,
  ) {}

  onEdit(): void {
    this.router.navigate(['/produtos', this.productId, 'editar']);
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id') || '1';
    this.loadData();
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    try {
      const p = await this.productService.getById(this.productId);

      // Resolve category breadcrumb path
      let categoryPath: string | null = null;
      if (p.categoryId) {
        categoryPath = await this.resolveCategoryPath(p.categoryId);
      }

      // Compute stock from variants
      const totalStock = (p as any).variants
        ? (p as any).variants.reduce((sum: number, v: any) => sum + (v.stock || 0), 0)
        : 0;

      const statusVariant: BadgeVariant = p.status === 'Ativo' ? 'success'
        : p.status === 'Inativo' ? 'neutral'
        : p.status === 'Pausado' ? 'warning'
        : 'neutral';

      this.product.set({
        id: p.id,
        name: p.name,
        sku: p.sku,
        description: p.description ?? null,
        categoryId: p.categoryId ?? null,
        categoryPath,
        supplier: p.supplier ?? null,
        price: p.price,
        purchaseCost: (p as any).purchaseCost ?? p.acquisitionCost ?? 0,
        packagingCost: (p as any).packagingCost ?? 0,
        status: p.status,
        statusVariant,
        imageUrl: p.imageUrl ?? ((p as any).photoUrls?.[0] ?? null),
        photoUrls: (p as any).photoUrls ?? [],
        weight: (p as any).weight ?? 0,
        height: (p as any).height ?? 0,
        width: (p as any).width ?? 0,
        length: (p as any).length ?? 0,
        stock: totalStock || p.stock || 0,
        isActive: p.isActive ?? true,
        createdAt: (p as any).createdAt ?? '',
        updatedAt: (p as any).updatedAt ?? '',
      });

      // Load variants
      this.variantService.getByProductId(this.productId).then(v => this.variants.set(v));

      // Load analytics and pricing rules
      this.loadAnalytics();
      this.loadPricingRules();
    } catch (err) {
      console.error('Failed to load product', err);
    } finally {
      this.loading.set(false);
    }

    // Load cost history from API
    this.productService.getCostHistory(this.productId).subscribe({
      next: (result) => {
        this.costHistory.set(result.items);
        this.costHistoryTotalCount.set(result.totalCount);
      },
      error: () => {
        this.costHistory.set([]);
        this.costHistoryTotalCount.set(0);
      },
    });
  }

  private async resolveCategoryPath(categoryId: string): Promise<string | null> {
    try {
      if (this.categoryService.allCategories().length === 0) {
        await this.categoryService.getAll();
      }
      const categories = this.categoryService.allCategories();
      const buildPath = (id: string): string[] => {
        const cat = categories.find(c => c.id === id);
        if (!cat) return [];
        const parentPath = cat.parentId ? buildPath(cat.parentId) : [];
        return [...parentPath, cat.name];
      };
      const path = buildPath(categoryId);
      return path.length > 0 ? path.join(' > ') : null;
    } catch {
      return null;
    }
  }

  onDateRangeChange(value: string): void {
    this.analyticsDays.set(Number(value));
    this.loadAnalytics();
  }

  private async loadAnalytics(): Promise<void> {
    this.analyticsLoading.set(true);
    try {
      const data = await this.productService.getAnalytics(this.productId, this.analyticsDays());
      this.analyticsData.set(data);
    } catch {
      this.analyticsData.set(null);
    } finally {
      this.analyticsLoading.set(false);
    }
    this.loadRecentOrders();
  }

  private loadRecentOrders(): void {
    this.recentOrdersLoading.set(true);
    this.productService.getRecentOrders(this.productId, this.analyticsDays()).subscribe({
      next: (result) => {
        this.recentOrders.set(result.items);
        this.recentOrdersTotalCount.set(result.totalCount);
        this.recentOrdersLoading.set(false);
      },
      error: () => {
        this.recentOrders.set([]);
        this.recentOrdersTotalCount.set(0);
        this.recentOrdersLoading.set(false);
      },
    });
  }

  formatBrl = formatBrlUtil;
  formatDate = formatDateShort;

  getProfitClass(profit: number): string {
    return profit >= 0 ? 'value--positive' : 'value--negative';
  }

  getCostChangeClass(previous: number, current: number): string {
    if (current > previous) return 'value--negative';
    if (current < previous) return 'value--positive';
    return '';
  }

  getVariantRowClass(variant: ProductVariant): string {
    if (variant.needsReview) return 'variant-row--review';
    if (variant.stock === 0) return 'variant-row--danger';
    if (variant.stock > 0 && variant.stock <= 5) return 'variant-row--warning';
    return '';
  }

  // Pricing methods
  onMarginInput(event: Event): void {
    const value = parseFloat((event.target as HTMLInputElement).value);
    if (!isNaN(value) && value >= 0 && value <= 99) {
      this.pricingMargin.set(value);
    }
  }

  onMarketplaceChange(value: string): void {
    this.pricingMarketplace.set(value);
    this.pricingResult.set(null);
  }

  onListingTypeChange(value: string): void {
    this.pricingListingType.set(value || null);
    this.pricingResult.set(null);
  }

  async calculatePrice(): Promise<void> {
    if (!this.productId || this.pricingCalculating()) return;
    this.pricingCalculating.set(true);
    try {
      const result = await this.pricingService.calculate({
        productId: this.productId,
        targetMarginPercent: this.pricingMargin(),
        marketplaceId: this.pricingMarketplace(),
        listingType: this.pricingListingType(),
      });
      this.pricingResult.set(result);
    } catch (err: any) {
      console.error('Pricing calculation failed', err);
      this.pricingResult.set(null);
    } finally {
      this.pricingCalculating.set(false);
    }
  }

  async savePricingRule(): Promise<void> {
    if (!this.productId || !this.pricingResult() || this.pricingSaving()) return;
    this.pricingSaving.set(true);
    try {
      // Check if rule already exists for this product+marketplace
      const existing = this.pricingRules().find(
        r => r.productId === this.productId && r.marketplaceId === this.pricingMarketplace()
      );
      if (existing) {
        await this.pricingService.updateRule(existing.id, this.pricingMargin());
      } else {
        await this.pricingService.createRule({
          productId: this.productId,
          marketplaceId: this.pricingMarketplace(),
          listingType: this.pricingListingType(),
          targetMarginPercent: this.pricingMargin(),
        });
      }
      await this.loadPricingRules();
    } catch (err: any) {
      console.error('Failed to save pricing rule', err);
    } finally {
      this.pricingSaving.set(false);
    }
  }

  async deletePricingRule(id: string): Promise<void> {
    try {
      await this.pricingService.deleteRule(id);
      this.pricingRules.update(rules => rules.filter(r => r.id !== id));
    } catch (err: any) {
      console.error('Failed to delete pricing rule', err);
    }
  }

  private async loadPricingRules(): Promise<void> {
    try {
      const rules = await this.pricingService.getRules(this.productId);
      this.pricingRules.set(rules);
    } catch {
      this.pricingRules.set([]);
    }
  }

  getMarginColorClass(margin: number): string {
    if (margin >= 20) return 'value--positive';
    if (margin >= 10) return 'value--warning';
    return 'value--negative';
  }
}
