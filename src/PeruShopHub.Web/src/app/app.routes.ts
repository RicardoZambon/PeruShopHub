import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { tenantGuard } from './guards/tenant.guard';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { superAdminGuard } from './guards/super-admin.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
  },
  {
    path: 'oauth-callback',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/oauth-callback/oauth-callback.component').then(m => m.OAuthCallbackComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shared/components/layout/layout.component').then(m => m.LayoutComponent),
    canActivate: [authGuard, tenantGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'produtos',
        loadComponent: () =>
          import('./pages/products/products-list.component').then(m => m.ProductsListComponent),
      },
      {
        path: 'produtos/novo',
        loadComponent: () =>
          import('./pages/products/product-form.component').then(m => m.ProductFormComponent),
        canDeactivate: [unsavedChangesGuard],
      },
      {
        path: 'produtos/:id',
        loadComponent: () =>
          import('./pages/products/product-detail.component').then(m => m.ProductDetailComponent),
      },
      {
        path: 'produtos/:id/editar',
        loadComponent: () =>
          import('./pages/products/product-form.component').then(m => m.ProductFormComponent),
        canDeactivate: [unsavedChangesGuard],
      },
      {
        path: 'categorias',
        loadComponent: () =>
          import('./pages/categories/categories.component').then(m => m.CategoriesComponent),
      },
      {
        path: 'vendas',
        loadComponent: () =>
          import('./pages/sales/sales-list.component').then(m => m.SalesListComponent),
      },
      {
        path: 'vendas/:id',
        loadComponent: () =>
          import('./pages/sales/sale-detail.component').then(m => m.SaleDetailComponent),
      },
      {
        path: 'perguntas',
        loadComponent: () =>
          import('./pages/questions/questions.component').then(m => m.QuestionsComponent),
      },
      {
        path: 'clientes',
        loadComponent: () =>
          import('./pages/customers/customers.component').then(m => m.CustomersComponent),
      },
      {
        path: 'clientes/:id',
        loadComponent: () =>
          import('./pages/customers/customer-detail.component').then(m => m.CustomerDetailComponent),
      },
      {
        path: 'anuncios',
        loadComponent: () =>
          import('./pages/listings/listings.component').then(m => m.ListingsComponent),
      },
      {
        path: 'suprimentos',
        loadComponent: () =>
          import('./pages/supplies/supplies.component').then(m => m.SuppliesComponent),
      },
      {
        path: 'compras',
        loadComponent: () =>
          import('./pages/purchase-orders/purchase-orders-list.component').then(m => m.PurchaseOrdersListComponent),
      },
      {
        path: 'compras/novo',
        loadComponent: () =>
          import('./pages/purchase-orders/purchase-order-form.component').then(m => m.PurchaseOrderFormComponent),
      },
      {
        path: 'compras/:id',
        loadComponent: () =>
          import('./pages/purchase-orders/purchase-order-detail.component').then(m => m.PurchaseOrderDetailComponent),
      },
      {
        path: 'financeiro',
        loadComponent: () =>
          import('./pages/finance/finance.component').then(m => m.FinanceComponent),
      },
      {
        path: 'simulador',
        loadComponent: () =>
          import('./pages/simulator/simulator.component').then(m => m.SimulatorComponent),
      },
      {
        path: 'estoque',
        loadComponent: () =>
          import('./pages/inventory/inventory.component').then(m => m.InventoryComponent),
      },
      {
        path: 'configuracoes',
        loadComponent: () =>
          import('./pages/settings/settings.component').then(m => m.SettingsComponent),
      },
      {
        path: 'configuracoes/log-atividades',
        loadComponent: () =>
          import('./pages/audit-log/audit-log.component').then(m => m.AuditLogComponent),
      },
      {
        path: 'admin/tenants',
        canActivate: [superAdminGuard],
        loadComponent: () =>
          import('./pages/admin/admin-tenants.component').then(m => m.AdminTenantsComponent),
      },
      {
        path: '**',
        loadComponent: () =>
          import('./pages/not-found/not-found.component').then(m => m.NotFoundComponent),
      },
    ],
  },
];
