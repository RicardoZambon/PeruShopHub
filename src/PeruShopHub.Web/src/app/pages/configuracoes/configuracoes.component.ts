import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

type SettingsTab = 'empresa' | 'usuarios' | 'integracoes' | 'custos-fixos' | 'alertas' | 'aparencia';

interface UserRow {
  id: number;
  nome: string;
  email: string;
  role: 'Admin' | 'Manager' | 'Viewer';
  ativo: boolean;
}

interface Integration {
  id: string;
  name: string;
  logo: string;
  connected: boolean;
  sellerNickname?: string;
  lastSync?: string;
  comingSoon?: boolean;
}

const MOCK_USERS: UserRow[] = [
  { id: 1, nome: 'Carlos Silva', email: 'carlos@perushophub.com', role: 'Admin', ativo: true },
  { id: 2, nome: 'Ana Costa', email: 'ana@perushophub.com', role: 'Manager', ativo: true },
  { id: 3, nome: 'Pedro Santos', email: 'pedro@perushophub.com', role: 'Viewer', ativo: false },
];

const MOCK_INTEGRATIONS: Integration[] = [
  {
    id: 'mercadolivre',
    name: 'Mercado Livre',
    logo: 'ML',
    connected: true,
    sellerNickname: 'PERUSHOP_OFICIAL',
    lastSync: '2026-03-22 14:45',
  },
  {
    id: 'amazon',
    name: 'Amazon',
    logo: 'AZ',
    connected: false,
    comingSoon: true,
  },
];

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BadgeComponent],
  templateUrl: './configuracoes.component.html',
  styleUrl: './configuracoes.component.scss',
})
export class ConfiguracoesComponent {
  activeTab = signal<SettingsTab>('empresa');
  showUserModal = signal(false);
  editingUser = signal<UserRow | null>(null);

  tabs: { key: SettingsTab; label: string; disabled?: boolean }[] = [
    { key: 'empresa', label: 'Empresa' },
    { key: 'usuarios', label: 'Usuários' },
    { key: 'integracoes', label: 'Integrações' },
    { key: 'custos-fixos', label: 'Custos Fixos', disabled: true },
    { key: 'alertas', label: 'Alertas', disabled: true },
    { key: 'aparencia', label: 'Aparência', disabled: true },
  ];

  users = signal<UserRow[]>([...MOCK_USERS]);
  integrations = signal<Integration[]>([...MOCK_INTEGRATIONS]);

  companyForm: FormGroup;
  userForm: FormGroup;

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
}
