import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, Edit } from 'lucide-angular';
import { KpiCardComponent, BadgeComponent } from '../../shared/components';
import type { BadgeVariant } from '../../shared/components';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { ProductVariantService } from '../../services/product-variant.service';
import type { ProductVariant } from '../../models/product-variant.model';

interface ProductDetail {
  id: string;
  name: string;
  sku: string;
  status: string;
  statusVariant: BadgeVariant;
  imageUrl: string | null;
  sales30d: number;
  revenue30d: number;
  profit30d: number;
  margin30d: number;
  stock: number;
}

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

  loading = signal(true);
  product = signal<ProductDetail | null>(null);
  costHistory = signal<CostHistory[]>([]);
  recentOrders = signal<RecentOrder[]>([]);
  variants = signal<ProductVariant[]>([]);

  kpis = computed(() => {
    const p = this.product();
    if (!p) return [];
    return [
      { label: 'Vendas 30d', value: String(p.sales30d), change: 12.5, changeLabel: 'vs mês anterior' },
      { label: 'Receita 30d', value: this.formatBrl(p.revenue30d), change: 8.3, changeLabel: 'vs mês anterior' },
      { label: 'Lucro 30d', value: this.formatBrl(p.profit30d), change: -2.1, changeLabel: 'vs mês anterior' },
      { label: 'Margem 30d', value: `${p.margin30d.toFixed(1)}%`, change: -1.3, changeLabel: 'vs mês anterior' },
      { label: 'Estoque', value: String(p.stock), change: undefined, changeLabel: undefined },
    ];
  });

  hasVariants = computed(() => {
    const v = this.variants();
    // Don't count default variant (no attributes)
    return v.length > 0 && !(v.length === 1 && Object.keys(v[0].attributes).length === 0);
  });

  variantFields = computed(() => {
    const v = this.variants();
    if (v.length === 0) return [];
    // Get field names from the first variant that has attributes
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
  ) {}

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id') || '1';
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    setTimeout(() => {
      this.product.set({
        id: this.productId,
        name: 'Fone de Ouvido Bluetooth TWS Pro Max',
        sku: 'FONE-BT-PRO-001',
        status: 'Ativo',
        statusVariant: 'success',
        imageUrl: null,
        sales30d: 47,
        revenue30d: 7506.30,
        profit30d: 1876.58,
        margin30d: 25.0,
        stock: 83,
      });

      this.costHistory.set([
        { date: '2026-03-20', type: 'Custo de aquisição', value: 45.00 },
        { date: '2026-03-15', type: 'Reajuste fornecedor', value: 48.50 },
        { date: '2026-03-01', type: 'Custo embalagem', value: 3.20 },
        { date: '2026-02-15', type: 'Custo de aquisição', value: 42.00 },
        { date: '2026-02-01', type: 'Custo embalagem', value: 2.80 },
      ]);

      this.recentOrders.set([
        { id: '2087654321', date: '2026-03-21', qty: 1, value: 159.90, profit: 38.45 },
        { id: '2087654298', date: '2026-03-20', qty: 2, value: 319.80, profit: 76.90 },
        { id: '2087654275', date: '2026-03-19', qty: 1, value: 159.90, profit: 40.12 },
        { id: '2087654250', date: '2026-03-18', qty: 1, value: 159.90, profit: 37.80 },
        { id: '2087654230', date: '2026-03-17', qty: 3, value: 479.70, profit: 115.35 },
      ]);

      // Load variants
      this.variants.set(this.variantService.getByProductId(this.productId));

      this.loading.set(false);
    }, 600);
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
}
