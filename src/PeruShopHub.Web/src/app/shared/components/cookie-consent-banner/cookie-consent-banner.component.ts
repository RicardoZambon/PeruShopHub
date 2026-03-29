import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CookieConsentService } from '../../../services/cookie-consent.service';
import { ButtonComponent } from '../button/button.component';
import { ToggleSwitchComponent } from '../toggle-switch/toggle-switch.component';

@Component({
  selector: 'app-cookie-consent-banner',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonComponent, ToggleSwitchComponent],
  templateUrl: './cookie-consent-banner.component.html',
  styleUrl: './cookie-consent-banner.component.scss',
})
export class CookieConsentBannerComponent {
  private readonly consentService = inject(CookieConsentService);

  readonly showBanner = this.consentService.showBanner;
  readonly showSettings = signal(false);
  readonly analyticsToggle = signal(false);

  acceptAll(): void {
    this.consentService.acceptAll();
  }

  acceptEssentialOnly(): void {
    this.consentService.acceptEssentialOnly();
  }

  openSettings(): void {
    this.showSettings.set(true);
  }

  saveSettings(): void {
    this.consentService.saveCustom(this.analyticsToggle());
    this.showSettings.set(false);
  }

  onAnalyticsToggle(value: boolean): void {
    this.analyticsToggle.set(value);
  }
}
