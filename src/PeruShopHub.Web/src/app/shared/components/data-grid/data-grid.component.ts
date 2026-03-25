import {
  Component,
  Input,
  ChangeDetectionStrategy,
  ContentChildren,
  QueryList,
  Directive,
  TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';

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
  imports: [CommonModule],
  templateUrl: './data-grid.component.html',
  styleUrl: './data-grid.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataGridComponent {
  @Input({ required: true }) columns: GridColumn[] = [];
  @Input({ required: true }) data: Record<string, any>[] = [];

  @ContentChildren(GridCellDirective) cellTemplates!: QueryList<GridCellDirective>;
  @ContentChildren(GridHeaderDirective) headerTemplates!: QueryList<GridHeaderDirective>;

  getCellTemplate(columnKey: string): TemplateRef<GridCellContext> | null {
    const directive = this.cellTemplates?.find(d => d.appGridCell === columnKey);
    return directive ? directive.templateRef : null;
  }

  getHeaderTemplate(columnKey: string): TemplateRef<void> | null {
    const directive = this.headerTemplates?.find(d => d.appGridHeader === columnKey);
    return directive ? directive.templateRef : null;
  }
}
