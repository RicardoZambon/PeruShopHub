import { Component, Input, Output, EventEmitter, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, X } from 'lucide-angular';
import { IconPickerComponent, DialogComponent, FormFieldComponent, ToggleSwitchComponent, FormActionsComponent } from '../../shared/components';
import { CategoryService } from '../../services/category.service';
import type { Category, CreateCategoryDto, UpdateCategoryDto } from '../../models/category.model';

@Component({
  selector: 'app-category-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, IconPickerComponent, DialogComponent, FormFieldComponent, ToggleSwitchComponent, FormActionsComponent],
  templateUrl: './category-form-dialog.component.html',
  styleUrl: './category-form-dialog.component.scss',
})
export class CategoryFormDialogComponent implements OnInit {
  @Input() category: Category | null = null; // null = create mode
  @Input() allCategories: Category[] = [];
  @Input() preselectedParentId: string | null = null;

  @Output() saved = new EventEmitter<Category>();
  @Output() closed = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);

  readonly closeIcon = X;
  readonly saving = signal(false);
  readonly serverErrors = signal<Record<string, string>>({});

  form!: FormGroup;
  parentDropdownOpen = signal(false);
  selectedIcon = signal<string | null>(null);
  private slugManuallyEdited = false;

  get isEditMode(): boolean {
    return this.category !== null;
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Editar Categoria' : 'Nova Categoria';
  }

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
    this.selectedIcon.set(this.category?.icon || null);

    this.form = this.fb.group({
      name: [
        this.category?.name || '',
        [Validators.required, Validators.maxLength(100)],
      ],
      slug: [
        this.category?.slug || '',
        [Validators.required, Validators.maxLength(120)],
      ],
      parentId: [this.category?.parentId || this.preselectedParentId || null],
      isActive: [this.category?.isActive ?? true],
    });

    if (this.isEditMode) {
      this.slugManuallyEdited = true;
    }
  }

  onIconChange(icon: string | null): void {
    this.selectedIcon.set(icon);
  }

  onNameInput(): void {
    if (!this.slugManuallyEdited) {
      const name = this.form.get('name')!.value;
      this.form.patchValue({ slug: this.generateSlug(name) });
    }
  }

  onSlugInput(): void {
    this.slugManuallyEdited = true;
  }

  private generateSlug(name: string): string {
    return name.toLowerCase()
      .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');
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

  close(): void {
    this.closed.emit();
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.serverErrors.set({});

    try {
      const { name, slug, parentId, isActive } = this.form.value;
      const iconValue = this.selectedIcon() || null;

      if (this.isEditMode && this.category) {
        const dto: UpdateCategoryDto = { name, slug, parentId, icon: iconValue, isActive };
        const updated = await this.categoryService.update(this.category.id, dto);
        if (updated) {
          this.saved.emit(updated);
        }
      } else {
        const siblings = this.categoryService.allCategories()
          .filter(c => c.parentId === parentId);
        const dto: CreateCategoryDto = {
          name,
          slug,
          parentId,
          icon: iconValue,
          order: siblings.length + 1,
        };
        const created = await this.categoryService.create(dto);
        this.saved.emit(created);
      }
    } catch (err: any) {
      const errors = err?.error?.errors;
      if (errors) {
        const mapped: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(errors)) {
          mapped[key.toLowerCase()] = (msgs as string[])[0];
        }
        this.serverErrors.set(mapped);
      }
    } finally {
      this.saving.set(false);
    }
  }
}
