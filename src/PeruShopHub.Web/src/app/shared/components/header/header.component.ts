import { Component, inject, signal, HostListener, ElementRef } from '@angular/core';
import { ThemeService } from '../../../services/theme.service';
import { SidebarService } from '../../../services/sidebar.service';
import { NotificationService } from '../../../services/notification.service';
import { RouterLink } from '@angular/router';
import {
  LucideAngularModule,
  Menu,
  Search,
  Bell,
  Sun,
  Moon,
  Monitor,
  ChevronDown,
  Settings,
  LogOut,
  type LucideIconData,
} from 'lucide-angular';
import { NotificationPanelComponent } from '../notification-panel/notification-panel.component';
import { SearchPaletteComponent } from '../search-palette/search-palette.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [LucideAngularModule, RouterLink, NotificationPanelComponent, SearchPaletteComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  readonly theme = inject(ThemeService);
  readonly sidebar = inject(SidebarService);
  readonly notifications = inject(NotificationService);
  private readonly el = inject(ElementRef);

  readonly userMenuOpen = signal(false);
  readonly notificationPanelOpen = signal(false);
  readonly searchPaletteOpen = signal(false);

  // Icons
  readonly menuIcon = Menu;
  readonly searchIcon = Search;
  readonly bellIcon = Bell;
  readonly sunIcon = Sun;
  readonly moonIcon = Moon;
  readonly monitorIcon = Monitor;
  readonly chevronDownIcon = ChevronDown;
  readonly settingsIcon = Settings;
  readonly logOutIcon = LogOut;

  get themeIcon(): LucideIconData {
    const current = this.theme.currentTheme();
    if (current === 'dark') return this.moonIcon;
    if (current === 'system') return this.monitorIcon;
    return this.sunIcon;
  }

  get themeLabel(): string {
    const current = this.theme.currentTheme();
    if (current === 'dark') return 'Escuro';
    if (current === 'system') return 'Sistema';
    return 'Claro';
  }

  toggleSidebar(): void {
    if (window.innerWidth < 768) {
      this.sidebar.toggleMobile();
    } else {
      this.sidebar.toggle();
    }
  }

  toggleTheme(): void {
    this.theme.toggleTheme();
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update((v) => !v);
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  toggleNotificationPanel(): void {
    this.notificationPanelOpen.update((v) => !v);
    this.closeUserMenu();
  }

  closeNotificationPanel(): void {
    this.notificationPanelOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.userMenuOpen() && !this.el.nativeElement.contains(event.target)) {
      this.closeUserMenu();
    }
  }

  openSearchPalette(): void {
    this.searchPaletteOpen.set(true);
    this.closeUserMenu();
    this.closeNotificationPanel();
  }

  closeSearchPalette(): void {
    this.searchPaletteOpen.set(false);
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
      event.preventDefault();
      this.openSearchPalette();
    }
    if (event.key === 'Escape') {
      if (this.searchPaletteOpen()) {
        // search palette handles its own Esc
        return;
      }
      this.closeUserMenu();
    }
  }
}
