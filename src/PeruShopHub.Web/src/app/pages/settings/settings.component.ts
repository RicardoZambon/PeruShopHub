import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { LucideAngularModule, Pencil, Trash2 } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import type { TabItem } from '../../shared/components/tab-bar/tab-bar.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { DialogComponent } from '../../shared/components/dialog/dialog.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import { ToggleSwitchComponent } from '../../shared/components/toggle-switch/toggle-switch.component';
import { ThemeService } from '../../services/theme.service';
import type { ThemePreference } from '../../services/theme.service';
import { SettingsService, type UserRow, type Integration, type FixedCostsResponse, type CommissionRule, type TaxProfile, type PaymentFeeRule, type ReportSchedule } from '../../services/settings.service';
import { TenantService, type TenantMember } from '../../services/tenant.service';
import { ToastService } from '../../services/toast.service';
import { ConfirmDialogService } from '../../shared/components';

type SettingsTab = 'empresa' | 'usuarios' | 'integracoes' | 'custos-fixos' | 'fiscal' | 'alertas' | 'relatorios' | 'aparencia';

interface FixedCost {
  id: number;
  nome: string;
  valor: number;
}

interface AlertConfig {
  id: string;
  label: string;
  description: string;
  enabled: boolean;
  threshold: number;
  unit: string;
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, BadgeComponent, PageHeaderComponent, ButtonComponent, DialogComponent, FormFieldComponent, FormActionsComponent, ToggleSwitchComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  private readonly themeService = inject(ThemeService);
  private readonly settingsService = inject(SettingsService);
  private readonly tenantService = inject(TenantService);
  private readonly toastService = inject(ToastService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly pencilIcon = Pencil;
  readonly trashIcon = Trash2;

  activeTab = signal<SettingsTab>('empresa');
  readonly saving = signal(false);
  showUserModal = signal(false);
  editingUser = signal<UserRow | null>(null);

  tabs: TabItem[] = [
    { key: 'empresa', label: 'Empresa' },
    { key: 'usuarios', label: 'Usuários' },
    { key: 'integracoes', label: 'Integrações' },
    { key: 'custos-fixos', label: 'Custos Fixos' },
    { key: 'fiscal', label: 'Fiscal' },
    { key: 'alertas', label: 'Alertas' },
    { key: 'relatorios', label: 'Relatórios' },
    { key: 'aparencia', label: 'Aparência' },
  ];

  users = signal<UserRow[]>([]);
  integrations = signal<Integration[]>([]);

  // Fixed costs
  embalagemPadrao = signal(0);
  aliquotaSimples = signal(0);
  fixedCosts = signal<FixedCost[]>([]);

  // Commission rules
  commissionRules = signal<CommissionRule[]>([]);
  showCommissionRuleModal = signal(false);
  editingCommissionRule = signal<CommissionRule | null>(null);
  commissionRuleForm!: FormGroup;

  // Payment fee rules
  paymentFeeRules = signal<PaymentFeeRule[]>([]);
  showPaymentFeeRuleModal = signal(false);
  editingPaymentFeeRule = signal<PaymentFeeRule | null>(null);
  paymentFeeRuleForm!: FormGroup;

  // Tax rate
  taxRate = signal(0);
  taxRateForm!: FormGroup;

  // Tax profile (Fiscal tab)
  taxProfile = signal<TaxProfile | null>(null);
  taxProfileForm!: FormGroup;
  savingTaxProfile = signal(false);

  // Alerts
  alerts = signal<AlertConfig[]>([
    { id: 'margem', label: 'Margem mínima', description: 'Alerta quando a margem de um produto ficar abaixo do limite', enabled: true, threshold: 10, unit: '%' },
    { id: 'estoque', label: 'Estoque mínimo', description: 'Alerta quando o estoque ficar abaixo do limite', enabled: true, threshold: 5, unit: 'unidades' },
    { id: 'pergunta', label: 'Pergunta sem resposta', description: 'Alerta quando uma pergunta ficar sem resposta por mais de', enabled: false, threshold: 24, unit: 'horas' },
    { id: 'divergencia', label: 'Divergência financeira', description: 'Alerta quando a divergência entre esperado e depositado exceder', enabled: false, threshold: 5, unit: '%' },
  ]);

  // Report schedules
  reportSchedules = signal<ReportSchedule[]>([]);
  showReportScheduleModal = signal(false);
  editingReportSchedule = signal<ReportSchedule | null>(null);
  reportScheduleForm!: FormGroup;
  savingReportSchedule = signal(false);

  // Theme
  currentTheme = this.themeService.currentTheme;

  companyForm: FormGroup;
  userForm: FormGroup;
  fixedCostsForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.companyForm = this.fb.group({
      nome: ['PeruShopHub Comercio LTDA', Validators.required],
      cnpj: ['12.345.678/0001-90', [Validators.required, Validators.pattern(/^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$/)]],
      endereco: ['Rua das Flores, 123 - Centro, São Paulo - SP, 01001-000'],
    });

    this.userForm = this.fb.group({
      nome: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: [''],
      role: ['Viewer', Validators.required],
      ativo: [true],
    });

    this.fixedCostsForm = this.fb.group({
      embalagemPadrao: [0, [Validators.required, Validators.min(0)]],
      aliquotaSimples: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    });

    this.commissionRuleForm = this.fb.group({
      marketplace: ['', Validators.required],
      categoryPattern: ['', Validators.required],
      listingType: ['', Validators.required],
      rate: [null as number | null, [Validators.required, Validators.min(0), Validators.max(100)]],
    });

    this.paymentFeeRuleForm = this.fb.group({
      installmentMin: [1, [Validators.required, Validators.min(1), Validators.max(24)]],
      installmentMax: [1, [Validators.required, Validators.min(1), Validators.max(24)]],
      feePercentage: [null as number | null, [Validators.required, Validators.min(0), Validators.max(100)]],
    });

    this.taxRateForm = this.fb.group({
      taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    });

    this.taxProfileForm = this.fb.group({
      taxRegime: ['SimplesNacional', Validators.required],
      aliquotPercentage: [6.0, [Validators.required, Validators.min(0), Validators.max(100)]],
      state: [''],
    });

    this.reportScheduleForm = this.fb.group({
      frequency: ['weekly', Validators.required],
      recipients: ['', [Validators.required]],
      isActive: [true],
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadIntegrations();
    this.loadCosts();
    this.loadCommissionRules();
    this.loadPaymentFeeRules();
    this.loadTaxProfile();
    this.loadReportSchedules();
  }

  selectTab(tab: SettingsTab): void {
    const tabDef = this.tabs.find(t => t.key === tab);
    if (!tabDef?.disabled) {
      this.activeTab.set(tab);
    }
  }

  // Company form
  saveCompany(): void {
    if (this.companyForm.valid) {
      this.saving.set(true);
      // Mock save - would call API
      alert('Dados da empresa salvos com sucesso!');
      this.saving.set(false);
    }
  }

  applyCnpjMask(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\D/g, '');
    if (value.length > 14) value = value.substring(0, 14);

    if (value.length > 12) {
      value = value.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{1,2})/, '$1.$2.$3/$4-$5');
    } else if (value.length > 8) {
      value = value.replace(/^(\d{2})(\d{3})(\d{3})(\d{1,4})/, '$1.$2.$3/$4');
    } else if (value.length > 5) {
      value = value.replace(/^(\d{2})(\d{3})(\d{1,3})/, '$1.$2.$3');
    } else if (value.length > 2) {
      value = value.replace(/^(\d{2})(\d{1,3})/, '$1.$2');
    }

