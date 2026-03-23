export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface KpiCard {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

export interface ChartDataPoint {
  label: string;
  value1: number;
  value2?: number;
}

export interface SearchResult {
  type: 'pedido' | 'produto' | 'cliente';
  id: string;
  primary: string;
  secondary: string;
  route: string;
}

export interface FileUploadResponse {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  sortOrder: number;
}

export interface DataChangeEvent {
  entityType: string;
  action: 'created' | 'updated' | 'deleted';
  entityId: string;
}
