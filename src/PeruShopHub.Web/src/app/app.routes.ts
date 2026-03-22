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
          import('./pages/produtos/produtos-list.component').then(m => m.ProdutosListComponent),
      },
      {
        path: 'produtos/novo',
        loadComponent: () =>
          import('./pages/produtos/produto-form.component').then(m => m.ProdutoFormComponent),
        canDeactivate: [unsavedChangesGuard],
      },
      {
        path: 'produtos/:id',
        loadComponent: () =>
          import('./pages/produtos/produto-detail.component').then(m => m.ProdutoDetailComponent),
      },
      {
        path: 'produtos/:id/editar',
        loadComponent: () =>
          import('./pages/produtos/produto-form.component').then(m => m.ProdutoFormComponent),
        canDeactivate: [unsavedChangesGuard],
      },
      {
        path: 'categorias',
        loadComponent: () =>
          import('./pages/categorias/categorias.component').then(m => m.CategoriasComponent),
      },
      {
        path: 'vendas',
        loadComponent: () =>
          import('./pages/vendas/vendas-list.component').then(m => m.VendasListComponent),
      },
      {
        path: 'vendas/:id',
        loadComponent: () =>
          import('./pages/vendas/venda-detail.component').then(m => m.VendaDetailComponent),
      },
      {
        path: 'perguntas',
        loadComponent: () =>
          import('./pages/perguntas/perguntas.component').then(m => m.PerguntasComponent),
      },
      {
        path: 'clientes',
        loadComponent: () =>
          import('./pages/clientes/clientes.component').then(m => m.ClientesComponent),
      },
      {
        path: 'financeiro',
        loadComponent: () =>
          import('./pages/financeiro/financeiro.component').then(m => m.FinanceiroComponent),
      },
      {
        path: 'estoque',
        loadComponent: () =>
          import('./pages/estoque/estoque.component').then(m => m.EstoqueComponent),
      },
      {
        path: 'configuracoes',
        loadComponent: () =>
          import('./pages/configuracoes/configuracoes.component').then(m => m.ConfiguracoesComponent),
      },
      {
        path: '**',
        loadComponent: () =>
          import('./pages/not-found/not-found.component').then(m => m.NotFoundComponent),
      },
    ],
  },
];
