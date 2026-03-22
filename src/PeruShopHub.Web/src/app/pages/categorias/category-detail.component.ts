import { Component, Input, Output, EventEmitter, signal, computed, inject, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, Pencil, Trash2, ArrowLeft } from 'lucide-angular';
import { CategoryService } from '../../services/category.service';
import { ToastService } from '../../services/toast.service';
import type { Category, UpdateCategoryDto } from '../../models/category.model';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './category-detail.component.html',
  styleUrl: './category-detail.component.scss',
})
export class CategoryDetailComponent implements OnChanges {
  @Input() category: Category | null = null;
  @Input() allCategories: Category[] = [];

  @Output() deleted = new EventEmitter<string>();
  @Output() updated = new EventEmitter<Category>();
  @Output() back = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  readonly pencilIcon = Pencil;
  readonly trashIcon = Trash2;
  readonly arrowLeftIcon = ArrowLeft;

  form: FormGroup | null = null;
  editing = signal(false);
  saving = signal(false);
  confirmingDelete = signal(false);

  readonly breadcrumb = computed(() => {
    if (!this.category) return [];
    return this.categoryService.getBreadcrumb(this.category.id);
  });

  readonly hasChildren = computed(() => {
    if (!this.category) return false;
    return this.categoryService.hasChildren(this.category.id);
  });

  readonly childCount = computed(() => {
    if (!this.category) return 0;
    return this.categoryService.getChildCount(this.category.id);
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['category'] && this.category) {
      this.editing.set(false);
      this.confirmingDelete.set(false);
      this.initForm();
    }
  }

  private initForm(): void {
    if (!this.category) return;
    this.form = this.fb.group({
      name: [this.category.name, [Validators.required, Validators.maxLength(100)]],
      isActive: [this.category.isActive],
    });
  }

  startEditing(): void {
    this.editing.set(true);
    this.initForm();
  }

  cancelEditing(): void {
    this.editing.set(false);
    this.initForm();
  }

  async saveEditing(): Promise<void> {
    if (!this.form || this.form.invalid || !this.category) return;

    this.saving.set(true);
    try {
      const dto: UpdateCategoryDto = {
        name: this.form.value.name,
        isActive: this.form.value.isActive,
      };
      const updated = await this.categoryService.update(this.category.id, dto);
      if (updated) {
        this.editing.set(false);
        this.updated.emit(updated);
        this.toast.show('Categoria atualizada com sucesso', 'success');
      }
    } catch {
      this.toast.show('Erro ao atualizar categoria', 'danger');
    } finally {
      this.saving.set(false);
    }
  }

  onDeleteClick(): void {
    if (this.hasChildren()) {
      return;
    }
    this.confirmingDelete.set(true);
  }

  cancelDelete(): void {
    this.confirmingDelete.set(false);
  }

  async confirmDelete(): Promise<void> {
    if (!this.category) return;

    try {
      const success = await this.categoryService.delete(this.category.id);
      if (success) {
        this.deleted.emit(this.category.id);
        this.toast.show('Categoria excluída com sucesso', 'success');
      } else {
        this.toast.show('Não foi possível excluir a categoria', 'danger');
      }
    } catch {
      this.toast.show('Erro ao excluir categoria', 'danger');
    }
    this.confirmingDelete.set(false);
  }

  onBack(): void {
    this.back.emit();
  }

  formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}
