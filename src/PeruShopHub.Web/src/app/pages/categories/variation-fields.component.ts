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
  loading = signal(true);
  saving = signal(false);
  showAddForm = signal(false);
  editingFieldId = signal<string | null>(null);
  addForm: FormGroup | null = null;
  editForm: FormGroup | null = null;
  chipInput = signal('');
  serverErrors = signal<Record<string, string>>({});
  editChipInput = signal('');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryId'] && this.categoryId) {
      this.loadFields();
      this.showAddForm.set(false);
      this.editingFieldId.set(null);
    }
  }

  private async loadFields(): Promise<void> {
    this.loading.set(true);
    this.ownFields.set([]);
    this.inheritedFields.set([]);
    try {
      const [own, inherited] = await Promise.all([
        this.categoryService.getVariationFields(this.categoryId),
        this.categoryService.getInheritedVariationFields(this.categoryId),
      ]);
      this.ownFields.set(own);
      this.inheritedFields.set(inherited);
    } catch {
      // Silently fail — empty lists shown
    } finally {
      this.loading.set(false);
    }
  }

  // ── Add field ──

  openAddForm(): void {
    this.serverErrors.set({});
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
    const trimmedName = name.trim();

    const duplicate = this.ownFields().some(f => f.name.toLowerCase() === trimmedName.toLowerCase())
      || this.inheritedFields().some(f => f.name.toLowerCase() === trimmedName.toLowerCase());
    if (duplicate) {
      this.toast.show(`Já existe um campo com o nome "${trimmedName}"`, 'warning');
      return;
    }

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

    this.saving.set(true);
    try {
      this.serverErrors.set({});
      const created = await this.categoryService.addVariationField(this.categoryId, dto);
      this.ownFields.update(fields => [...fields, created]);
      this.toast.show('Campo de variação adicionado', 'success');
      this.cancelAdd();
      this._addFormOptions = [];
    } catch (err: any) {
      const errors = err?.error?.errors;
      if (errors) {
        const mapped: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(errors)) {
          mapped[key.toLowerCase()] = (msgs as string[])[0];
        }
        this.serverErrors.set(mapped);
      } else {
        this.toast.show('Erro ao adicionar campo', 'danger');
      }
    } finally {
      this.saving.set(false);
    }
  }

  // ── Edit field ──

  startEdit(field: VariationField): void {
    this.serverErrors.set({});
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
    const trimmedName = name.trim();
    const fieldId = this.editingFieldId()!;

    const duplicate = this.ownFields().some(f => f.id !== fieldId && f.name.toLowerCase() === trimmedName.toLowerCase())
      || this.inheritedFields().some(f => f.name.toLowerCase() === trimmedName.toLowerCase());
    if (duplicate) {
      this.toast.show(`Já existe um campo com o nome "${trimmedName}"`, 'warning');
      return;
    }

    if (type === 'select' && this._editFormOptions.length < 2) {
      this.toast.show('Adicione pelo menos 2 opções para campos de seleção', 'warning');
      return;
    }

    this.saving.set(true);
    try {
      this.serverErrors.set({});
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
    } catch (err: any) {
      const errors = err?.error?.errors;
      if (errors) {
        const mapped: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(errors)) {
          mapped[key.toLowerCase()] = (msgs as string[])[0];
        }
        this.serverErrors.set(mapped);
      } else {
        this.toast.show('Erro ao atualizar campo', 'danger');
      }
    } finally {
      this.saving.set(false);
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
      this.confirm.done();
      if (success) {
        this.ownFields.update(fields => fields.filter(f => f.id !== field.id));
        this.toast.show('Campo de variação removido', 'success');
      }
    } catch {
      this.confirm.done();
      this.toast.show('Erro ao remover campo', 'danger');
    }
  }

  // ── Reorder ──

  async onFieldDrop(event: CdkDragDrop<VariationField[]>): Promise<void> {
    if (event.previousIndex !== event.currentIndex) {
      const fields = [...this.ownFields()];
      const [moved] = fields.splice(event.previousIndex, 1);
      fields.splice(event.currentIndex, 0, moved);

      // Update local state immediately
      this.ownFields.set(fields.map((f, i) => ({ ...f, order: i })));

      // Persist order to backend in background (no reload needed)
      for (let i = 0; i < fields.length; i++) {
        this.categoryService.updateVariationField(
          this.categoryId, fields[i].id, { order: i }
        ).catch(() => {}); // silently fail — order is cosmetic
      }
    }
  }
}
