import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, ChevronRight, ChevronDown } from 'lucide-angular';
import type { Category } from '../../models/category.model';

@Component({
  selector: 'app-category-tree-node',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './category-tree-node.component.html',
  styleUrl: './category-tree-node.component.scss',
})
export class CategoryTreeNodeComponent {
  @Input({ required: true }) category!: Category;
  @Input() selectedId: string | null = null;
  @Input() depth = 0;

  @Output() select = new EventEmitter<string>();
  @Output() toggleExpand = new EventEmitter<string>();

  readonly chevronRightIcon = ChevronRight;
  readonly chevronDownIcon = ChevronDown;

  expanded = signal(false);

  ngOnInit(): void {
    // Root-level items start expanded
    if (this.depth === 0) {
      this.expanded.set(true);
    }
  }

  get isSelected(): boolean {
    return this.selectedId === this.category.id;
  }

  get hasChildren(): boolean {
    return this.category.children && this.category.children.length > 0;
  }

  get indentPx(): string {
    return `${this.depth * 24}px`;
  }

  onToggleExpand(event: MouseEvent): void {
    event.stopPropagation();
    this.expanded.update((v) => !v);
    this.toggleExpand.emit(this.category.id);
  }

  onSelect(): void {
    this.select.emit(this.category.id);
  }

  onChildSelect(id: string): void {
    this.select.emit(id);
  }

  onChildToggleExpand(id: string): void {
    this.toggleExpand.emit(id);
  }

  expandToReveal(): void {
    this.expanded.set(true);
  }
}
