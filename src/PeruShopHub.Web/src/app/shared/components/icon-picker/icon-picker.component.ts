import { Component, Input, Output, EventEmitter, signal, computed, HostListener, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, X } from 'lucide-angular';

const MATERIAL_ICONS = [
  'home', 'search', 'settings', 'favorite', 'star', 'check_circle', 'delete', 'add',
  'shopping_cart', 'person', 'visibility', 'lock', 'schedule', 'language', 'help',
  'info', 'warning', 'error', 'cloud', 'file_download', 'file_upload', 'folder',
  'create', 'mail', 'call', 'chat', 'notifications', 'share', 'bookmark',
  // Devices & Electronics
  'devices', 'smartphone', 'tablet', 'laptop', 'computer', 'desktop_windows',
  'keyboard', 'mouse', 'headphones', 'speaker', 'tv', 'monitor', 'watch',
  'phonelink_ring', 'battery_full', 'bluetooth', 'wifi', 'usb', 'memory',
  'sd_card', 'sim_card', 'router', 'print', 'scanner', 'camera',
  // Shopping & Commerce
  'store', 'storefront', 'shopping_bag', 'local_offer', 'sell', 'payments',
  'credit_card', 'receipt', 'receipt_long', 'loyalty', 'redeem', 'card_giftcard',
  'price_check', 'currency_exchange', 'account_balance_wallet', 'savings',
  'point_of_sale', 'qr_code_scanner', 'barcode', 'inventory', 'package',
  // Fashion & Clothing
  'checkroom', 'dry_cleaning', 'iron', 'local_laundry_service',
  // Home & Living
  'chair', 'table_restaurant', 'bed', 'bathtub', 'kitchen', 'dining',
  'living', 'weekend', 'light', 'lightbulb', 'power', 'electrical_services',
  'door_front', 'window', 'roofing', 'deck', 'fence', 'garage',
  // Sports & Outdoors
  'sports_soccer', 'sports_basketball', 'sports_tennis', 'sports_esports',
  'fitness_center', 'pool', 'surfing', 'hiking', 'camping', 'kayaking',
  'skateboarding', 'snowboarding', 'cycling', 'running',
  // Beauty & Health
  'spa', 'self_improvement', 'health_and_safety', 'medical_services',
  'medication', 'vaccines', 'healing', 'psychology', 'face',
  // Automotive
  'directions_car', 'two_wheeler', 'local_gas_station', 'car_repair',
  'tire_repair', 'oil_barrel', 'car_crash',
  // Kids & Toys
  'toys', 'child_friendly', 'child_care', 'stroller', 'smart_toy',
  // Books & Education
  'menu_book', 'auto_stories', 'library_books', 'school', 'science',
  // Food & Drink
  'restaurant', 'local_cafe', 'local_bar', 'bakery_dining', 'lunch_dining',
  'fastfood', 'icecream', 'local_pizza', 'ramen_dining', 'set_meal',
  'coffee', 'wine_bar', 'liquor',
  // Pets & Animals
  'pets', 'cruelty_free',
  // Garden & Nature
  'yard', 'park', 'forest', 'grass', 'eco', 'water_drop', 'compost',
  // Tools & Hardware
  'build', 'construction', 'handyman', 'plumbing', 'hardware',
  'carpenter', 'precision_manufacturing',
  // Office & Business
  'business', 'business_center', 'work', 'badge', 'groups',
  'meeting_room', 'domain',
  // Arts & Entertainment
  'music_note', 'headset', 'movie', 'photo_camera', 'videocam',
  'palette', 'brush', 'draw', 'theater_comedy',
  // Stationery
  'edit_note', 'sticky_note_2', 'note', 'article', 'description',
  // Phone & Telecom
  'phone_android', 'phone_iphone', 'smartphone', 'perm_phone_msg',
  // Photography
  'photo_camera', 'camera_alt', 'photo_library', 'image', 'panorama',
  // Travel
  'flight', 'hotel', 'luggage', 'beach_access', 'map',
  // Misc
  'category', 'label', 'tag', 'grade', 'thumb_up', 'emoji_events',
  'celebration', 'rocket_launch', 'diamond', 'token',
  'workspace_premium', 'verified', 'new_releases', 'auto_awesome',
];

// Deduplicate
const ICON_LIST = [...new Set(MATERIAL_ICONS)];

@Component({
  selector: 'app-icon-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './icon-picker.component.html',
  styleUrl: './icon-picker.component.scss',
})
export class IconPickerComponent {
  @Input() value: string | null = null;
  @Input() size = 24;
  @Output() valueChange = new EventEmitter<string | null>();

  private readonly el = inject(ElementRef);
  readonly closeIcon = X;

  readonly open = signal(false);
  readonly search = signal('');

  readonly filteredIcons = computed(() => {
    const q = this.search().toLowerCase().trim();
    if (!q) return ICON_LIST;
    return ICON_LIST.filter(icon => icon.includes(q));
  });

  toggle(): void {
    this.open.update(v => !v);
    if (this.open()) {
      this.search.set('');
    }
  }

  selectIcon(icon: string): void {
    this.value = icon;
    this.valueChange.emit(icon);
    this.open.set(false);
  }

  clearIcon(event: MouseEvent): void {
    event.stopPropagation();
    this.value = null;
    this.valueChange.emit(null);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target)) {
      this.open.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(false);
  }
}
