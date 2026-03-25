import {
  Component,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
  ContentChildren,
  QueryList,
  Directive,
  TemplateRef,
  ElementRef,
  ViewChild,
  AfterViewInit,
  OnDestroy,
  NgZone,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkeletonComponent } from '../skeleton/skeleton.component';

export interface GridColumn {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';
  width?: string;
  sortable?: boolean;
  sticky?: boolean;
  headerClass?: string;
  cellClass?: string;
}

export interface GridCellContext {
  $implicit: Record<string, any>;
  value: any;
}

export type SortDirection = 'asc' | 'desc' | null;

export interface GridSortEvent {
  column: string;
  direction: SortDirection;
}

@Directive({
  selector: '[appGridCell]',
  standalone: true,
})
export class GridCellDirective {
  @Input({ required: true }) appGridCell!: string;

  constructor(public templateRef: TemplateRef<GridCellContext>) {}
}

@Directive({
  selector: '[appGridHeader]',
  standalone: true,
})
export class GridHeaderDirective {
  @Input({ required: true }) appGridHeader!: string;

  constructor(public templateRef: TemplateRef<void>) {}
}

@Component({
  selector: 'app-data-grid',
  standalone: true,
  imports: [CommonModule, SkeletonComponent],
  templateUrl: './data-grid.component.html',
  styleUrl: './data-grid.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataGridComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) columns: GridColumn[] = [];
  @Input({ required: true }) data: Record<string, any>[] = [];
  @Input() loading = false;
  @Input() hasMore = false;
  @Input() pageSize = 20;
  @Input() skeletonRows = 8;

  @Output() sortChange = new EventEmitter<GridSortEvent>();
  @Output() loadMore = new EventEmitter<void>();

  @ContentChildren(GridCellDirective) cellTemplates!: QueryList<GridCellDirective>;
  @ContentChildren(GridHeaderDirective) headerTemplates!: QueryList<GridHeaderDirective>;

  @ViewChild('sentinel', { static: false }) sentinelRef!: ElementRef<HTMLDivElement>;

  activeSort: GridSortEvent = { column: '', direction: null };

  private observer: IntersectionObserver | null = null;

  constructor(private ngZone: NgZone) {}

  ngAfterViewInit(): void {
    this.setupObserver();
  }

  ngOnDestroy(): void {
    this.destroyObserver();
  }

  private setupObserver(): void {
    if (!this.sentinelRef) return;

    this.ngZone.runOutsideAngular(() => {
      this.observer = new IntersectionObserver(
        (entries) => {
          const entry = entries[0];
          if (entry.isIntersecting && this.hasMore && !this.loading) {
            this.ngZone.run(() => this.loadMore.emit());
          }
        },
        { root: this.sentinelRef.nativeElement.closest('.data-grid__scroll-wrapper'), threshold: 0 }
      );
      this.observer.observe(this.sentinelRef.nativeElement);
    });
  }

  private destroyObserver(): void {
    if (this.observer) {
      this.observer.disconnect();
      this.observer = null;
    }
  }

  getCellTemplate(columnKey: string): TemplateRef<GridCellContext> | null {
    const directive = this.cellTemplates?.find(d => d.appGridCell === columnKey);
    return directive ? directive.templateRef : null;
  }

  getHeaderTemplate(columnKey: string): TemplateRef<void> | null {
    const directive = this.headerTemplates?.find(d => d.appGridHeader === columnKey);
    return directive ? directive.templateRef : null;
  }

  onSortClick(column: GridColumn): void {
    if (!column.sortable) return;

    let direction: SortDirection;

    if (this.activeSort.column === column.key) {
      // Cycle: asc → desc → null
      if (this.activeSort.direction === 'asc') {
        direction = 'desc';
      } else if (this.activeSort.direction === 'desc') {
        direction = null;
      } else {
        direction = 'asc';
      }
    } else {
      direction = 'asc';
    }

    this.activeSort = {
      column: direction ? column.key : '',
      direction,
    };

    this.sortChange.emit({
      column: column.key,
      direction,
    });
  }

  get skeletonRowsArray(): number[] {
    return Array.from({ length: this.skeletonRows }, (_, i) => i);
  }

  get showSkeleton(): boolean {
    return this.loading && this.data.length === 0;
  }

  getSortDirection(columnKey: string): SortDirection {
    return this.activeSort.column === columnKey ? this.activeSort.direction : null;
  }
}
