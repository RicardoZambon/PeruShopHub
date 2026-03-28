import { Component, inject, computed, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import {
  LayoutDashboard,
  Package,
  FolderTree,
  ShoppingCart,
  MessageCircle,
  Users,
  DollarSign,
  Warehouse,
  Settings,
  ChevronsLeft,
  ChevronsRight,
  X,
  Megaphone,
  PackageOpen,
  ClipboardList,
  Shield,
  Calculator,
} from 'lucide-angular';
import { SidebarService } from '../../../services/sidebar.service';
import { AuthService } from '../../../services/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: LucideIconData;
  group?: string;
}

interface NavGroup {
  label: string | null;
  items: NavItem[];
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
  private readonly auth = inject(AuthService);

  readonly shopName = computed(() => this.auth.tenantName() || 'PeruShopHub');
  readonly isSuperAdmin = computed(() => this.auth.isSuperAdmin());
  readonly adminIcon = Shield;

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: LayoutDashboard },

    { label: 'Vendas', route: '/vendas', icon: ShoppingCart, group: 'COMERCIAL' },
    { label: 'Perguntas', route: '/perguntas', icon: MessageCircle, group: 'COMERCIAL' },
    { label: 'Anúncios', route: '/anuncios', icon: Megaphone, group: 'COMERCIAL' },
    { label: 'Clientes', route: '/clientes', icon: Users, group: 'COMERCIAL' },

    { label: 'Produtos', route: '/produtos', icon: Package, group: 'CATÁLOGO' },
    { label: 'Categorias', route: '/categorias', icon: FolderTree, group: 'CATÁLOGO' },
    { label: 'Estoque', route: '/estoque', icon: Warehouse, group: 'CATÁLOGO' },
    { label: 'Suprimentos', route: '/suprimentos', icon: PackageOpen, group: 'CATÁLOGO' },
    { label: 'Compras', route: '/compras', icon: ClipboardList, group: 'CATÁLOGO' },
    { label: 'Financeiro', route: '/financeiro', icon: DollarSign, group: 'CATÁLOGO' },
    { label: 'Simulador', route: '/simulador', icon: Calculator, group: 'CATÁLOGO' },

    { label: 'Configurações', route: '/configuracoes', icon: Settings },
  ];

  /** Group nav items by their group property for template rendering */
  readonly navGroups: NavGroup[] = this.buildNavGroups();

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

  private buildNavGroups(): NavGroup[] {
    const groups: NavGroup[] = [];
    let currentGroup: string | null | undefined = undefined;

    for (const item of this.navItems) {
      const group = item.group ?? null;
      if (group !== currentGroup) {
        groups.push({ label: group, items: [item] });
        currentGroup = group;
      } else {
        groups[groups.length - 1].items.push(item);
      }
    }

    return groups;
  }
}
