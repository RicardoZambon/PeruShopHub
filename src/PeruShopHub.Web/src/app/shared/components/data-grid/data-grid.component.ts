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

@Directive({
  selector: '[appGridEmpty]',
  standalone: true,
})
export class GridEmptyDirective {
  constructor(public templateRef: TemplateRef<void>) {}
}

export interface GridCardContext {
  $implicit: Record<string, any>;
}

@Directive({
  selector: '[appGridCard]',
  standalone: true,
})
export class GridCardDirective {
  constructor(public templateRef: TemplateRef<GridCardContext>) {}
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
  @Input() emptyTitle = 'Nenhum item encontrado';
  @Input() emptyDescription = '';

  @Output() sortChange = new EventEmitter<GridSortEvent>();
  @Output() loadMore = new EventEmitter<void>();
  @Output() rowClick = new EventEmitter<Record<string, any>>();

  @ContentChildren(GridCellDirective) cellTemplates!: QueryList<GridCellDirective>;
  @ContentChildren(GridHeaderDirective) headerTemplates!: QueryList<GridHeaderDirective>;
  @ContentChildren(GridEmptyDirective) emptyTemplates!: QueryList<GridEmptyDirective>;
  @ContentChildren(GridCardDirective) cardTemplates!: QueryList<GridCardDirective>;

  @ViewChild('sentinel', { static: false }) sentinelRef!: ElementRef<HTMLDivElement>;
  @ViewChild('scrollWrapper', { static: false }) scrollWrapperRef!: ElementRef<HTMLDivElement>;

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

  get showEmpty(): boolean {
    return !this.loading && this.data.length === 0;
  }

  get emptyTemplate(): TemplateRef<void> | null {
    return this.emptyTemplates?.first?.templateRef ?? null;
  }

  get cardTemplate(): TemplateRef<GridCardContext> | null {
    return this.cardTemplates?.first?.templateRef ?? null;
  }

  getSortDirection(columnKey: string): SortDirection {
    return this.activeSort.column === columnKey ? this.activeSort.direction : null;
  }

  onRowClick(event: MouseEvent, row: Record<string, any>): void {
    // Don't fire rowClick when user clicks interactive elements inside cells
    const target = event.target as HTMLElement;
    if (target.closest('button, a, input, select, textarea, [role="button"]')) {
      return;
    }
    this.rowClick.emit(row);
  }

  get hasRowClick(): boolean {
    return this.rowClick.observed;
  }

  scrollToTop(): void {
    this.scrollWrapperRef?.nativeElement?.scrollTo({ top: 0 });
  }

  scrollToRow(predicate: (row: any) => boolean): boolean {
    const index = this.data.findIndex(predicate);
    if (index === -1) return false;

    const wrapper = this.scrollWrapperRef?.nativeElement;
    if (!wrapper) return false;

    // Find the matching element in both desktop and mobile layouts
    const tableRows = wrapper.querySelectorAll<HTMLElement>('.data-grid__table tbody tr:not(.data-grid__skeleton-row)');
    const cards = wrapper.querySelectorAll<HTMLElement>('.data-grid__cards .data-grid__card:not(.data-grid__card--skeleton)');

    const targetRow = tableRows?.[index];
    const targetCard = cards?.[index];

    if (targetRow) {
      targetRow.scrollIntoView({ block: 'center' });
      this.applyHighlight(targetRow);
    }

    if (targetCard) {
      targetCard.scrollIntoView({ block: 'center' });
      this.applyHighlight(targetCard);
    }

    return true;
  }

  private applyHighlight(element: HTMLElement): void {
    element.classList.add('data-grid__highlight');
    setTimeout(() => {
      element.classList.remove('data-grid__highlight');
    }, 1500);
  }
}
