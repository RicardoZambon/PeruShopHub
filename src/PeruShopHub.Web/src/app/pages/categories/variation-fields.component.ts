import { Component, Input, inject, signal, computed, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CdkDragDrop, CdkDropList, CdkDrag, CdkDragHandle, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { LucideAngularModule, Plus, GripVertical, Trash2, Pencil, Link, Type, ListChecks, X } from 'lucide-angular';
import { CategoryService } from '../../services/category.service';
import { ProductVariantService } from '../../services/product-variant.service';
import { ToastService } from '../../services/toast.service';
import type { VariationField, InheritedVariationField, CreateVariationFieldDto } from '../../models/category.model';

@Component({
  selector: 'app-variation-fields',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideAngularModule,
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    CdkDragPlaceholder,
  ],
  templateUrl: './variation-fields.component.html',
  styleUrl: './variation-fields.component.scss',
})
export class VariationFieldsComponent implements OnChanges {
  @Input({ required: true }) categoryId!: string;

  private readonly categoryService = inject(CategoryService);
  private readonly productVariantService = inject(ProductVariantService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly plusIcon = Plus;
  readonly gripIcon = GripVertical;
  readonly trashIcon = Trash2;
  readonly pencilIcon = Pencil;
  readonly linkIcon = Link;
  readonly typeIcon = Type;
  readonly listChecksIcon = ListChecks;
  readonly closeIcon = X;

  inheritedFields = signal<InheritedVariationField[]>([]);
  ownFields = signal<VariationField[]>([]);
  showAddForm = signal(false);
  editingFieldId = signal<string | null>(null);
  addForm: FormGroup | null = null;
  editForm: FormGroup | null = null;
  chipInput = signal('');
  editChipInput = signal('');

  // Undo state
  private undoTimer: ReturnType<typeof setTimeout> | null = null;
  private undoField: VariationField | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryId'] && this.categoryId) {
      this.loadFields();
      this.showAddForm.set(false);
      this.editingFieldId.set(null);
    }
  }

  private loadFields(): void {
    this.inheritedFields.set(
      this.categoryService.getInheritedVariationFields(this.categoryId)
    );
    this.ownFields.set(
      this.categoryService.getVariationFields(this.categoryId)
    );
  }

  // ── Add field ──

  openAddForm(): void {
    this.addForm = this.fb.group({
      name: ['', [Validators.required]],
      type: ['select' as 'text' | 'select'],
      required: [false],
    });
    this.chipInput.set('');
    this.showAddForm.set(true);
    this.editingFieldId.set(null);
  }

  cancelAdd(): void {
    this.showAddForm.set(false);
    this.addForm = null;
  }

  get addFormOptions(): string[] {
    // Track options for the add form separately
    return this._addFormOptions;
  }
  private _addFormOptions: string[] = [];

  onAddChipKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      const value = this.chipInput().trim();
      if (value && !this._addFormOptions.includes(value)) {
        this._addFormOptions = [...this._addFormOptions, value];
      }
      this.chipInput.set('');
    }
  }

  removeAddOption(index: number): void {
    this._addFormOptions = this._addFormOptions.filter((_, i) => i !== index);
  }

  async saveAdd(): Promise<void> {
    if (!this.addForm || this.addForm.invalid) return;

    const { name, type, required } = this.addForm.value;

    if (type === 'select' && this._addFormOptions.length < 2) {
      this.toast.show('Adicione pelo menos 2 opções para campos de seleção', 'warning');
      return;
    }

    const dto: CreateVariationFieldDto = {
      name,
      type,
      options: type === 'select' ? [...this._addFormOptions] : [],
      required,
    };

    await this.categoryService.addVariationField(this.categoryId, dto);
    this.toast.show('Campo de variação adicionado', 'success');
    this.cancelAdd();
    this._addFormOptions = [];
    this.loadFields();
  }

  // ── Edit field ──

  startEdit(field: VariationField): void {
    this.editForm = this.fb.group({
      name: [field.name, [Validators.required]],
      type: [field.type],
      required: [field.required],
    });
    this._editFormOptions = [...field.options];
    this.editChipInput.set('');
    this.editingFieldId.set(field.id);
    this.showAddForm.set(false);
  }

  cancelEdit(): void {
    this.editingFieldId.set(null);
    this.editForm = null;
  }

  private _editFormOptions: string[] = [];

  get editFormOptions(): string[] {
    return this._editFormOptions;
  }

  onEditChipKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      const value = this.editChipInput().trim();
      if (value && !this._editFormOptions.includes(value)) {
        this._editFormOptions = [...this._editFormOptions, value];
      }
      this.editChipInput.set('');
    }
  }

  removeEditOption(index: number): void {
    this._editFormOptions = this._editFormOptions.filter((_, i) => i !== index);
  }

  async saveEdit(): Promise<void> {
    if (!this.editForm || this.editForm.invalid || !this.editingFieldId()) return;

    const { name, type, required } = this.editForm.value;

    if (type === 'select' && this._editFormOptions.length < 2) {
      this.toast.show('Adicione pelo menos 2 opções para campos de seleção', 'warning');
      return;
    }

    // Show affected count
    const affectedCount = this.productVariantService.getAffectedProductCount(this.categoryId);

    await this.categoryService.updateVariationField(this.editingFieldId()!, {
      name,
      type,
      options: type === 'select' ? [...this._editFormOptions] : [],
      required,
    });

    if (affectedCount > 0) {
      await this.productVariantService.flagForReview(this.categoryId);
      this.toast.show(
        `Campo atualizado. ${affectedCount} produtos marcados para revisão`,
        'warning'
      );
    } else {
      this.toast.show('Campo de variação atualizado', 'success');
    }

    this.cancelEdit();
    this.loadFields();
  }

  // ── Delete field ──

  async deleteField(field: VariationField): Promise<void> {
    const affectedCount = this.productVariantService.getAffectedProductCount(this.categoryId);

    const deleted = await this.categoryService.deleteVariationField(field.id);
    if (!deleted) return;

    if (affectedCount > 0) {
      await this.productVariantService.flagForReview(this.categoryId);
    }

    this.loadFields();

    // Store for undo
    this.undoField = deleted;

    // Clear any previous undo timer
    if (this.undoTimer) {
      clearTimeout(this.undoTimer);
    }

    const message = affectedCount > 0
      ? `Campo removido. ${affectedCount} produtos marcados para revisão`
      : 'Campo de variação removido';

    this.toast.show(message, 'info', 5000, 'Clique para desfazer');

    // Auto-clear undo after 5s
    this.undoTimer = setTimeout(() => {
      this.undoField = null;
      this.undoTimer = null;
    }, 5000);
  }

  undoDelete(): void {
    if (!this.undoField) return;

    this.categoryService.restoreVariationField(this.undoField);

    // If products were flagged, we'd clear those flags here
    // For now, the undo just restores the field

    this.undoField = null;
    if (this.undoTimer) {
      clearTimeout(this.undoTimer);
      this.undoTimer = null;
    }

    this.toast.show('Campo de variação restaurado', 'success');
    this.loadFields();
  }

  // ── Reorder ──

  onFieldDrop(event: CdkDragDrop<VariationField[]>): void {
    if (event.previousIndex !== event.currentIndex) {
      const fields = [...this.ownFields()];
      const [moved] = fields.splice(event.previousIndex, 1);
      fields.splice(event.currentIndex, 0, moved);

      // Update order on each field
      fields.forEach((f, i) => {
        this.categoryService.updateVariationField(f.id, { order: i });
      });

      this.loadFields();
    }
  }

  // ── Helpers ──

  isInherited(field: VariationField | InheritedVariationField): field is InheritedVariationField {
    return 'inheritedFrom' in field;
  }
}
