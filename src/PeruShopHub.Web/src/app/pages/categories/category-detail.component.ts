import { Component, Input, Output, EventEmitter, signal, computed, inject, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, Pencil, Trash2, ArrowLeft } from 'lucide-angular';
import { IconPickerComponent, ButtonComponent, FormFieldComponent, ToggleSwitchComponent, FormActionsComponent, ConfirmDialogService } from '../../shared/components';
import { CategoryService } from '../../services/category.service';
import { ToastService } from '../../services/toast.service';
import { VariationFieldsComponent } from './variation-fields.component';
import type { Category, UpdateCategoryDto } from '../../models/category.model';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, IconPickerComponent, ButtonComponent, FormFieldComponent, ToggleSwitchComponent, FormActionsComponent, VariationFieldsComponent],
  templateUrl: './category-detail.component.html',
  styleUrl: './category-detail.component.scss',
})
export class CategoryDetailComponent implements OnChanges {
  @Input() category: Category | null = null;
  @Input() allCategories: Category[] = [];
  @Input() loading = false;

  @Output() deleted = new EventEmitter<string>();
  @Output() updated = new EventEmitter<Category>();
  @Output() back = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly pencilIcon = Pencil;
  readonly trashIcon = Trash2;
  readonly arrowLeftIcon = ArrowLeft;

  form: FormGroup | null = null;
  editing = signal(false);
  saving = signal(false);
  editIcon = signal<string | null>(null);

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
      this.initForm();
    }
  }

  private initForm(): void {
    if (!this.category) return;
    this.editIcon.set(this.category.icon || null);
    this.form = this.fb.group({
      name: [this.category.name, [Validators.required, Validators.maxLength(100)]],
      slug: [this.category.slug, [Validators.required, Validators.maxLength(120)]],
      isActive: [this.category.isActive],
    });
  }

  onEditIconChange(icon: string | null): void {
    this.editIcon.set(icon);
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
        slug: this.form.value.slug,
        icon: this.editIcon(),
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

  async onDeleteClick(): Promise<void> {
    if (this.hasChildren() || !this.category) return;

    const message = this.category.productCount > 0
      ? `Esta categoria possui ${this.category.productCount} produtos. Deseja realmente excluir?`
      : `Deseja realmente excluir a categoria "${this.category.name}"?`;

    const confirmed = await this.confirmDialog.confirm({
      title: 'Excluir categoria',
      message,
      confirmLabel: 'Excluir',
      variant: 'danger',
    });
    if (!confirmed) return;

    try {
      const success = await this.categoryService.delete(this.category.id);
      this.confirmDialog.done();
      if (success) {
        this.deleted.emit(this.category.id);
        this.toast.show('Categoria excluída com sucesso', 'success');
      } else {
        this.toast.show('Não foi possível excluir a categoria', 'danger');
      }
    } catch {
      this.confirmDialog.done();
      this.toast.show('Erro ao excluir categoria', 'danger');
    }
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
