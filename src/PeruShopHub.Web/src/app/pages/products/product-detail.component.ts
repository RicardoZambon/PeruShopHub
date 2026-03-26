import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, Edit } from 'lucide-angular';
import {
  KpiCardComponent,
  BadgeComponent,
  SelectDropdownComponent,
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
} from '../../shared/components';
import type { BadgeVariant, SelectOption, GridColumn } from '../../shared/components';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { ProductVariantService } from '../../services/product-variant.service';
import { ProductService } from '../../services/product.service';
import type { CostHistoryItem } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import type { ProductVariant } from '../../models/product-variant.model';

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
    RouterLink,
    LucideAngularModule,
    KpiCardComponent,
    BadgeComponent,
    ButtonComponent,
    BrlCurrencyPipe,
    SelectDropdownComponent,
    DataGridComponent,
    GridCellDirective,
    GridCardDirective,
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
  ];

  recentOrderColumns: GridColumn[] = [
    { key: 'orderId', label: 'Pedido' },
    { key: 'date', label: 'Data' },
    { key: 'quantity', label: 'Qtd', align: 'right' },
    { key: 'unitPrice', label: 'Preço Unitário', align: 'right' },
    { key: 'total', label: 'Total', align: 'right' },
    { key: 'profit', label: 'Lucro', align: 'right' },
  ];

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

  private productId = '';

  constructor(
    private route: ActivatedRoute,
    private variantService: ProductVariantService,
    private productService: ProductService,
    private categoryService: CategoryService,
  ) {}

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

      // Load analytics
      this.loadAnalytics();
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

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr + 'T00:00:00');
    return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

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
}
