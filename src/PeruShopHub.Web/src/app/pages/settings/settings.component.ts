import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { ThemeService } from '../../services/theme.service';
import type { ThemePreference } from '../../services/theme.service';
import { SettingsService, type UserRow, type Integration, type FixedCostsResponse, type CommissionRule } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';

type SettingsTab = 'empresa' | 'usuarios' | 'integracoes' | 'custos-fixos' | 'alertas' | 'aparencia';

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
  imports: [CommonModule, ReactiveFormsModule, BadgeComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  private readonly themeService = inject(ThemeService);
  private readonly settingsService = inject(SettingsService);
  private readonly toastService = inject(ToastService);

  activeTab = signal<SettingsTab>('empresa');
  showUserModal = signal(false);
  editingUser = signal<UserRow | null>(null);

  tabs: { key: SettingsTab; label: string; disabled?: boolean }[] = [
    { key: 'empresa', label: 'Empresa' },
    { key: 'usuarios', label: 'Usuários' },
    { key: 'integracoes', label: 'Integrações' },
    { key: 'custos-fixos', label: 'Custos Fixos' },
    { key: 'alertas', label: 'Alertas' },
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

  // Tax rate
  taxRate = signal(0);
  taxRateForm!: FormGroup;

  // Alerts
  alerts = signal<AlertConfig[]>([
    { id: 'margem', label: 'Margem mínima', description: 'Alerta quando a margem de um produto ficar abaixo do limite', enabled: true, threshold: 10, unit: '%' },
    { id: 'estoque', label: 'Estoque mínimo', description: 'Alerta quando o estoque ficar abaixo do limite', enabled: true, threshold: 5, unit: 'unidades' },
    { id: 'pergunta', label: 'Pergunta sem resposta', description: 'Alerta quando uma pergunta ficar sem resposta por mais de', enabled: false, threshold: 24, unit: 'horas' },
    { id: 'divergencia', label: 'Divergência financeira', description: 'Alerta quando a divergência entre esperado e depositado exceder', enabled: false, threshold: 5, unit: '%' },
  ]);

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

    this.taxRateForm = this.fb.group({
      taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadIntegrations();
    this.loadCosts();
    this.loadCommissionRules();
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
      // Mock save - would call API
      alert('Dados da empresa salvos com sucesso!');
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
    this.userForm.reset({ nome: '', email: '', role: 'Viewer', ativo: true });
    this.showUserModal.set(true);
  }

  openEditUserModal(user: UserRow): void {
    this.editingUser.set(user);
    this.userForm.patchValue({
      nome: user.nome,
      email: user.email,
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
    if (!this.userForm.valid) return;

    const formValue = this.userForm.value;
    const editing = this.editingUser();

    if (editing) {
      this.users.update(users =>
        users.map(u => u.id === editing.id ? { ...u, ...formValue } : u)
      );
    } else {
      const newId = Math.max(...this.users().map(u => u.id)) + 1;
      this.users.update(users => [...users, { id: newId, ...formValue }]);
    }

    this.closeUserModal();
  }

  deleteUser(user: UserRow): void {
    if (confirm(`Deseja remover o usuário "${user.nome}"?`)) {
      this.users.update(users => users.filter(u => u.id !== user.id));
    }
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
      this.embalagemPadrao.set(this.fixedCostsForm.value.embalagemPadrao);
      this.aliquotaSimples.set(this.fixedCostsForm.value.aliquotaSimples);
      alert('Custos fixos salvos com sucesso!');
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

  removeFixedCost(id: number): void {
    this.fixedCosts.update(list => list.filter(c => c.id !== id));
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

    const formValue = this.commissionRuleForm.value;
    const dto = { ...formValue, rate: formValue.rate / 100 };
    const editing = this.editingCommissionRule();

    if (editing) {
      this.settingsService.updateCommissionRule(editing.id, dto).subscribe({
        next: () => {
          this.loadCommissionRules();
          this.closeCommissionRuleModal();
          this.toastService.show('Regra de comissão atualizada', 'success');
        },
        error: () => this.toastService.show('Erro ao atualizar regra', 'danger'),
      });
    } else {
      this.settingsService.createCommissionRule(dto).subscribe({
        next: () => {
          this.loadCommissionRules();
          this.closeCommissionRuleModal();
          this.toastService.show('Regra de comissão criada', 'success');
        },
        error: () => this.toastService.show('Erro ao criar regra', 'danger'),
      });
    }
  }

  deleteCommissionRule(rule: CommissionRule): void {
    if (rule.isDefault) return;
    if (!confirm(`Deseja remover a regra de comissão para "${rule.categoryPattern}"?`)) return;

    this.settingsService.deleteCommissionRule(rule.id).subscribe({
      next: () => {
        this.loadCommissionRules();
        this.toastService.show('Regra de comissão removida', 'success');
      },
      error: () => this.toastService.show('Erro ao remover regra', 'danger'),
    });
  }

  // Tax Rate
  saveTaxRate(): void {
    if (!this.taxRateForm.valid) return;
    const { taxRate } = this.taxRateForm.value;
    this.settingsService.updateCosts({ taxRate }).subscribe({
      next: () => {
        this.taxRate.set(taxRate);
        this.toastService.show('Alíquota de imposto atualizada', 'success');
      },
      error: () => this.toastService.show('Erro ao atualizar alíquota', 'danger'),
    });
  }

  // Appearance
  selectTheme(theme: ThemePreference): void {
    this.themeService.setTheme(theme);
  }

  private loadUsers(): void {
    this.settingsService.getUsers().subscribe({
      next: (data) => this.users.set(data),
      error: (err) => console.error('Failed to load users:', err),
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
}
