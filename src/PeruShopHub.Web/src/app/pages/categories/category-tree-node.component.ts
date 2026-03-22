import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, CdkDrag, CdkDropList, CdkDragHandle, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { LucideAngularModule, ChevronRight, ChevronDown, GripVertical } from 'lucide-angular';
import type { Category } from '../../models/category.model';

@Component({
  selector: 'app-category-tree-node',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, CdkDrag, CdkDropList, CdkDragHandle, CdkDragPlaceholder],
  templateUrl: './category-tree-node.component.html',
  styleUrl: './category-tree-node.component.scss',
})
export class CategoryTreeNodeComponent {
  @Input({ required: true }) category!: Category;
  @Input() selectedId: string | null = null;
  @Input() depth = 0;
  @Input() dragEnabled = true;

  @Output() select = new EventEmitter<string>();
  @Output() toggleExpand = new EventEmitter<string>();
  @Output() reorder = new EventEmitter<{ categoryId: string; newParentId: string | null; newIndex: number }>();

  readonly chevronRightIcon = ChevronRight;
  readonly chevronDownIcon = ChevronDown;
  readonly gripIcon = GripVertical;

  expanded = signal(false);

  ngOnInit(): void {
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

  get dropListId(): string {
    return `drop-list-${this.category.id}`;
  }

  get childIds(): string[] {
    return this.category.children.map((c) => c.id);
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

  onChildReorder(event: { categoryId: string; newParentId: string | null; newIndex: number }): void {
    this.reorder.emit(event);
  }

  onDrop(event: CdkDragDrop<Category[]>): void {
    if (event.previousIndex !== event.currentIndex) {
      this.reorder.emit({
        categoryId: this.category.children[event.previousIndex].id,
        newParentId: this.category.id,
        newIndex: event.currentIndex,
      });
    }
  }

  expandToReveal(): void {
    this.expanded.set(true);
  }
}
