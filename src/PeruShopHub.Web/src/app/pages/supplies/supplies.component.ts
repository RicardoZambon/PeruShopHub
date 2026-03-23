import { Component, signal, computed, HostListener, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, Search, Plus } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { SupplyService, type SupplyDto, type CreateSupplyDto } from '../../services/supply.service';

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
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, BrlCurrencyPipe],
  templateUrl: './supplies.component.html',
  styleUrl: './supplies.component.scss',
})
export class SuppliesComponent implements OnInit {
  private readonly supplyService = inject(SupplyService);

  readonly searchIcon = Search;
  readonly plusIcon = Plus;
  readonly categories = CATEGORIES;

  readonly searchQuery = signal('');
  readonly categoryFilter = signal<SupplyCategory | 'Todas'>('Todas');
  readonly loading = signal(true);
  readonly supplies = signal<Supply[]>([]);

  // Modal state
  readonly modalOpen = signal(false);
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

  @HostListener('document:keydown.escape')
  onEscKey(): void {
    if (this.modalOpen()) {
      this.closeModal();
    }
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

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

  onModalBackdropClick(): void {
    this.closeModal();
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

    this.supplyService.create(dto).subscribe({
      next: (created) => {
        this.supplies.update(list => [...list, created as Supply]);
        this.closeModal();
      },
      error: (err) => {
        console.error('Failed to create supply:', err);
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
      error: (err) => {
        console.error('Failed to load supplies:', err);
        this.loading.set(false);
      },
    });
  }
}
