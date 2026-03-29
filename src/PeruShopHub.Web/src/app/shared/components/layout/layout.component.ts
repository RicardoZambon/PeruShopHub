import { Component, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { SidebarService } from '../../../services/sidebar.service';
import { CookieConsentService } from '../../../services/cookie-consent.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, SidebarComponent, HeaderComponent, ConfirmDialogComponent],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss',
  host: {
    '[class.sidebar-collapsed]': 'sidebar.collapsed()',
    '[class.sidebar-expanded]': '!sidebar.collapsed()',
  },
})
export class LayoutComponent {
  readonly sidebar = inject(SidebarService);
  private readonly cookieConsent = inject(CookieConsentService);

  openCookieSettings(): void {
    this.cookieConsent.resetConsent();
  }
}
