import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from './shared/components';
import { CookieConsentBannerComponent } from './shared/components';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent, CookieConsentBannerComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('PeruShopHub.Web');
}
