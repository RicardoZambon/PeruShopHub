import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Plus, Pencil, Trash2, FileText } from 'lucide-angular';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { DialogComponent } from '../../shared/components/dialog/dialog.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ConfirmDialogService } from '../../shared/components';
import { ToastService } from '../../services/toast.service';
import { ResponseTemplateService, type ResponseTemplate, type ResponseTemplateDetail } from '../../services/response-template.service';

@Component({
  selector: 'app-response-templates',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, LucideAngularModule,
    PageHeaderComponent, ButtonComponent, DialogComponent,
    FormFieldComponent, FormActionsComponent, EmptyStateComponent,
  ],
  templateUrl: './response-templates.component.html',
  styleUrl: './response-templates.component.scss',
})
export class ResponseTemplatesComponent implements OnInit {
  private readonly templateService = inject(ResponseTemplateService);
  private readonly toastService = inject(ToastService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly router = inject(Router);

  readonly backIcon = ArrowLeft;
  readonly plusIcon = Plus;
  readonly pencilIcon = Pencil;
  readonly trashIcon = Trash2;
  readonly fileIcon = FileText;

  readonly loading = signal(true);
  readonly templates = signal<ResponseTemplate[]>([]);
  readonly showModal = signal(false);
  readonly saving = signal(false);
  readonly editingTemplate = signal<ResponseTemplate | null>(null);

  readonly categories = computed(() => {
    const cats = new Set(this.templates().map(t => t.category));
    return Array.from(cats).sort();
  });

  templateForm: FormGroup;

  readonly placeholderOptions = [
    { value: 'produto', label: '{produto} - Nome do produto' },
    { value: 'preco', label: '{preco} - Preço do produto' },
    { value: 'prazo', label: '{prazo} - Prazo de entrega' },
  ];

  constructor(private fb: FormBuilder) {
    this.templateForm = this.fb.group({
      name: ['', Validators.required],
      category: ['', Validators.required],
      body: ['', Validators.required],
      placeholders: [''],
    });
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  goBack(): void {
    this.router.navigate(['/configuracoes']);
  }

  loadTemplates(): void {
    this.loading.set(true);
    this.templateService.list().subscribe({
      next: (data) => {
        this.templates.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.show('Erro ao carregar templates', 'danger');
        this.loading.set(false);
      },
    });
  }

  openCreateModal(): void {
    this.editingTemplate.set(null);
    this.templateForm.reset({ name: '', category: '', body: '', placeholders: '' });
    this.showModal.set(true);
  }

  openEditModal(template: ResponseTemplate): void {
    this.editingTemplate.set(template);
    const placeholders = template.placeholders
      ? (JSON.parse(template.placeholders) as string[]).join(', ')
      : '';
    this.templateForm.patchValue({
      name: template.name,
      category: template.category,
      body: template.body,
      placeholders,
    });
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.editingTemplate.set(null);
  }

  saveTemplate(): void {
    if (this.templateForm.invalid) {
      this.templateForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const formValue = this.templateForm.value;

    const placeholdersArray = formValue.placeholders
      ? formValue.placeholders.split(',').map((p: string) => p.trim()).filter((p: string) => p)
      : [];
    const placeholders = placeholdersArray.length > 0 ? JSON.stringify(placeholdersArray) : null;

    const editing = this.editingTemplate();
    if (editing) {
      const detail = editing as unknown as ResponseTemplateDetail;
      this.templateService.update(editing.id, {
        name: formValue.name,
        category: formValue.category,
        body: formValue.body,
        placeholders,
        version: detail.version ?? 0,
      }).subscribe({
        next: (updated) => {
          this.templates.update(list =>
            list.map(t => t.id === updated.id ? updated : t)
          );
          this.toastService.show('Template atualizado', 'success');
          this.saving.set(false);
          this.closeModal();
        },
        error: () => {
          this.toastService.show('Erro ao atualizar template', 'danger');
          this.saving.set(false);
        },
      });
    } else {
      const maxOrder = this.templates().reduce((max, t) => Math.max(max, t.order), 0);
      this.templateService.create({
        name: formValue.name,
        category: formValue.category,
        body: formValue.body,
        placeholders,
        order: maxOrder + 1,
      }).subscribe({
        next: (created) => {
          this.templates.update(list => [...list, created]);
          this.toastService.show('Template criado', 'success');
          this.saving.set(false);
          this.closeModal();
        },
        error: () => {
          this.toastService.show('Erro ao criar template', 'danger');
          this.saving.set(false);
        },
      });
    }
  }

  async deleteTemplate(template: ResponseTemplate): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover template',
      message: `Deseja remover o template "${template.name}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.templateService.delete(template.id).subscribe({
      next: () => {
        this.confirmDialog.done();
        this.templates.update(list => list.filter(t => t.id !== template.id));
        this.toastService.show('Template removido', 'success');
      },
      error: () => {
        this.confirmDialog.done();
        this.toastService.show('Erro ao remover template', 'danger');
      },
    });
  }

  insertPlaceholder(placeholder: string): void {
    const bodyControl = this.templateForm.get('body');
    if (bodyControl) {
      const current = bodyControl.value || '';
      bodyControl.setValue(current + `{${placeholder}}`);
    }
  }
}
