import { Component, Input, Output, EventEmitter, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { SkeletonComponent } from '../skeleton/skeleton.component';

export interface DataTableColumn {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';
  format?: 'text' | 'currency' | 'mono' | 'custom';
  sortable?: boolean;
}

export interface SortEvent {
  column: string;
  direction: 'asc' | 'desc';
}

export interface PageEvent {
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, EmptyStateComponent, SkeletonComponent],
  template: `
    <!-- Loading State -->
    <div class="data-table" *ngIf="loading">
      <table class="data-table__table data-table__table--desktop">
        <thead>
          <tr>
            <th *ngFor="let col of columns">{{ col.label }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of skeletonRows">
            <td *ngFor="let col of columns">
              <app-skeleton type="text" width="80%" height="14px"></app-skeleton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Empty State -->
    <div class="data-table" *ngIf="!loading && (!data || data.length === 0)">
      <app-empty-state
        [title]="emptyTitle"
        [description]="emptyDescription"
      ></app-empty-state>
    </div>

    <!-- Data State -->
    <div class="data-table" *ngIf="!loading && data && data.length > 0">
      <!-- Desktop/Tablet Table -->
      <div class="data-table__scroll-wrapper">
        <table class="data-table__table data-table__table--desktop">
          <thead>
            <tr>
              <th
                *ngFor="let col of columns"
                [class.data-table__th--sortable]="col.sortable"
                [class.data-table__th--active]="currentSort?.column === col.key"
                [style.text-align]="col.align || 'left'"
                (click)="col.sortable ? onSort(col.key) : null"
              >
                {{ col.label }}
                <span class="data-table__sort-icon" *ngIf="col.sortable">
                  <span *ngIf="currentSort?.column === col.key">
                    {{ currentSort?.direction === 'asc' ? '▲' : '▼' }}
                  </span>
                  <span *ngIf="currentSort?.column !== col.key" class="data-table__sort-icon--inactive">⇅</span>
                </span>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              *ngFor="let row of data"
              class="data-table__row"
              (click)="rowClick.emit(row)"
            >
              <td
                *ngFor="let col of columns"
                [style.text-align]="col.align || 'left'"
                [class.data-table__td--mono]="col.format === 'mono' || col.format === 'currency'"
              >
                {{ row[col.key] }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Mobile Card List -->
      <div class="data-table__cards">
        <div
          class="data-table__card"
          *ngFor="let row of data"
          (click)="rowClick.emit(row)"
        >
          <div class="data-table__card-row" *ngFor="let col of columns">
            <span class="data-table__card-label">{{ col.label }}</span>
            <span
              class="data-table__card-value"
              [class.data-table__td--mono]="col.format === 'mono' || col.format === 'currency'"
            >
              {{ row[col.key] }}
            </span>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div class="data-table__pagination" *ngIf="totalItems > 0">
        <span class="data-table__pagination-info">
          Mostrando {{ rangeStart }}-{{ rangeEnd }} de {{ totalItems }}
        </span>

        <div class="data-table__pagination-controls">
          <select
            class="data-table__page-size"
            [value]="pageSize"
            (change)="onPageSizeChange($event)"
          >
            <option [value]="10">10</option>
            <option [value]="25">25</option>
            <option [value]="50">50</option>
          </select>

          <button
            class="data-table__page-btn"
            [disabled]="currentPage <= 1"
            (click)="onPageChange(currentPage - 1)"
          >
            ‹ Anterior
          </button>
          <span class="data-table__page-current">{{ currentPage }} / {{ totalPages }}</span>
          <button
            class="data-table__page-btn"
            [disabled]="currentPage >= totalPages"
            (click)="onPageChange(currentPage + 1)"
          >
            Próximo ›
          </button>
        </div>
      </div>
    </div>
  `,
  styleUrl: './data-table.component.scss',
})
export class DataTableComponent {
  @Input({ required: true }) columns: DataTableColumn[] = [];
  @Input({ required: true }) data: Record<string, any>[] = [];
  @Input() loading = false;
  @Input() emptyTitle = 'Nenhum dado encontrado';
  @Input() emptyDescription = 'Não há registros para exibir.';
  @Input() pageSize = 10;
  @Input() totalItems = 0;

  @Output() sortChange = new EventEmitter<SortEvent>();
  @Output() pageChange = new EventEmitter<PageEvent>();
  @Output() rowClick = new EventEmitter<Record<string, any>>();

  currentSort: SortEvent | null = null;
  currentPage = 1;

  readonly skeletonRows = Array(6);

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalItems / this.pageSize));
  }

  get rangeStart(): number {
    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalItems);
  }

  onSort(column: string): void {
    if (this.currentSort?.column === column) {
      this.currentSort = {
        column,
        direction: this.currentSort.direction === 'asc' ? 'desc' : 'asc',
      };
    } else {
      this.currentSort = { column, direction: 'asc' };
    }
    this.sortChange.emit(this.currentSort);
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.pageChange.emit({ page: this.currentPage, pageSize: this.pageSize });
  }

  onPageSizeChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.pageSize = Number(target.value);
    this.currentPage = 1;
    this.pageChange.emit({ page: this.currentPage, pageSize: this.pageSize });
  }
}
