import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LucideAngularModule, Camera, Trash2, User, Mail, Lock, Users, Shield, Download, AlertTriangle, X } from 'lucide-angular';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { DialogComponent } from '../../shared/components/dialog/dialog.component';
import { ConfirmDialogService } from '../../shared/components';
import { ProfileService, type Profile, type UserDataExport, type AccountDeletion } from '../../services/profile.service';
import { TenantService, type TenantMember } from '../../services/tenant.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

type ProfileTab = 'perfil' | 'equipe';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideAngularModule,
    PageHeaderComponent,
    ButtonComponent,
    FormFieldComponent,
    FormActionsComponent,
    BadgeComponent,
    DialogComponent,
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly tenantService = inject(TenantService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  // Icons
  readonly cameraIcon = Camera;
  readonly trashIcon = Trash2;
  readonly userIcon = User;
  readonly mailIcon = Mail;
  readonly lockIcon = Lock;
  readonly usersIcon = Users;
  readonly shieldIcon = Shield;
  readonly downloadIcon = Download;
  readonly alertIcon = AlertTriangle;
  readonly xIcon = X;

  // State
  readonly activeTab = signal<ProfileTab>('perfil');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly savingEmail = signal(false);
  readonly savingPassword = signal(false);
  readonly uploadingAvatar = signal(false);
  readonly profile = signal<Profile | null>(null);

  // Team
  readonly members = signal<TenantMember[]>([]);
  readonly loadingMembers = signal(false);
  readonly showInviteModal = signal(false);
  readonly inviting = signal(false);

  // Forms
  profileForm!: FormGroup;
  emailForm!: FormGroup;
  passwordForm!: FormGroup;
  inviteForm!: FormGroup;

  readonly isOwnerOrAdmin = signal(false);
  readonly exportingData = signal(false);
  readonly dataExport = signal<UserDataExport | null>(null);

  // Account Deletion
  readonly showDeleteModal = signal(false);
  readonly deletionStep = signal<1 | 2>(1);
  readonly deletingAccount = signal(false);
  readonly cancellingDeletion = signal(false);
  readonly pendingDeletion = signal<AccountDeletion | null>(null);
  deleteForm!: FormGroup;

  ngOnInit(): void {
    const role = this.auth.tenantRole();
    this.isOwnerOrAdmin.set(role === 'Owner' || role === 'Admin');

    this.profileForm = this.fb.group({
      name: ['', Validators.required],
    });

    this.emailForm = this.fb.group({
      newEmail: ['', [Validators.required, Validators.email]],
      currentPassword: ['', Validators.required],
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    });

    this.inviteForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      role: ['Viewer', Validators.required],
    });

    this.deleteForm = this.fb.group({
      password: ['', Validators.required],
      confirmPhrase: ['', Validators.required],
    });

    this.loadProfile();
    this.loadDeletionStatus();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.profileService.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.profileForm.patchValue({ name: p.name });
        this.loading.set(false);
      },
      error: () => {
        this.toast.show('Erro ao carregar perfil.', 'danger');
        this.loading.set(false);
      },
    });
  }

  switchTab(tab: ProfileTab): void {
    this.activeTab.set(tab);
    if (tab === 'equipe' && this.members().length === 0) {
      this.loadMembers();
    }
  }

  loadMembers(): void {
    this.loadingMembers.set(true);
    this.tenantService.getMembers().subscribe({
      next: (m) => {
        this.members.set(m);
        this.loadingMembers.set(false);
      },
      error: () => {
        this.toast.show('Erro ao carregar membros.', 'danger');
        this.loadingMembers.set(false);
      },
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.profileService.updateProfile(this.profileForm.value.name).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.toast.show('Perfil atualizado com sucesso.', 'success');
        this.saving.set(false);
      },
      error: () => {
        this.toast.show('Erro ao salvar perfil.', 'danger');
        this.saving.set(false);
      },
    });
  }

  saveEmail(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }
    this.savingEmail.set(true);
    const { newEmail, currentPassword } = this.emailForm.value;
    this.profileService.updateEmail(newEmail, currentPassword).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.emailForm.reset();
        this.toast.show('E-mail atualizado com sucesso.', 'success');
        this.savingEmail.set(false);
      },
      error: () => {
        this.toast.show('Erro ao atualizar e-mail. Verifique a senha.', 'danger');
        this.savingEmail.set(false);
      },
    });
  }

  savePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;
    if (newPassword !== confirmPassword) {
      this.toast.show('As senhas não coincidem.', 'danger');
      return;
    }
    this.savingPassword.set(true);
    this.profileService.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordForm.reset();
        this.toast.show('Senha alterada com sucesso.', 'success');
        this.savingPassword.set(false);
      },
      error: () => {
        this.toast.show('Erro ao alterar senha. Verifique a senha atual.', 'danger');
        this.savingPassword.set(false);
      },
    });
  }

  onAvatarFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      this.toast.show('Formato inválido. Use JPG, PNG ou WebP.', 'danger');
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      this.toast.show('Arquivo deve ter no máximo 2MB.', 'danger');
      return;
    }

    this.uploadingAvatar.set(true);
    this.profileService.uploadAvatar(file).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.toast.show('Foto atualizada.', 'success');
        this.uploadingAvatar.set(false);
      },
      error: () => {
        this.toast.show('Erro ao enviar foto.', 'danger');
        this.uploadingAvatar.set(false);
      },
    });
    input.value = '';
  }

  async removeAvatar(): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover foto',
      message: 'Deseja remover sua foto de perfil?',
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.uploadingAvatar.set(true);
    this.profileService.removeAvatar().subscribe({
      next: () => {
        this.profile.update(p => p ? { ...p, avatarUrl: null } : null);
        this.toast.show('Foto removida.', 'success');
        this.uploadingAvatar.set(false);
      },
      error: () => {
        this.toast.show('Erro ao remover foto.', 'danger');
        this.uploadingAvatar.set(false);
      },
    });
  }

  getInitials(): string {
    const name = this.profile()?.name ?? '';
    const parts = name.split(' ').filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return name.substring(0, 2).toUpperCase();
  }

  getRoleBadge(role: string): BadgeVariant {
    if (role === 'Owner') return 'primary';
    if (role === 'Admin') return 'accent';
    if (role === 'Manager') return 'warning';
    return 'neutral';
  }

  getRoleLabel(role: string): string {
    const map: Record<string, string> = {
      Owner: 'Proprietário',
      Admin: 'Administrador',
      Manager: 'Gerente',
      Viewer: 'Visualizador',
    };
    return map[role] ?? role;
  }

  openInviteModal(): void {
    this.inviteForm.reset({ role: 'Viewer' });
    this.showInviteModal.set(true);
  }

  closeInviteModal(): void {
    this.showInviteModal.set(false);
  }

  submitInvite(): void {
    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }
    this.inviting.set(true);
    this.tenantService.inviteMember(this.inviteForm.value).subscribe({
      next: (member) => {
        this.members.update(list => [...list, member]);
        this.toast.show('Membro convidado com sucesso.', 'success');
        this.inviting.set(false);
        this.closeInviteModal();
      },
      error: () => {
        this.toast.show('Erro ao convidar membro.', 'danger');
        this.inviting.set(false);
      },
    });
  }

  async removeMember(member: TenantMember): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover membro',
      message: `Deseja remover "${member.name}" da equipe?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;

    this.tenantService.removeMember(member.id).subscribe({
      next: () => {
        this.members.update(list => list.filter(m => m.id !== member.id));
        this.toast.show('Membro removido.', 'success');
      },
      error: () => {
        this.toast.show('Erro ao remover membro.', 'danger');
      },
    });
  }

  requestDataExport(): void {
    this.exportingData.set(true);
    this.profileService.requestDataExport().subscribe({
      next: (exp) => {
        this.dataExport.set(exp);
        this.exportingData.set(false);
        if (exp.status === 'Completed') {
          this.toast.show('Seus dados estão prontos para download.', 'success');
        } else {
          this.toast.show('Exportação solicitada. Você será notificado quando estiver pronta.', 'success');
        }
      },
      error: () => {
        this.toast.show('Erro ao solicitar exportação de dados.', 'danger');
        this.exportingData.set(false);
      },
    });
  }

  downloadExport(): void {
    const exp = this.dataExport();
    if (!exp) return;

    this.profileService.downloadExport(exp.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `dados_pessoais_${new Date().toISOString().split('T')[0]}.zip`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.toast.show('Erro ao baixar dados. O link pode ter expirado.', 'danger');
      },
    });
  }

  // Account Deletion
  loadDeletionStatus(): void {
    this.profileService.getDeletionStatus().subscribe({
      next: (d) => this.pendingDeletion.set(d),
      error: () => {},
    });
  }

  openDeleteModal(): void {
    this.deleteForm.reset();
    this.deletionStep.set(1);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    this.showDeleteModal.set(false);
  }

  nextDeleteStep(): void {
    this.deletionStep.set(2);
  }

  submitAccountDeletion(): void {
    if (this.deleteForm.invalid) {
      this.deleteForm.markAllAsTouched();
      return;
    }
    const { password, confirmPhrase } = this.deleteForm.value;
    this.deletingAccount.set(true);
    this.profileService.requestAccountDeletion(password, confirmPhrase).subscribe({
      next: (d) => {
        this.pendingDeletion.set(d);
        this.closeDeleteModal();
        this.toast.show('Solicitação de exclusão registrada. Sua conta será excluída em 30 dias.', 'warning');
        this.deletingAccount.set(false);
      },
      error: () => {
        this.toast.show('Erro ao solicitar exclusão. Verifique os dados informados.', 'danger');
        this.deletingAccount.set(false);
      },
    });
  }

  async cancelDeletion(): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Cancelar exclusão',
      message: 'Deseja cancelar a solicitação de exclusão da conta? Sua conta será reativada.',
      confirmLabel: 'Sim, cancelar exclusão',
      variant: 'primary',
    });
    if (!confirmed) return;

    this.cancellingDeletion.set(true);
    this.profileService.cancelAccountDeletion().subscribe({
      next: () => {
        this.pendingDeletion.set(null);
        this.toast.show('Exclusão cancelada. Sua conta foi reativada.', 'success');
        this.cancellingDeletion.set(false);
      },
      error: () => {
        this.toast.show('Erro ao cancelar exclusão.', 'danger');
        this.cancellingDeletion.set(false);
      },
    });
  }
}
