import '../../../../test-setup';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import {
  DataGridComponent,
  GridColumn,
  GridCellDirective,
  GridEmptyDirective,
} from './data-grid.component';

// Mock IntersectionObserver which doesn't exist in jsdom
class MockIntersectionObserver {
  readonly root = null;
  readonly rootMargin = '';
  readonly thresholds: ReadonlyArray<number> = [];
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
  takeRecords = vi.fn().mockReturnValue([]);
}

vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);

const testColumns: GridColumn[] = [
  { key: 'name', label: 'Nome', sortable: true },
  { key: 'price', label: 'Preço', align: 'right', sortable: true },
  { key: 'status', label: 'Status' },
];

const testData = [
  { name: 'Product A', price: 100, status: 'Ativo' },
  { name: 'Product B', price: 200, status: 'Inativo' },
  { name: 'Product C', price: 150, status: 'Ativo' },
];

describe('DataGridComponent', () => {
  let component: DataGridComponent;
  let fixture: ComponentFixture<DataGridComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DataGridComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DataGridComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('empty state', () => {
    it('should show empty state when not loading and no data', () => {
      component.columns = testColumns;
      component.data = [];
      component.loading = false;
      fixture.detectChanges();

      expect(component.showEmpty).toBe(true);
      expect(component.showSkeleton).toBe(false);

      const emptyEl = fixture.debugElement.query(By.css('.data-grid__empty'));
      expect(emptyEl).toBeTruthy();
    });

    it('should show custom empty title and description', () => {
      component.columns = testColumns;
      component.data = [];
      component.loading = false;
      component.emptyTitle = 'Sem produtos';
      component.emptyDescription = 'Crie seu primeiro produto';
      fixture.detectChanges();

      const title = fixture.debugElement.query(By.css('.data-grid__empty-title'));
      const desc = fixture.debugElement.query(By.css('.data-grid__empty-description'));
      expect(title.nativeElement.textContent).toContain('Sem produtos');
      expect(desc.nativeElement.textContent).toContain('Crie seu primeiro produto');
    });

    it('should NOT show empty state while loading', () => {
      component.columns = testColumns;
      component.data = [];
      component.loading = true;
      fixture.detectChanges();

      expect(component.showEmpty).toBe(false);
      expect(component.showSkeleton).toBe(true);
    });
  });

  describe('skeleton loading', () => {
    it('should show skeleton rows when loading with no data', () => {
      component.columns = testColumns;
      component.data = [];
      component.loading = true;
      component.skeletonRows = 5;
      fixture.detectChanges();

      expect(component.showSkeleton).toBe(true);
      expect(component.skeletonRowsArray.length).toBe(5);
    });

    it('should NOT show skeleton when loading with existing data (infinite scroll)', () => {
      component.columns = testColumns;
      component.data = testData;
      component.loading = true;
      fixture.detectChanges();

      expect(component.showSkeleton).toBe(false);
    });
  });

  describe('sorting', () => {
    it('should cycle sort direction: asc → desc → null', () => {
      component.columns = testColumns;
      component.data = testData;
      fixture.detectChanges();

      const sortEvents: any[] = [];
      component.sortChange.subscribe((e: any) => sortEvents.push(e));

      const nameCol = testColumns[0]; // sortable

      // First click: asc
      component.onSortClick(nameCol);
      expect(component.activeSort).toEqual({ column: 'name', direction: 'asc' });
      expect(sortEvents[0]).toEqual({ column: 'name', direction: 'asc' });

      // Second click: desc
      component.onSortClick(nameCol);
      expect(component.activeSort).toEqual({ column: 'name', direction: 'desc' });
      expect(sortEvents[1]).toEqual({ column: 'name', direction: 'desc' });

      // Third click: null (reset)
      component.onSortClick(nameCol);
      expect(component.activeSort).toEqual({ column: '', direction: null });
      expect(sortEvents[2]).toEqual({ column: 'name', direction: null });
    });

    it('should reset to asc when sorting a different column', () => {
      component.columns = testColumns;
      component.data = testData;
      fixture.detectChanges();

      component.onSortClick(testColumns[0]); // name → asc
      component.onSortClick(testColumns[1]); // price → asc (reset)

      expect(component.activeSort).toEqual({ column: 'price', direction: 'asc' });
    });

    it('should ignore sort click on non-sortable column', () => {
      component.columns = testColumns;
      component.data = testData;
      fixture.detectChanges();

      const sortEvents: any[] = [];
      component.sortChange.subscribe((e: any) => sortEvents.push(e));

      component.onSortClick(testColumns[2]); // status — not sortable
      expect(sortEvents.length).toBe(0);
    });

    it('should return correct sort direction for a column', () => {
      component.activeSort = { column: 'name', direction: 'asc' };
      expect(component.getSortDirection('name')).toBe('asc');
      expect(component.getSortDirection('price')).toBeNull();
    });
  });

  describe('data rendering', () => {
    it('should render data rows in the table', () => {
      component.columns = testColumns;
      component.data = testData;
      component.loading = false;
      fixture.detectChanges();

      const rows = fixture.debugElement.queryAll(
        By.css('.data-grid__desktop-only tbody tr:not(.data-grid__skeleton-row)')
      );
      expect(rows.length).toBe(3);
    });

    it('should render column headers', () => {
      component.columns = testColumns;
      component.data = testData;
      fixture.detectChanges();

      const headers = fixture.debugElement.queryAll(By.css('.data-grid__desktop-only th'));
      expect(headers.length).toBe(3);
      expect(headers[0].nativeElement.textContent).toContain('Nome');
      expect(headers[1].nativeElement.textContent).toContain('Preço');
    });
  });

  describe('row click', () => {
    it('should emit rowClick when a row is clicked', () => {
      component.columns = testColumns;
      component.data = testData;
      fixture.detectChanges();

      const clickEvents: any[] = [];
      component.rowClick.subscribe((e: any) => clickEvents.push(e));

      // Simulate click on a cell element
      const mockEvent = { target: document.createElement('td') } as unknown as MouseEvent;
      (mockEvent.target as HTMLElement).closest = () => null;

      component.onRowClick(mockEvent, testData[0]);
      expect(clickEvents[0]).toEqual(testData[0]);
    });

    it('should NOT emit rowClick when clicking a button inside a row', () => {
      component.columns = testColumns;
      component.data = testData;
      fixture.detectChanges();

      const clickEvents: any[] = [];
      component.rowClick.subscribe((e: any) => clickEvents.push(e));

      const btn = document.createElement('button');
      const mockEvent = { target: btn } as unknown as MouseEvent;
      (mockEvent.target as HTMLElement).closest = (sel: string) =>
        sel.includes('button') ? btn : null;

      component.onRowClick(mockEvent, testData[0]);
      expect(clickEvents.length).toBe(0);
    });
  });

  describe('footer', () => {
    it('should show footer when totalCount > 0 and not loading skeleton', () => {
      component.columns = testColumns;
      component.data = testData;
      component.totalCount = 10;
      component.loading = false;
      fixture.detectChanges();

      expect(component.showFooter).toBe(true);
    });

    it('should format count with pt-BR locale', () => {
      expect(component.formatCount(1234)).toBe('1.234');
    });

    it('should detect when all data is loaded', () => {
      component.data = testData;
      component.totalCount = 3;
      expect(component.allLoaded).toBe(true);

      component.totalCount = 10;
      expect(component.allLoaded).toBe(false);
    });
  });
});
