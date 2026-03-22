import { Component, Input, Output, EventEmitter, signal, computed, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, X } from 'lucide-angular';
import { CategoryService } from '../../services/category.service';
import type { Category, CreateCategoryDto, UpdateCategoryDto } from '../../models/category.model';

@Component({
  selector: 'app-category-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './category-form-dialog.component.html',
  styleUrl: './category-form-dialog.component.scss',
})
export class CategoryFormDialogComponent {
  @Input() category: Category | null = null; // null = create mode
  @Input() allCategories: Category[] = [];
  @Input() preselectedParentId: string | null = null;

  @Output() saved = new EventEmitter<Category>();
  @Output() closed = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);

  readonly closeIcon = X;
  readonly saving = signal(false);

  form!: FormGroup;
  parentDropdownOpen = signal(false);

  get isEditMode(): boolean {
    return this.category !== null;
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Editar Categoria' : 'Nova Categoria';
  }

  // Flatten the tree for parent selection, excluding self and descendants
  readonly flatParentOptions = computed(() => {
    const result: { id: string; name: string; depth: number }[] = [];
    const excludeIds = new Set<string>();

    if (this.category) {
      excludeIds.add(this.category.id);
      const descendants = this.categoryService.getDescendantIds(this.category.id);
      for (const id of descendants) {
        excludeIds.add(id);
      }
    }

    const flatten = (categories: Category[], depth: number): void => {
      for (const cat of categories) {
        if (!excludeIds.has(cat.id)) {
          result.push({ id: cat.id, name: cat.name, depth });
          flatten(cat.children, depth + 1);
        }
      }
    };

    flatten(this.allCategories, 0);
    return result;
  });

  readonly selectedParentName = computed(() => {
    const parentId = this.form?.get('parentId')?.value;
    if (!parentId) return 'Nenhuma (raiz)';
    const found = this.flatParentOptions().find((o) => o.id === parentId);
    return found ? found.name : 'Nenhuma (raiz)';
  });

  ngOnInit(): void {
    this.form = this.fb.group({
      name: [
        this.category?.name || '',
        [Validators.required, Validators.maxLength(100)],
      ],
      parentId: [this.category?.parentId || this.preselectedParentId || null],
      isActive: [this.category?.isActive ?? true],
    });
  }

  getIndent(depth: number): string {
    return '\u2014 '.repeat(depth);
  }

  toggleParentDropdown(): void {
    this.parentDropdownOpen.update((v) => !v);
  }

  selectParent(parentId: string | null): void {
    this.form.patchValue({ parentId });
    this.parentDropdownOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('dialog-overlay')) {
      this.close();
    }
  }

  close(): void {
    this.closed.emit();
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    try {
      const { name, parentId, isActive } = this.form.value;

      if (this.isEditMode && this.category) {
        const dto: UpdateCategoryDto = { name, parentId, isActive };
        const updated = await this.categoryService.update(this.category.id, dto);
        if (updated) {
          this.saved.emit(updated);
        }
      } else {
        const dto: CreateCategoryDto = { name, parentId, isActive };
        const created = await this.categoryService.create(dto);
        this.saved.emit(created);
      }
    } finally {
      this.saving.set(false);
    }
  }
}
