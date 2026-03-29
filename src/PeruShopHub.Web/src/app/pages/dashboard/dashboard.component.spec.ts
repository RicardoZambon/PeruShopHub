import '../../../test-setup';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { DashboardService } from '../../services/dashboard.service';
import { OnboardingService } from '../../services/onboarding.service';
import type { KpiCard, ChartDataPoint, CostBreakdownItem, ProductRanking, PendingAction, DashboardSummary } from '../../models/api.models';

const mockKpis: KpiCard[] = [
  { title: 'Receita Bruta', value: '15000', previousValue: '12000', changePercent: 25, changeDirection: 'up', icon: null },
  { title: 'Vendas', value: '42', previousValue: '35', changePercent: 20, changeDirection: 'up', icon: null },
  { title: 'Total Custos', value: '8000', previousValue: '7000', changePercent: 14.3, changeDirection: 'up', icon: null },
  { title: 'Margem Media', value: '22.5', previousValue: '20.0', changePercent: 12.5, changeDirection: 'up', icon: null },
];

const mockSummary: DashboardSummary = {
  kpis: mockKpis,
  topProducts: [],
  pendingActions: [],
  revenueChart: [],
  ordersChart: [],
};

const mockChart: ChartDataPoint[] = [
  { label: '01/03', value: 5000, secondaryValue: 2000 },
  { label: '02/03', value: 6000, secondaryValue: 2500 },
];

const mockCosts: CostBreakdownItem[] = [
  { category: 'Comissão', total: 3000, percentage: 37.5, color: null },
  { category: 'Frete', total: 2000, percentage: 25, color: null },
];

const mockTop: ProductRanking[] = [
  { id: '1', name: 'Product A', sku: 'SKU-A', quantitySold: 10, revenue: 5000, profit: 2000, margin: 40 },
];

const mockPending: PendingAction[] = [
  { type: 'question', title: '3 perguntas', description: 'Sem resposta', navigationTarget: '/perguntas', count: 3 },
];

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let dashboardService: any;
  let router: Router;

  beforeEach(async () => {
    dashboardService = {
      getSummary: vi.fn().mockReturnValue(of(mockSummary)),
      getRevenueProfit: vi.fn().mockReturnValue(of(mockChart)),
      getCostBreakdown: vi.fn().mockReturnValue(of(mockCosts)),
      getTopProducts: vi.fn().mockReturnValue(of(mockTop)),
      getLeastProfitable: vi.fn().mockReturnValue(of([])),
      getPendingActions: vi.fn().mockReturnValue(of(mockPending)),
    };

    const onboardingService = {
      getProgress: vi.fn().mockReturnValue(of({
        isCompleted: true,
        stepsCompleted: ['profile', 'connect_ml'],
        steps: [],
        completedAt: null,
      })),
    };

    // We test the component class directly without rendering (avoids chart.js canvas issues)
    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardService },
        { provide: OnboardingService, useValue: onboardingService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    // Instantiate the component class via DI without rendering the template
    component = TestBed.runInInjectionContext(() => new DashboardComponent());
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load data on init', () => {
    component.ngOnInit();

    expect(dashboardService.getSummary).toHaveBeenCalledWith('30dias');
    expect(dashboardService.getRevenueProfit).toHaveBeenCalledWith(30);
    expect(dashboardService.getCostBreakdown).toHaveBeenCalledWith('30dias');
    expect(dashboardService.getTopProducts).toHaveBeenCalledWith(5, '30dias');
    expect(dashboardService.getPendingActions).toHaveBeenCalled();
  });

  it('should map KPIs after loading', () => {
    component.ngOnInit();

    const kpis = component.kpis();
    expect(kpis.length).toBe(4);
    expect(kpis[0].label).toBe('Receita Bruta');
    expect(kpis[0].change).toBe(25);
  });

  it('should format Vendas KPI as plain number', () => {
    component.ngOnInit();

    const vendasKpi = component.kpis().find(k => k.label === 'Vendas');
    expect(vendasKpi?.value).toBe('42');
  });

  it('should format Margem Media as percentage', () => {
    component.ngOnInit();

    const margemKpi = component.kpis().find(k => k.label === 'Margem Media');
    expect(margemKpi?.value).toBe('22.5%');
  });

  it('should invert colors for cost KPIs', () => {
    component.ngOnInit();

    const custosKpi = component.kpis().find(k => k.label === 'Total Custos');
    expect(custosKpi?.invertColors).toBe(true);

    const receitaKpi = component.kpis().find(k => k.label === 'Receita Bruta');
    expect(receitaKpi?.invertColors).toBe(false);
  });

  it('should update donut chart total', () => {
    component.ngOnInit();
    expect(component.donutTotal()).toBe(5000);
  });

  it('should populate top profitable products', () => {
    component.ngOnInit();
    expect(component.topProfitable().length).toBe(1);
    expect(component.topProfitable()[0].name).toBe('Product A');
  });

  it('should populate pending actions', () => {
    component.ngOnInit();
    expect(component.pendingActions().length).toBe(1);
    expect(component.pendingActions()[0].type).toBe('question');
  });

  it('should change period and reload', () => {
    component.ngOnInit();
    vi.clearAllMocks();

    dashboardService.getSummary.mockReturnValue(of(mockSummary));
    dashboardService.getRevenueProfit.mockReturnValue(of(mockChart));
    dashboardService.getCostBreakdown.mockReturnValue(of(mockCosts));
    dashboardService.getTopProducts.mockReturnValue(of(mockTop));
    dashboardService.getLeastProfitable.mockReturnValue(of([]));
    dashboardService.getPendingActions.mockReturnValue(of(mockPending));

    component.selectPeriod('7dias');

    expect(component.activePeriod()).toBe('7dias');
    expect(dashboardService.getSummary).toHaveBeenCalledWith('7dias');
    expect(dashboardService.getRevenueProfit).toHaveBeenCalledWith(7);
  });

  it('should navigate on product click', () => {
    component.onProductClick('prod-123');
    expect(router.navigate).toHaveBeenCalledWith(['/produtos', 'prod-123']);
  });

  it('should return correct pending action variant', () => {
    expect(component.getPendingActionVariant({ type: 'question' } as any)).toBe('accent');
    expect(component.getPendingActionVariant({ type: 'order' } as any)).toBe('warning');
    expect(component.getPendingActionVariant({ type: 'stock_alert' } as any)).toBe('danger');
  });

  it('should handle API error gracefully', () => {
    dashboardService.getSummary.mockReturnValue(throwError(() => new Error('fail')));
    dashboardService.getRevenueProfit.mockReturnValue(throwError(() => new Error('fail')));
    dashboardService.getCostBreakdown.mockReturnValue(throwError(() => new Error('fail')));
    dashboardService.getTopProducts.mockReturnValue(throwError(() => new Error('fail')));
    dashboardService.getLeastProfitable.mockReturnValue(throwError(() => new Error('fail')));
    dashboardService.getPendingActions.mockReturnValue(throwError(() => new Error('fail')));

    expect(() => component.ngOnInit()).not.toThrow();
    expect(component.loading()).toBe(false);
  });

  it('should have period definitions', () => {
    expect(component.periods.length).toBe(5);
    expect(component.periods.map(p => p.key)).toEqual(['hoje', '7dias', '30dias', 'estemes', 'personalizado']);
  });

  it('should use hoje as 1-day period', () => {
    component.selectPeriod('hoje');
    expect(dashboardService.getRevenueProfit).toHaveBeenCalledWith(1);
  });
});
