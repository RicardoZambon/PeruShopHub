import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { LucideAngularModule, Search, Plus, Trash2, ArrowLeft, Package } from 'lucide-angular';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { PurchaseOrderService, type CreatePurchaseOrderDto } from '../../services/purchase-order.service';
import { ProductService, type Product } from '../../services/product.service';
import { ToastService } from '../../services/toast.service';

interface SelectedProduct {
  productId: string;
  variantId: string;
  name: string;
  sku: string;
  quantity: number;
  unitCost: number;
}

interface AdditionalCost {
  description: string;
  value: number;
  distributionMethod: string;
}

@Component({
  selector: 'app-purchase-order-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule, BrlCurrencyPipe, ButtonComponent, FormFieldComponent, SelectDropdownComponent],
  templateUrl: './purchase-order-form.component.html',
  styleUrl: './purchase-order-form.component.scss',
})
export class PurchaseOrderFormComponent {
  readonly costMethodOptions: SelectOption[] = [
    { value: 'Por Valor', label: 'Por Valor' },
    { value: 'Por Quantidade', label: 'Por Quantidade' },
    { value: 'Manual', label: 'Manual' },
  ];

  readonly searchIcon = Search;
  readonly plusIcon = Plus;
  readonly trashIcon = Trash2;
  readonly arrowLeftIcon = ArrowLeft;
  readonly packageIcon = Package;

  private readonly router = inject(Router);
  private readonly poService = inject(PurchaseOrderService);
  private readonly productService = inject(ProductService);
  private readonly toastService = inject(ToastService);

  // Supplier
  readonly supplier = signal('');
  readonly notes = signal('');

  // Product search
  readonly productSearchQuery = signal('');
  readonly productSearchResults = signal<Product[]>([]);
  readonly showProductDropdown = signal(false);
  readonly searchLoading = signal(false);

  // Selected items
  readonly selectedItems = signal<SelectedProduct[]>([]);

  // Additional costs
  readonly additionalCosts = signal<AdditionalCost[]>([]);
  readonly showCostForm = signal(false);
  readonly costDescription = signal('');
  readonly costValue = signal<number>(0);
  readonly costMethod = signal('Por Valor');

  // Saving state
  readonly saving = signal(false);

  // Computed totals
  readonly subtotal = computed(() =>
    this.selectedItems().reduce((sum, item) => sum + item.quantity * item.unitCost, 0)
  );

  readonly totalAdditionalCosts = computed(() =>
    this.additionalCosts().reduce((sum, c) => sum + c.value, 0)
  );

  readonly total = computed(() => this.subtotal() + this.totalAdditionalCosts());

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  onSupplierChange(event: Event): void {
    this.supplier.set((event.target as HTMLInputElement).value);
  }

  onNotesChange(event: Event): void {
    this.notes.set((event.target as HTMLTextAreaElement).value);
  }

  // Product search
  async onProductSearch(event: Event): Promise<void> {
    const query = (event.target as HTMLInputElement).value;
    this.productSearchQuery.set(query);

    if (query.length < 2) {
      this.productSearchResults.set([]);
      this.showProductDropdown.set(false);
      return;
    }

    this.searchLoading.set(true);
    this.showProductDropdown.set(true);
    try {
      const result = await this.productService.list({ search: query, pageSize: 10 });
      this.productSearchResults.set(result.items as Product[]);
    } catch {
      this.productSearchResults.set([]);
    } finally {
      this.searchLoading.set(false);
    }
  }

  onProductSearchFocus(): void {
    if (this.productSearchResults().length > 0) {
      this.showProductDropdown.set(true);
    }
  }

  onProductSearchBlur(): void {
    setTimeout(() => this.showProductDropdown.set(false), 200);
  }

