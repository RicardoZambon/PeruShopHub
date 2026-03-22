import { Component, Input, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  LucideAngularModule,
  ChevronDown,
  ChevronUp,
  Trash2,
  Plus,
  DollarSign,
} from 'lucide-angular';
import {
  ProductVariant,
  DEFAULT_VARIANT_COSTS,
  DEFAULT_VARIANT_SHIPPING,
} from '../../models/product-variant.model';
import { ProductVariantService } from '../../services/product-variant.service';

@Component({
  selector: 'app-variant-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './variant-manager.component.html',
  styleUrl: './variant-manager.component.scss',
})
export class VariantManagerComponent {
  private variantService = inject(ProductVariantService);

  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;
  readonly trashIcon = Trash2;
  readonly plusIcon = Plus;
  readonly dollarIcon = DollarSign;

  @Input() productId = 'prod-cam-001';

  variants = computed(() => this.variantService.getByProductId(this.productId));

  expandedRows = signal<Set<string>>(new Set());
  bulkPrice = signal<number | null>(null);

  attributeKeys = computed(() => {
    const vs = this.variants();
    if (vs.length === 0) return [];
    const keys = new Set<string>();
    vs.forEach(v => Object.keys(v.attributes).forEach(k => keys.add(k)));
    return Array.from(keys);
  });

  toggleExpand(variantId: string): void {
    const current = new Set(this.expandedRows());
    if (current.has(variantId)) {
      current.delete(variantId);
    } else {
      current.add(variantId);
    }
    this.expandedRows.set(current);
  }

  isExpanded(variantId: string): boolean {
    return this.expandedRows().has(variantId);
  }

  async updateVariantPrice(variantId: string, price: number | null): Promise<void> {
    await this.variantService.update(variantId, { price });
  }

  async updateVariantStock(variantId: string, stock: number): Promise<void> {
    await this.variantService.update(variantId, { stock });
  }

  async updateVariantSku(variantId: string, sku: string): Promise<void> {
    await this.variantService.update(variantId, { sku });
  }

  async updateVariantActive(variantId: string, isActive: boolean): Promise<void> {
    await this.variantService.update(variantId, { isActive });
  }

  async updateVariantCostAquisicao(variant: ProductVariant, value: number | null): Promise<void> {
    const costs = { ...(variant.costs ?? DEFAULT_VARIANT_COSTS), custoAquisicao: value };
    await this.variantService.update(variant.id, { costs });
  }

  async updateVariantShippingField(
    variant: ProductVariant,
    field: 'peso' | 'altura' | 'largura' | 'comprimento',
    value: number | null,
  ): Promise<void> {
    const shipping = { ...(variant.shipping ?? DEFAULT_VARIANT_SHIPPING), [field]: value };
    await this.variantService.update(variant.id, { shipping });
  }

  async deleteVariant(variantId: string): Promise<void> {
    if (!confirm('Tem certeza que deseja excluir esta variante?')) return;
    await this.variantService.delete(variantId);
  }

  updateBulkPrice(value: string): void {
    const num = parseFloat(value);
    this.bulkPrice.set(isNaN(num) ? null : num);
  }

  async applyBulkPrice(): Promise<void> {
    const price = this.bulkPrice();
    if (price === null) return;
    const vs = this.variants();
    for (const v of vs) {
      await this.variantService.update(v.id, { price });
    }
    this.bulkPrice.set(null);
  }

  formatCurrency(value: number | null | undefined): string {
    if (value == null) return '-';
    return `R$ ${value.toFixed(2).replace('.', ',')}`;
  }
}
