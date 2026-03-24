import { Component, Input, inject, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CdkDragDrop, CdkDropList, CdkDrag, CdkDragHandle, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { LucideAngularModule, Plus, GripVertical, Trash2, Pencil, Link, Type, ListChecks, X } from 'lucide-angular';
import { ButtonComponent, FormFieldComponent, RadioGroupComponent, ToggleSwitchComponent, FormActionsComponent, ConfirmDialogService } from '../../shared/components';
import { CategoryService } from '../../services/category.service';
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
    ButtonComponent,
    FormFieldComponent,
    RadioGroupComponent,
    ToggleSwitchComponent,
    FormActionsComponent,
  ],
  templateUrl: './variation-fields.component.html',
  styleUrl: './variation-fields.component.scss',
})
export class VariationFieldsComponent implements OnChanges {
  @Input({ required: true }) categoryId!: string;

  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly fb = inject(FormBuilder);

  readonly plusIcon = Plus;
  readonly gripIcon = GripVertical;
  readonly trashIcon = Trash2;
  readonly pencilIcon = Pencil;
  readonly linkIcon = Link;
  readonly typeIcon = Type;
  readonly listChecksIcon = ListChecks;
  readonly closeIcon = X;

  readonly typeOptions = [
    { value: 'text', label: 'Texto livre' },
    { value: 'select', label: 'Opções predefinidas' },
  ];

  inheritedFields = signal<InheritedVariationField[]>([]);
  ownFields = signal<VariationField[]>([]);
  showAddForm = signal(false);
  editingFieldId = signal<string | null>(null);
  addForm: FormGroup | null = null;
  editForm: FormGroup | null = null;
  chipInput = signal('');
  editChipInput = signal('');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryId'] && this.categoryId) {
      this.loadFields();
      this.showAddForm.set(false);
      this.editingFieldId.set(null);
    }
  }

  private async loadFields(): Promise<void> {
    try {
      const [own, inherited] = await Promise.all([
        this.categoryService.getVariationFields(this.categoryId),
        this.categoryService.getInheritedVariationFields(this.categoryId),
      ]);
      this.ownFields.set(own);
      this.inheritedFields.set(inherited);
    } catch {
      // Silently fail — empty lists shown
    }
  }

  // ── Add field ──

  openAddForm(): void {
    this.addForm = this.fb.group({
      name: ['', [Validators.required]],
      type: ['text' as 'text' | 'select'],
      required: [false],
    });
    this.chipInput.set('');
    this._addFormOptions = [];
    this.showAddForm.set(true);
    this.editingFieldId.set(null);
  }

  cancelAdd(): void {
    this.showAddForm.set(false);
    this.addForm = null;
  }

  private _addFormOptions: string[] = [];
  get addFormOptions(): string[] {
    return this._addFormOptions;
  }

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
    if (!this.addForm) return;
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

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

    try {
      const created = await this.categoryService.addVariationField(this.categoryId, dto);
      this.ownFields.update(fields => [...fields, created]);
      this.toast.show('Campo de variação adicionado', 'success');
      this.cancelAdd();
      this._addFormOptions = [];
    } catch {
      this.toast.show('Erro ao adicionar campo', 'danger');
    }
  }

  // ── Edit field ──

  startEdit(field: VariationField): void {
    this.editForm = this.fb.group({
      name: [field.name, [Validators.required]],
      type: [field.type],
      required: [field.required],
    });
    this._editFormOptions = [...(field.options || [])];
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
    if (!this.editForm || !this.editingFieldId()) return;
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const { name, type, required } = this.editForm.value;

    if (type === 'select' && this._editFormOptions.length < 2) {
      this.toast.show('Adicione pelo menos 2 opções para campos de seleção', 'warning');
      return;
    }

    try {
      const fieldId = this.editingFieldId()!;
      const updated = await this.categoryService.updateVariationField(this.categoryId, fieldId, {
        name,
        type,
        options: type === 'select' ? [...this._editFormOptions] : [],
        required,
      });
      this.ownFields.update(fields =>
        fields.map(f => f.id === fieldId ? updated : f)
      );
      this.toast.show('Campo de variação atualizado', 'success');
      this.cancelEdit();
    } catch {
      this.toast.show('Erro ao atualizar campo', 'danger');
    }
  }

  // ── Delete field ──

  async deleteField(field: VariationField): Promise<void> {
    const confirmed = await this.confirm.confirm({
      title: 'Remover campo',
      message: `Deseja remover o campo "${field.name}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    try {
      const success = await this.categoryService.deleteVariationField(this.categoryId, field.id);
      if (success) {
        this.ownFields.update(fields => fields.filter(f => f.id !== field.id));
        this.toast.show('Campo de variação removido', 'success');
      }
    } catch {
      this.toast.show('Erro ao remover campo', 'danger');
    }
  }

  // ── Reorder ──

  async onFieldDrop(event: CdkDragDrop<VariationField[]>): Promise<void> {
    if (event.previousIndex !== event.currentIndex) {
      const fields = [...this.ownFields()];
      const [moved] = fields.splice(event.previousIndex, 1);
      fields.splice(event.currentIndex, 0, moved);

      // Update order on each field
      for (let i = 0; i < fields.length; i++) {
        await this.categoryService.updateVariationField(
          this.categoryId, fields[i].id, { order: i }
        );
      }

      await this.loadFields();
    }
  }
}