  selectProduct(product: Product): void {
    const existing = this.selectedItems().find(
      i => i.productId === product.id && i.variantId === (product.id)
    );
    if (existing) {
      // Already added, increment quantity
      this.selectedItems.update(items =>
        items.map(i =>
          i.productId === product.id ? { ...i, quantity: i.quantity + 1 } : i
        )
      );
    } else {
      this.selectedItems.update(items => [
        ...items,
        {
          productId: product.id,
          variantId: product.id,
          name: product.name,
          sku: product.sku,
          quantity: 1,
          unitCost: product.acquisitionCost || 0,
        },
      ]);
    }
    this.productSearchQuery.set('');
    this.productSearchResults.set([]);
    this.showProductDropdown.set(false);
  }

  removeItem(index: number): void {
    this.selectedItems.update(items => items.filter((_, i) => i !== index));
  }

  onItemQuantityChange(index: number, event: Event): void {
    const value = parseInt((event.target as HTMLInputElement).value, 10);
    if (isNaN(value) || value < 1) return;
    this.selectedItems.update(items =>
      items.map((item, i) => (i === index ? { ...item, quantity: value } : item))
    );
  }

  onItemUnitCostChange(index: number, event: Event): void {
    const value = parseFloat((event.target as HTMLInputElement).value);
    if (isNaN(value) || value < 0) return;
    this.selectedItems.update(items =>
      items.map((item, i) => (i === index ? { ...item, unitCost: value } : item))
    );
  }

  // Additional costs
  openCostForm(): void {
    this.costDescription.set('');
    this.costValue.set(0);
    this.costMethod.set('Por Valor');
    this.showCostForm.set(true);
  }

  cancelCostForm(): void {
    this.showCostForm.set(false);
  }

  onCostDescriptionChange(event: Event): void {
    this.costDescription.set((event.target as HTMLInputElement).value);
  }

  onCostValueChange(event: Event): void {
    this.costValue.set(parseFloat((event.target as HTMLInputElement).value) || 0);
  }

  onCostMethodChange(event: Event): void {
    this.costMethod.set((event.target as HTMLSelectElement).value);
  }

  addCost(): void {
    const desc = this.costDescription();
    const val = this.costValue();
    if (!desc || val <= 0) return;

    this.additionalCosts.update(costs => [
      ...costs,
      { description: desc, value: val, distributionMethod: this.costMethod() },
    ]);
    this.showCostForm.set(false);
  }

  removeCost(index: number): void {
    this.additionalCosts.update(costs => costs.filter((_, i) => i !== index));
  }

  getMethodLabel(method: string): string {
    switch (method) {
      case 'Por Valor': return 'Por Valor';
      case 'Por Quantidade': return 'Por Quantidade';
      case 'Manual': return 'Manual';
      default: return method;
    }
  }

  // Save
  async save(): Promise<void> {
    if (this.selectedItems().length === 0) {
      this.toastService.show('Adicione pelo menos um produto.', 'warning');
      return;
    }

    this.saving.set(true);
    try {
      const dto: CreatePurchaseOrderDto = {
        supplier: this.supplier(),
        notes: this.notes() || undefined,
        items: this.selectedItems().map(item => ({
          productId: item.productId,
          variantId: item.variantId,
          quantity: item.quantity,
          unitCost: item.unitCost,
        })),
        costs: this.additionalCosts().map(c => ({
          description: c.description,
          value: c.value,
          distributionMethod: c.distributionMethod,
        })),
      };

      const result = await new Promise<any>((resolve, reject) => {
        this.poService.create(dto).subscribe({ next: resolve, error: reject });
      });

      this.toastService.show('Ordem de compra criada com sucesso!', 'success');
      this.router.navigate(['/compras', result.id]);
    } catch (err) {
      console.error('Failed to create purchase order:', err);
      this.toastService.show('Erro ao criar ordem de compra.', 'danger');
    } finally {
      this.saving.set(false);
    }
  }

  goBack(): void {
    this.router.navigate(['/compras']);
  }
}
