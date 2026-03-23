import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, Edit } from 'lucide-angular';
import { KpiCardComponent, BadgeComponent } from '../../shared/components';
import type { BadgeVariant } from '../../shared/components';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { ProductService, Product } from '../../services/product.service';
import { ProductVariantService } from '../../services/product-variant.service';
import type { ProductVariant } from '../../models/product-variant.model';

interface CostHistory {
  date: string;
  type: string;
  value: number;
}

interface RecentOrder {
  id: string;
  date: string;
  qty: number;
  value: number;
  profit: number;
}

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, KpiCardComponent, BadgeComponent, BrlCurrencyPipe],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
})
export class ProductDetailComponent implements OnInit {
  readonly ArrowLeft = ArrowLeft;
  readonly Package = Package;
  readonly EditIcon = Edit;

  private readonly productService = inject(ProductService);
  private readonly variantService = inject(ProductVariantService);

  loading = signal(true);
  product = signal<Product | null>(null);
  costHistory = signal<CostHistory[]>([]);
  recentOrders = signal<RecentOrder[]>([]);
  variants = signal<ProductVariant[]>([]);

  statusVariant = computed<BadgeVariant>(() => {
    const p = this.product();
    if (!p) return 'neutral';
    return this.getStatusVariant(p.status);
  });

  kpis = computed(() => {
    const p = this.product();
    if (!p) return [];
    return [
      { label: 'Vendas 30d', value: String(p.sales30d), change: 12.5, changeLabel: 'vs mes anterior' },
      { label: 'Receita 30d', value: this.formatBrl(p.revenue30d), change: 8.3, changeLabel: 'vs mes anterior' },
      { label: 'Lucro 30d', value: this.formatBrl(p.profit30d), change: -2.1, changeLabel: 'vs mes anterior' },
      { label: 'Margem 30d', value: `${p.margin30d.toFixed(1)}%`, change: -1.3, changeLabel: 'vs mes anterior' },
      { label: 'Estoque', value: String(p.stock), change: undefined, changeLabel: undefined },
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
  ) {}

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id') || '1';
    this.loadData();
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    try {
      const [product, variants] = await Promise.all([
        this.productService.getById(this.productId),
        this.productService.getVariants(this.productId),
      ]);

      this.product.set(product);
      this.variants.set(variants);

      // Cost history and recent orders would come from dedicated endpoints
      // For now these remain empty until those endpoints are available
      this.costHistory.set([]);
      this.recentOrders.set([]);
    } catch {
      this.product.set(null);
      this.variants.set([]);
    } finally {
      this.loading.set(false);
    }
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

  getVariantRowClass(variant: ProductVariant): string {
    if (variant.needsReview) return 'variant-row--review';
    if (variant.stock === 0) return 'variant-row--danger';
    if (variant.stock > 0 && variant.stock <= 5) return 'variant-row--warning';
    return '';
  }

  private getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'Ativo': return 'success';
      case 'Pausado': return 'warning';
      case 'Encerrado': return 'danger';
      default: return 'neutral';
    }
  }
}
