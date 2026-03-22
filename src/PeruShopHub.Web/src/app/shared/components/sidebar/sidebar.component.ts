import { Component, inject, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import {
  LayoutDashboard,
  Package,
  ShoppingCart,
  MessageCircle,
  Users,
  DollarSign,
  Warehouse,
  Settings,
  ChevronsLeft,
  ChevronsRight,
  X,
} from 'lucide-angular';
import { SidebarService } from '../../../services/sidebar.service';

interface NavItem {
  label: string;
  route: string;
  icon: LucideIconData;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {
  readonly sidebar = inject(SidebarService);

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: LayoutDashboard },
    { label: 'Produtos', route: '/produtos', icon: Package },
    { label: 'Vendas', route: '/vendas', icon: ShoppingCart },
    { label: 'Perguntas', route: '/perguntas', icon: MessageCircle },
    { label: 'Clientes', route: '/clientes', icon: Users },
    { label: 'Financeiro', route: '/financeiro', icon: DollarSign },
    { label: 'Estoque', route: '/estoque', icon: Warehouse },
    { label: 'Configurações', route: '/configuracoes', icon: Settings },
  ];

  readonly collapseIcon = ChevronsLeft;
  readonly expandIcon = ChevronsRight;
  readonly closeIcon = X;

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.sidebar.closeMobile();
  }

  onBackdropClick(): void {
    this.sidebar.closeMobile();
  }

  onNavClick(): void {
    // Close mobile drawer on navigation
    if (this.sidebar.mobileOpen()) {
      this.sidebar.closeMobile();
    }
  }
}
