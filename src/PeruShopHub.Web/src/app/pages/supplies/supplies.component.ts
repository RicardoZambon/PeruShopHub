import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, Search, Plus } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { DialogComponent } from '../../shared/components/dialog/dialog.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import { PageSkeletonComponent } from '../../shared/components/page-skeleton/page-skeleton.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { formatBrl as formatBrlUtil } from '../../shared/utils';
import { SupplyService, type CreateSupplyDto } from '../../services/supply.service';
import { ToastService } from '../../services/toast.service';

type SupplyCategory = 'Embalagem' | 'Etiqueta' | 'Caixa' | 'Fita' | 'Proteção' | 'Outros';

interface Supply {
  id: string;
  nome: string;
  sku: string;
  categoria: SupplyCategory;
  custoUnitario: number;
  estoque: number;
  estoqueMinimo: number;
  fornecedor: string;
  status: 'Ativo' | 'Inativo';
}

const CATEGORIES: SupplyCategory[] = ['Embalagem', 'Etiqueta', 'Caixa', 'Fita', 'Proteção', 'Outros'];

@Component({
  selector: 'app-supplies',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, BrlCurrencyPipe, PageHeaderComponent, SearchInputComponent, SelectDropdownComponent, DialogComponent, FormFieldComponent, FormActionsComponent, PageSkeletonComponent],
  templateUrl: './supplies.component.html',
  styleUrl: './supplies.component.scss',
})
export class SuppliesComponent implements OnInit {
  private readonly supplyService = inject(SupplyService);
  private readonly toastService = inject(ToastService);

  readonly searchIcon = Search;
  readonly plusIcon = Plus;
  readonly categories = CATEGORIES;

  readonly categoryOptions: SelectOption[] = [
    { value: 'Todas', label: 'Todas as categorias' },
    ...CATEGORIES.map(cat => ({ value: cat, label: cat })),
  ];

  readonly searchQuery = signal('');
  readonly categoryFilter = signal<SupplyCategory | 'Todas'>('Todas');
  readonly loading = signal(true);
  readonly supplies = signal<Supply[]>([]);

  // Modal state
  readonly modalOpen = signal(false);
  readonly saving = signal(false);
  supplyForm: FormGroup;

  readonly filteredSupplies = computed(() => {
    let items = this.supplies();
    const query = this.searchQuery().toLowerCase();
    const cat = this.categoryFilter();

    if (query) {
      items = items.filter(
        s => s.nome.toLowerCase().includes(query) ||
             s.sku.toLowerCase().includes(query) ||
             s.fornecedor.toLowerCase().includes(query)
      );
    }

    if (cat !== 'Todas') {
      items = items.filter(s => s.categoria === cat);
    }

    return items;
  });

  constructor(private fb: FormBuilder) {
    this.supplyForm = this.fb.group({
      nome: ['', Validators.required],
      sku: ['', Validators.required],
      categoria: ['Embalagem'],
      custoUnitario: [null],
      estoque: [0],
      estoqueMinimo: [0],
      fornecedor: [''],
      observacao: [''],
    });
  }

  ngOnInit(): void {
    this.loadSupplies();
  }

  formatBrl = formatBrlUtil;

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  onCategoryChange(event: Event): void {
    this.categoryFilter.set((event.target as HTMLSelectElement).value as SupplyCategory | 'Todas');
  }

  getStockStatusVariant(supply: Supply): BadgeVariant {
    if (supply.estoque === 0) return 'danger';
    if (supply.estoque <= supply.estoqueMinimo) return 'warning';
    return 'success';
  }

  getStockStatusLabel(supply: Supply): string {
    if (supply.estoque === 0) return 'Sem estoque';
    if (supply.estoque <= supply.estoqueMinimo) return 'Estoque baixo';
    return 'Normal';
  }

  getStockClass(supply: Supply): string {
    if (supply.estoque === 0) return 'supplies-table__td--danger';
    if (supply.estoque <= supply.estoqueMinimo) return 'supplies-table__td--warning';
    return '';
  }

  getRowClass(supply: Supply): string {
    if (supply.estoque === 0) return 'supplies-table__row--zero';
    if (supply.estoque <= supply.estoqueMinimo) return 'supplies-table__row--low';
    return '';
  }

  openModal(): void {
    this.supplyForm.reset({
      nome: '',
      sku: '',
      categoria: 'Embalagem',
      custoUnitario: null,
      estoque: 0,
      estoqueMinimo: 0,
      fornecedor: '',
      observacao: '',
    });
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
  }

  onCategoryFilterChange(value: string): void {
    this.categoryFilter.set(value as SupplyCategory | 'Todas');
  }

  saveSupply(): void {
    if (this.supplyForm.invalid) {
      this.supplyForm.markAllAsTouched();
      return;
    }

    const val = this.supplyForm.value;
    const dto: CreateSupplyDto = {
      nome: val.nome,
      sku: val.sku,
      categoria: val.categoria,
      custoUnitario: val.custoUnitario || 0,
      estoque: val.estoque || 0,
      estoqueMinimo: val.estoqueMinimo || 0,
      fornecedor: val.fornecedor || '',
      observacao: val.observacao || '',
    };

    this.saving.set(true);
    this.supplyService.create(dto).subscribe({
      next: (created) => {
        this.saving.set(false);
        this.supplies.update(list => [...list, created as Supply]);
        this.closeModal();
        this.toastService.show('Suprimento criado com sucesso', 'success');
      },
      error: () => {
        this.saving.set(false);
        this.toastService.show('Erro ao criar suprimento', 'danger');
      },
    });
  }

  private loadSupplies(): void {
    this.loading.set(true);
    this.supplyService.list().subscribe({
      next: (data) => {
        this.supplies.set(data as Supply[]);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.show('Erro ao carregar suprimentos', 'danger');
        this.loading.set(false);
      },
    });
  }
}
