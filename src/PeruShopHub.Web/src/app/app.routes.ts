import { Routes } from '@angular/router';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shared/components/layout/layout.component').then(m => m.LayoutComponent),
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
        path: 'financeiro',
        loadComponent: () =>
          import('./pages/finance/finance.component').then(m => m.FinanceComponent),
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
        path: '**',
        loadComponent: () =>
          import('./pages/not-found/not-found.component').then(m => m.NotFoundComponent),
      },
    ],
  },
];
