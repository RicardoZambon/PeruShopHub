import { Component, signal, computed, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule, Megaphone, ExternalLink, Link2, RefreshCw, CheckCircle2, AlertTriangle, XCircle, CircleDashed, Image } from 'lucide-angular';
import {
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
  GridColumn,
  GridSortEvent,
} from '../../shared/components/data-grid/data-grid.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { formatBrl as formatBrlUtil } from '../../shared/utils';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { ListingService, type ListingGridItem } from '../../services/listing.service';
import { ToastService } from '../../services/toast.service';

type SyncFilter = 'Todos' | 'linked' | 'unlinked';
type StatusFilter = 'Todos' | string;

@Component({
  selector: 'app-listings',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    DataGridComponent,
    GridCellDirective,
    GridCardDirective,
    BadgeComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    SearchInputComponent,
    SelectDropdownComponent,
    ButtonComponent,
  ],
  templateUrl: './listings.component.html',
  styleUrl: './listings.component.scss',
})
export class ListingsComponent implements OnInit {
  private readonly listingService = inject(ListingService);
  private readonly toastService = inject(ToastService);
  readonly router = inject(Router);

  readonly megaphoneIcon = Megaphone;
  readonly externalLinkIcon = ExternalLink;
  readonly linkIcon = Link2;
  readonly refreshIcon = RefreshCw;
  readonly checkIcon = CheckCircle2;
  readonly alertIcon = AlertTriangle;
  readonly errorIcon = XCircle;
  readonly unlinkedIcon = CircleDashed;
  readonly imageIcon = Image;

  @ViewChild('grid') gridRef!: DataGridComponent;

  readonly statusOptions: SelectOption[] = [
    { value: 'Todos', label: 'Todos os status' },
    { value: 'active', label: 'Ativo' },
    { value: 'paused', label: 'Pausado' },
    { value: 'closed', label: 'Encerrado' },
  ];

  readonly syncOptions: SelectOption[] = [
    { value: 'Todos', label: 'Todos' },
    { value: 'linked', label: 'Vinculado' },
    { value: 'unlinked', label: 'Não vinculado' },
  ];

  readonly searchQuery = signal('');
  readonly statusFilter = signal<StatusFilter>('Todos');
  readonly syncFilter = signal<SyncFilter>('Todos');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly hasMore = signal(true);
  readonly totalCount = signal(0);

  readonly listings = signal<ListingGridItem[]>([]);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly sortBy = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  readonly gridColumns: GridColumn[] = [
    { key: 'thumbnailUrl', label: 'Foto', align: 'center', width: '56px' },
    { key: 'title', label: 'Título', sortable: true },
    { key: 'externalId', label: 'ID ML', cellClass: 'cell-mono' },
    { key: 'price', label: 'Preço', align: 'right', sortable: true },
    { key: 'stock', label: 'Estoque', align: 'right', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'syncStatus', label: 'Sincronização' },
    { key: 'productName', label: 'Produto Interno' },
    { key: 'actions', label: 'Ações', align: 'center', width: '88px' },
  ];

  readonly gridData = computed(() => {
    return this.listings().map(l => ({
      ...l,
      id: l.id,
      stock: l.availableQuantity,
    }));
  });

  formatBrl = formatBrlUtil;

  ngOnInit(): void {
    this.loadListings(true);
  }

  async loadListings(reset = false): Promise<void> {
    if (reset) {
      this.currentPage.set(1);
      this.listings.set([]);
      this.hasMore.set(true);
    }

    this.loading.set(true);
    try {
      const result = await this.listingService.list({
        page: this.currentPage(),
        pageSize: this.pageSize(),
        search: this.searchQuery() || undefined,
        status: this.statusFilter() === 'Todos' ? undefined : this.statusFilter(),
        syncStatus: this.syncFilter() === 'Todos' ? undefined : this.syncFilter(),
        sortBy: this.sortBy() ?? undefined,
        sortDirection: this.sortDirection(),
      });

      if (reset) {
        this.listings.set(result.items);
      } else {
        this.listings.update(prev => [...prev, ...result.items]);
      }

      const totalLoaded = this.listings().length;
      this.hasMore.set(totalLoaded < result.totalCount);
      this.totalCount.set(result.totalCount);
      this.hasData.set(
        totalLoaded > 0 ||
        this.searchQuery().length > 0 ||
        this.statusFilter() !== 'Todos' ||
        this.syncFilter() !== 'Todos'
      );
    } catch {
      if (reset) {
        this.listings.set([]);
      }
      this.hasMore.set(false);
      this.hasData.set(false);
    } finally {
      this.loading.set(false);
    }
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    this.loadListings(true);
    this.gridRef?.scrollToTop();
  }

  onStatusChange(value: string): void {
    this.statusFilter.set(value as StatusFilter);
    this.loadListings(true);
    this.gridRef?.scrollToTop();
  }

  onSyncChange(value: string): void {
    this.syncFilter.set(value as SyncFilter);
    this.loadListings(true);
    this.gridRef?.scrollToTop();
  }

  onSort(event: GridSortEvent): void {
    this.sortBy.set(event.direction ? event.column : null);
    this.sortDirection.set(event.direction ?? 'asc');
    this.loadListings(true);
    this.gridRef?.scrollToTop();
  }

  onLoadMore(): void {
    this.currentPage.update(p => p + 1);
    this.loadListings(false);
  }

  getStatusLabel(status: string): string {
    const map: Record<string, string> = {
      'active': 'Ativo',
      'paused': 'Pausado',
      'closed': 'Encerrado',
      'under_review': 'Em revisão',
    };
    return map[status] ?? status;
  }

  getStatusVariant(status: string): BadgeVariant {
    const map: Record<string, BadgeVariant> = {
      'active': 'success',
      'paused': 'warning',
      'closed': 'danger',
      'under_review': 'neutral',
    };
    return map[status] ?? 'neutral';
  }

  getSyncVariant(syncStatus: string): BadgeVariant {
    if (syncStatus === 'Sincronizado') return 'success';
    if (syncStatus === 'Desatualizado') return 'warning';
    if (syncStatus === 'Erro') return 'danger';
    return 'neutral'; // Não vinculado
  }

  openOnMl(event: Event, row: Record<string, any>): void {
    event.stopPropagation();
    if (row['permalink']) {
      window.open(row['permalink'], '_blank');
    }
  }

  goToProduct(event: Event, row: Record<string, any>): void {
    event.stopPropagation();
    if (row['productId']) {
      this.router.navigate(['/produtos', row['productId']]);
    }
  }

  onRowClick(row: Record<string, any>): void {
    if (row['productId']) {
      this.router.navigate(['/produtos', row['productId']]);
    } else if (row['permalink']) {
      window.open(row['permalink'], '_blank');
    }
  }
}