    this.companyForm.patchValue({ cnpj: value }, { emitEvent: false });
    input.value = value;
  }

  // Users
  roleBadgeVariant(role: string): BadgeVariant {
    switch (role) {
      case 'Admin': return 'primary';
      case 'Manager': return 'accent';
      default: return 'neutral';
    }
  }

  statusBadgeVariant(ativo: boolean): BadgeVariant {
    return ativo ? 'success' : 'danger';
  }

  openNewUserModal(): void {
    this.editingUser.set(null);
    this.userForm.reset({ nome: '', email: '', password: '', role: 'Viewer', ativo: true });
    this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.userForm.get('password')?.updateValueAndValidity();
    this.showUserModal.set(true);
  }

  openEditUserModal(user: UserRow): void {
    this.editingUser.set(user);
    this.userForm.get('password')?.clearValidators();
    this.userForm.get('password')?.updateValueAndValidity();
    this.userForm.patchValue({
      nome: user.nome,
      email: user.email,
      password: '',
      role: user.role,
      ativo: user.ativo,
    });
    this.showUserModal.set(true);
  }

  closeUserModal(): void {
    this.showUserModal.set(false);
    this.editingUser.set(null);
  }

  saveUser(): void {
    if (!this.userForm.valid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const formValue = this.userForm.value;
    const editing = this.editingUser();

    if (editing) {
      const updateData = { name: formValue.nome, email: formValue.email, role: formValue.role };
      this.tenantService.updateMember(String(editing.id), updateData).subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.users.update(users =>
            users.map(u => u.id === editing.id ? {
              id: parseInt(updated.id, 10) || editing.id,
              nome: updated.name,
              email: updated.email,
              role: updated.role,
              ativo: updated.isActive,
            } : u)
          );
          this.closeUserModal();
          this.toastService.show('Usuário atualizado', 'success');
        },
        error: () => {
          this.saving.set(false);
          this.toastService.show('Erro ao atualizar usuário', 'danger');
        },
      });
    } else {
      const inviteData = { name: formValue.nome, email: formValue.email, password: formValue.password, role: formValue.role };
      this.tenantService.inviteMember(inviteData).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.users.update(users => [...users, {
            id: parseInt(created.id, 10) || 0,
            nome: created.name,
            email: created.email,
            role: created.role,
            ativo: created.isActive,
          }]);
          this.closeUserModal();
          this.toastService.show('Usuário convidado', 'success');
        },
        error: () => {
          this.saving.set(false);
          this.toastService.show('Erro ao convidar usuário', 'danger');
        },
      });
    }
  }

  async deleteUser(user: UserRow): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover usuário',
      message: `Deseja remover o usuário "${user.nome}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.tenantService.removeMember(String(user.id)).subscribe({
      next: () => {
        this.confirmDialog.done();
        this.users.update(users => users.filter(u => u.id !== user.id));
        this.toastService.show('Usuário removido', 'success');
      },
      error: () => {
        this.confirmDialog.done();
        this.toastService.show('Erro ao remover usuário', 'danger');
      },
    });
  }

  onModalBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.closeUserModal();
    }
  }

  onModalKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.closeUserModal();
    }
  }

  // Fixed Costs
  saveFixedCosts(): void {
    if (this.fixedCostsForm.valid) {
      this.saving.set(true);
      this.embalagemPadrao.set(this.fixedCostsForm.value.embalagemPadrao);
      this.aliquotaSimples.set(this.fixedCostsForm.value.aliquotaSimples);
      alert('Custos fixos salvos com sucesso!');
      this.saving.set(false);
    }
  }

  addFixedCost(): void {
    const costs = this.fixedCosts();
    const newId = costs.length > 0 ? Math.max(...costs.map(c => c.id)) + 1 : 1;
    this.fixedCosts.update(list => [...list, { id: newId, nome: '', valor: 0 }]);
  }

  updateFixedCostName(id: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.fixedCosts.update(list => list.map(c => c.id === id ? { ...c, nome: value } : c));
  }

  updateFixedCostValue(id: number, event: Event): void {
    const value = parseFloat((event.target as HTMLInputElement).value) || 0;
    this.fixedCosts.update(list => list.map(c => c.id === id ? { ...c, valor: value } : c));
  }

  async removeFixedCost(id: number): Promise<void> {
    const cost = this.fixedCosts().find(c => c.id === id);
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover custo fixo',
      message: `Deseja remover o custo fixo "${cost?.nome || ''}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;
    this.fixedCosts.update(list => list.filter(c => c.id !== id));
    this.confirmDialog.done();
  }

  // Alerts
  toggleAlert(id: string): void {
    this.alerts.update(list =>
      list.map(a => a.id === id ? { ...a, enabled: !a.enabled } : a)
    );
  }

  updateAlertThreshold(id: string, event: Event): void {
    const value = parseFloat((event.target as HTMLInputElement).value) || 0;
    this.alerts.update(list =>
      list.map(a => a.id === id ? { ...a, threshold: value } : a)
    );
  }

  // Commission Rules
  openNewCommissionRuleModal(): void {
    this.editingCommissionRule.set(null);
    this.commissionRuleForm.reset({ marketplace: '', categoryPattern: '', listingType: '', rate: null });
    this.showCommissionRuleModal.set(true);
  }

  openEditCommissionRuleModal(rule: CommissionRule): void {
    this.editingCommissionRule.set(rule);
    this.commissionRuleForm.patchValue({
      marketplace: rule.marketplace,
      categoryPattern: rule.categoryPattern,
      listingType: rule.listingType,
      rate: rule.rate * 100,
    });
    this.showCommissionRuleModal.set(true);
  }

  closeCommissionRuleModal(): void {
    this.showCommissionRuleModal.set(false);
    this.editingCommissionRule.set(null);
  }

  onCommissionRuleModalBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.closeCommissionRuleModal();
    }
  }

  onCommissionRuleModalKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.closeCommissionRuleModal();
    }
  }

  saveCommissionRule(): void {
    if (!this.commissionRuleForm.valid) return;

    this.saving.set(true);
    const formValue = this.commissionRuleForm.value;
    const dto = { ...formValue, rate: formValue.rate / 100 };
    const editing = this.editingCommissionRule();

    if (editing) {
      this.settingsService.updateCommissionRule(editing.id, dto).subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.commissionRules.update(rules =>
            rules.map(r => r.id === editing.id ? updated : r)
          );
          this.closeCommissionRuleModal();
          this.toastService.show('Regra de comissão atualizada', 'success');
        },
        error: () => {
          this.saving.set(false);
          this.toastService.show('Erro ao atualizar regra', 'danger');
        },
      });
    } else {
      this.settingsService.createCommissionRule(dto).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.commissionRules.update(rules => [...rules, created]);
          this.closeCommissionRuleModal();
          this.toastService.show('Regra de comissão criada', 'success');
        },
        error: () => {
          this.saving.set(false);
          this.toastService.show('Erro ao criar regra', 'danger');
        },
      });
    }
  }

  async deleteCommissionRule(rule: CommissionRule): Promise<void> {
    if (rule.isDefault) return;
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover regra',
      message: `Deseja remover a regra de comissão para "${rule.categoryPattern}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.settingsService.deleteCommissionRule(rule.id).subscribe({
      next: () => {
        this.confirmDialog.done();
        this.commissionRules.update(rules => rules.filter(r => r.id !== rule.id));
        this.toastService.show('Regra de comissão removida', 'success');
      },
      error: () => {
        this.confirmDialog.done();
        this.toastService.show('Erro ao remover regra', 'danger');
      },
    });
  }

  // Payment Fee Rules
  openNewPaymentFeeRuleModal(): void {
    this.editingPaymentFeeRule.set(null);
    this.paymentFeeRuleForm.reset({ installmentMin: 1, installmentMax: 1, feePercentage: null });
    this.showPaymentFeeRuleModal.set(true);
  }

  openEditPaymentFeeRuleModal(rule: PaymentFeeRule): void {
    this.editingPaymentFeeRule.set(rule);
    this.paymentFeeRuleForm.patchValue({
      installmentMin: rule.installmentMin,
      installmentMax: rule.installmentMax,
      feePercentage: rule.feePercentage,
    });
    this.showPaymentFeeRuleModal.set(true);
  }

  closePaymentFeeRuleModal(): void {
    this.showPaymentFeeRuleModal.set(false);
    this.editingPaymentFeeRule.set(null);
  }

  savePaymentFeeRule(): void {
    if (!this.paymentFeeRuleForm.valid) {
      this.paymentFeeRuleForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const formValue = this.paymentFeeRuleForm.value;
    const dto = {
      installmentMin: formValue.installmentMin,
      installmentMax: formValue.installmentMax,
      feePercentage: formValue.feePercentage,
    };
    const editing = this.editingPaymentFeeRule();

    if (editing) {
      this.settingsService.updatePaymentFeeRule(editing.id, dto).subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.paymentFeeRules.update(rules =>
            rules.map(r => r.id === editing.id ? updated : r)
          );
          this.closePaymentFeeRuleModal();
          this.toastService.show('Regra de taxa de pagamento atualizada', 'success');
        },
        error: () => {
          this.saving.set(false);
          this.toastService.show('Erro ao atualizar regra', 'danger');
        },
      });
    } else {
      this.settingsService.createPaymentFeeRule(dto).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.paymentFeeRules.update(rules => [...rules, created]);
          this.closePaymentFeeRuleModal();
          this.toastService.show('Regra de taxa de pagamento criada', 'success');
        },
        error: () => {
          this.saving.set(false);
          this.toastService.show('Erro ao criar regra', 'danger');
        },
      });
    }
  }

  async deletePaymentFeeRule(rule: PaymentFeeRule): Promise<void> {
    if (rule.isDefault) return;
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover regra',
      message: `Deseja remover a regra de taxa de pagamento para ${rule.installmentMin}-${rule.installmentMax}x?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.settingsService.deletePaymentFeeRule(rule.id).subscribe({
      next: () => {
        this.confirmDialog.done();
        this.paymentFeeRules.update(rules => rules.filter(r => r.id !== rule.id));
        this.toastService.show('Regra de taxa de pagamento removida', 'success');
      },
      error: () => {
        this.confirmDialog.done();
        this.toastService.show('Erro ao remover regra', 'danger');
      },
    });
  }

  // Tax Rate
  saveTaxRate(): void {
    if (!this.taxRateForm.valid) return;
    const { taxRate } = this.taxRateForm.value;
    this.saving.set(true);
    this.settingsService.updateCosts({ taxRate }).subscribe({
      next: () => {
        this.saving.set(false);
        this.taxRate.set(taxRate);
        this.toastService.show('Alíquota de imposto atualizada', 'success');
      },
      error: () => {
        this.saving.set(false);
        this.toastService.show('Erro ao atualizar alíquota', 'danger');
      },
    });
  }

  // Tax Profile (Fiscal tab)
  saveTaxProfile(): void {
    if (!this.taxProfileForm.valid) {
      this.taxProfileForm.markAllAsTouched();
      return;
    }
    this.savingTaxProfile.set(true);
    const { taxRegime, aliquotPercentage, state } = this.taxProfileForm.value;
    this.settingsService.updateTaxProfile({
      taxRegime,
      aliquotPercentage,
      state: state || null,
    }).subscribe({
      next: (updated) => {
        this.savingTaxProfile.set(false);
        this.taxProfile.set(updated);
        this.toastService.show('Perfil fiscal atualizado', 'success');
      },
      error: () => {
        this.savingTaxProfile.set(false);
        this.toastService.show('Erro ao atualizar perfil fiscal', 'danger');
      },
    });
  }

  // Appearance
  selectTheme(theme: ThemePreference): void {
    this.themeService.setTheme(theme);
  }

  private loadUsers(): void {
    this.tenantService.getMembers().subscribe({
      next: (members) => {
        const mapped: UserRow[] = members.map(m => ({
          id: parseInt(m.id, 10) || 0,
          nome: m.name,
          email: m.email,
          role: m.role,
          ativo: m.isActive,
        }));
        this.users.set(mapped);
      },
      error: (err) => console.error('Failed to load members:', err),
    });
  }

  private loadIntegrations(): void {
    this.settingsService.getIntegrations().subscribe({
      next: (data) => this.integrations.set(data),
      error: (err) => console.error('Failed to load integrations:', err),
    });
  }

  private loadCommissionRules(): void {
    this.settingsService.getCommissionRules().subscribe({
      next: (data) => this.commissionRules.set(data),
      error: (err) => console.error('Failed to load commission rules:', err),
    });
  }

  private loadCosts(): void {
    this.settingsService.getCosts().subscribe({
      next: (data) => {
        this.embalagemPadrao.set(data.embalagemPadrao);
        this.aliquotaSimples.set(data.aliquotaSimples);
        this.fixedCosts.set(data.fixedCosts);
        this.fixedCostsForm.patchValue({
          embalagemPadrao: data.embalagemPadrao,
          aliquotaSimples: data.aliquotaSimples,
        });
      },
      error: (err) => console.error('Failed to load costs:', err),
    });
  }

  private loadPaymentFeeRules(): void {
    this.settingsService.getPaymentFeeRules().subscribe({
      next: (data) => this.paymentFeeRules.set(data),
      error: (err) => console.error('Failed to load payment fee rules:', err),
    });
  }

  private loadTaxProfile(): void {
    this.settingsService.getTaxProfile().subscribe({
      next: (data) => {
        this.taxProfile.set(data);
        this.taxProfileForm.patchValue({
          taxRegime: data.taxRegime,
          aliquotPercentage: data.aliquotPercentage,
          state: data.state || '',
        });
      },
      error: (err) => console.error('Failed to load tax profile:', err),
    });
  }

  // --- Report Schedules ---

  private loadReportSchedules(): void {
    this.settingsService.getReportSchedules().subscribe({
      next: (data) => this.reportSchedules.set(data),
      error: (err) => console.error('Failed to load report schedules:', err),
    });
  }

  openNewReportScheduleModal(): void {
    this.editingReportSchedule.set(null);
    this.reportScheduleForm.reset({ frequency: 'weekly', recipients: '', isActive: true });
    this.showReportScheduleModal.set(true);
  }

  openEditReportScheduleModal(schedule: ReportSchedule): void {
    this.editingReportSchedule.set(schedule);
    this.reportScheduleForm.patchValue({
      frequency: schedule.frequency,
      recipients: schedule.recipients,
      isActive: schedule.isActive,
    });
    this.showReportScheduleModal.set(true);
  }

  closeReportScheduleModal(): void {
    this.showReportScheduleModal.set(false);
    this.editingReportSchedule.set(null);
  }

  saveReportSchedule(): void {
    if (!this.reportScheduleForm.valid) {
      this.reportScheduleForm.markAllAsTouched();
      return;
    }

    this.savingReportSchedule.set(true);
    const { frequency, recipients, isActive } = this.reportScheduleForm.value;
    const dto = { frequency, recipients: recipients.trim(), isActive };
    const editing = this.editingReportSchedule();

    if (editing) {
      this.settingsService.updateReportSchedule(editing.id, dto).subscribe({
        next: (updated) => {
          this.savingReportSchedule.set(false);
          this.reportSchedules.update(list => list.map(s => s.id === editing.id ? updated : s));
          this.closeReportScheduleModal();
          this.toastService.show('Agendamento atualizado', 'success');
        },
        error: () => {
          this.savingReportSchedule.set(false);
          this.toastService.show('Erro ao atualizar agendamento', 'danger');
        },
      });
    } else {
      this.settingsService.createReportSchedule(dto).subscribe({
        next: (created) => {
          this.savingReportSchedule.set(false);
          this.reportSchedules.update(list => [...list, created]);
          this.closeReportScheduleModal();
          this.toastService.show('Agendamento criado', 'success');
        },
        error: () => {
          this.savingReportSchedule.set(false);
          this.toastService.show('Erro ao criar agendamento', 'danger');
        },
      });
    }
  }

  async deleteReportSchedule(schedule: ReportSchedule): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover agendamento',
      message: `Deseja remover o agendamento de relatório ${schedule.frequency === 'weekly' ? 'semanal' : 'mensal'} para "${schedule.recipients}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.settingsService.deleteReportSchedule(schedule.id).subscribe({
      next: () => {
        this.confirmDialog.done();
        this.reportSchedules.update(list => list.filter(s => s.id !== schedule.id));
        this.toastService.show('Agendamento removido', 'success');
      },
      error: () => {
        this.confirmDialog.done();
        this.toastService.show('Erro ao remover agendamento', 'danger');
      },
    });
  }

  toggleReportScheduleActive(schedule: ReportSchedule): void {
    const dto = {
      frequency: schedule.frequency,
      recipients: schedule.recipients,
      isActive: !schedule.isActive,
    };
    this.settingsService.updateReportSchedule(schedule.id, dto).subscribe({
      next: (updated) => {
        this.reportSchedules.update(list => list.map(s => s.id === schedule.id ? updated : s));
        this.toastService.show(
          updated.isActive ? 'Agendamento ativado' : 'Agendamento desativado',
          'success'
        );
      },
      error: () => {
        this.toastService.show('Erro ao atualizar agendamento', 'danger');
      },
    });
  }
}
