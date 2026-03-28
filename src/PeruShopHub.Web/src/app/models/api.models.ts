export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface KpiCard {
  title: string;
  value: string;
  previousValue: string | null;
  changePercent: number | null;
  changeDirection: string | null;
  icon: string | null;
}

export interface ChartDataPoint {
  label: string;
  value: number;
  secondaryValue?: number | null;
}

export interface DashboardSummary {
  kpis: KpiCard[];
  topProducts: ProductRanking[];
  pendingActions: PendingAction[];
  revenueChart: ChartDataPoint[];
  ordersChart: ChartDataPoint[];
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

export interface ProductRanking {
  id: string;
  name: string;
  sku: string;
  quantitySold: number;
  revenue: number;
  profit: number;
  margin: number;
}

export interface PendingAction {
  type: string;
  title: string;
  description: string;
  navigationTarget: string | null;
  count: number;
}

export interface CostBreakdownItem {
  category: string;
  total: number;
  percentage: number;
  color: string | null;
}

export interface MarginChartPoint {
  label: string;
  margin: number;
}

export interface FinanceSummary {
  totalRevenue: number;
  totalCosts: number;
  totalProfit: number;
  averageMargin: number;
  averageTicket: number;
  revenueChange: number;
  profitChange: number;
  costBreakdown: CostBreakdownItem[];
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

export interface AbcProductApi {
  productId: string;
  sku: string;
  name: string;
  revenue: number;
  profit: number;
  margin: number;
  cumulativePercentage: number;
  classification: 'A' | 'B' | 'C';
}

export interface AbcProduct {
  rank: number;
  sku: string;
  nome: string;
  produto: string;
  receita: number;
  lucro: number;
  margem: number;
  percentLucro: number;
  acumulado: number;
  classificacao: 'A' | 'B' | 'C';
}
