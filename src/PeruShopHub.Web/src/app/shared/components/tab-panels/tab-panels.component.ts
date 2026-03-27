import {
  Component,
  Input,
  Output,
  EventEmitter,
  signal,
  ContentChildren,
  QueryList,
  AfterContentInit,
  TemplateRef,
  Directive,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LucideAngularModule, ChevronDown, ChevronUp } from 'lucide-angular';
import { TabBarComponent, type TabItem } from '../tab-bar/tab-bar.component';

/**
 * Directive to mark a template as a tab panel.
 * Usage: <ng-template appTabPanel="key"> ... </ng-template>
 */
@Directive({
  selector: '[appTabPanel]',
  standalone: true,
})
export class TabPanelDirective {
  @Input({ required: true, alias: 'appTabPanel' }) key!: string;

  constructor(public templateRef: TemplateRef<unknown>) {}
}

/**
 * TabPanelsComponent — wraps app-tab-bar and provides:
 * - Desktop: horizontal tab bar with panel switching
 * - Mobile: accordion-style collapsible sections
 * - Content projection via appTabPanel directive
 *
 * Modes:
 * - `lazy=false` (default): All panels are rendered, CSS toggles visibility.
 *   Good for forms where you want to preserve state across tabs.
 * - `lazy=true`: Only the active panel is rendered via @if.
 *   Good for data-heavy pages where inactive tabs should be destroyed.
 *
 * Usage:
 * <app-tab-panels [tabs]="tabs" [(activeTab)]="activeTab" [lazy]="true">
 *   <ng-template appTabPanel="resumo"> ... </ng-template>
 *   <ng-template appTabPanel="detalhe"> ... </ng-template>
 * </app-tab-panels>
 */
@Component({
  selector: 'app-tab-panels',
  standalone: true,
  imports: [NgTemplateOutlet, TabBarComponent, LucideAngularModule],
  template: `
    <!-- Desktop: tab bar (hidden on mobile) -->
    <div class="tab-panels__nav">
      <app-tab-bar
        [tabs]="tabs"
        [activeTab]="activeTab"
        (tabChange)="onTabChange($event)"
      ></app-tab-bar>
    </div>

    <!-- Panels -->
    @for (tab of tabs; track tab.key) {
      <!-- Mobile: accordion header -->
      <button
        class="tab-panels__accordion"
        (click)="toggleAccordion(tab.key)"
        [attr.aria-expanded]="isAccordionOpen(tab.key)">
        <span>{{ tab.label }}</span>
        <lucide-icon [img]="isAccordionOpen(tab.key) ? chevronUpIcon : chevronDownIcon" [size]="20"></lucide-icon>
      </button>

      <!-- Panel content -->
      @if (getPanelTemplate(tab.key); as tmpl) {
        @if (lazy) {
          <!-- Lazy: only render the active panel (desktop) or open accordion (mobile) -->
          @if (activeTab === tab.key || isAccordionOpen(tab.key)) {
            <div class="tab-panels__panel tab-panels__panel--active tab-panels__panel--accordion-open">
              <ng-container [ngTemplateOutlet]="tmpl"></ng-container>
            </div>
          }
        } @else {
          <!-- Eager: all panels rendered, CSS toggles visibility -->
          <div
            class="tab-panels__panel"
            [class.tab-panels__panel--active]="activeTab === tab.key"
            [class.tab-panels__panel--accordion-open]="isAccordionOpen(tab.key)">
            <ng-container [ngTemplateOutlet]="tmpl"></ng-container>
          </div>
        }
      }
    }
  `,
  styleUrl: './tab-panels.component.scss',
})
export class TabPanelsComponent implements AfterContentInit {
  @Input({ required: true }) tabs: TabItem[] = [];
  @Input() activeTab = '';
  @Input() lazy = false;
  @Output() activeTabChange = new EventEmitter<string>();

  @ContentChildren(TabPanelDirective) panels!: QueryList<TabPanelDirective>;

  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;

  private panelMap = new Map<string, TemplateRef<unknown>>();
  private openAccordions = signal<Set<string>>(new Set());

  ngAfterContentInit(): void {
    this.buildPanelMap();
    this.panels.changes.subscribe(() => this.buildPanelMap());

    // Open the active tab's accordion by default
    if (this.activeTab) {
      this.openAccordions.set(new Set([this.activeTab]));
    }
  }

  private buildPanelMap(): void {
    this.panelMap.clear();
    this.panels.forEach(p => this.panelMap.set(p.key, p.templateRef));
  }

  getPanelTemplate(key: string): TemplateRef<unknown> | null {
    return this.panelMap.get(key) ?? null;
  }

  onTabChange(key: string): void {
    this.activeTab = key;
    this.activeTabChange.emit(key);
  }

  toggleAccordion(key: string): void {
    const current = new Set(this.openAccordions());
    if (current.has(key)) {
      current.delete(key);
    } else {
      current.add(key);
    }
    this.openAccordions.set(current);
  }

  isAccordionOpen(key: string): boolean {
    return this.openAccordions().has(key);
  }
}
