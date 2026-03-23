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

export interface ProductRow {
  id: number;
  name: string;
  sku: string;
  sales: number;
  revenue: number;
  profit: number;
  margin: number;
  imageUrl?: string;
}

export interface PendingAction {
  id: string;
  type: string;
  title: string;
  label: string;
  description: string;
  severity: 'info' | 'warning' | 'danger';
  variant: string;
  count: number;
  actionLabel: string;
  route?: string;
}

export interface CostBreakdownItem {
  label: string;
  value: number;
  color: string;
}

export interface SkuProfitability {
  sku: string;
  nome: string;
  produto: string;
  vendas: number;
  receita: number;
  cmv: number;
  comissoes: number;
  frete: number;
  impostos: number;
  lucro: number;
  margem: number;
  [key: string]: any;
}

export interface ReconciliationRow {
  mes: string;
  periodo: string;
  valorEsperado: number;
  valorDepositado: number;
  diferenca: number;
  status: string;
}

export interface AbcProduct {
  rank: number;
  sku: string;
  nome: string;
  produto: string;
  vendas: number;
  receita: number;
  lucro: number;
  percentLucro: number;
  percentual: number;
  acumulado: number;
  classificacao: 'A' | 'B' | 'C';
}
