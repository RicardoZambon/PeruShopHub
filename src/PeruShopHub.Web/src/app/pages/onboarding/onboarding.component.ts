import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  LucideAngularModule,
  User,
  Link,
  Package,
  DollarSign,
  TrendingUp,
  Check,
  ChevronRight,
  PartyPopper,
  Rocket,
  type LucideIconData,
} from 'lucide-angular';
import { OnboardingService, OnboardingProgress, OnboardingStep } from '../../services/onboarding.service';
import { firstValueFrom } from 'rxjs';

interface WizardStep {
  key: string;
  label: string;
  description: string;
  icon: LucideIconData;
  actionLabel: string;
  actionRoute: string;
  completed: boolean;
}

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule],
  templateUrl: './onboarding.component.html',
  styleUrl: './onboarding.component.scss',
})
export class OnboardingComponent implements OnInit {
  private readonly onboardingService = inject(OnboardingService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly steps = signal<WizardStep[]>([]);
  readonly allCompleted = signal(false);
  readonly showConfetti = signal(false);
  readonly activeStepIndex = signal(0);
  readonly dismissing = signal(false);

  // Icons
  readonly checkIcon = Check;
  readonly chevronRightIcon = ChevronRight;
  readonly partyPopperIcon = PartyPopper;
  readonly rocketIcon = Rocket;

  private readonly stepMeta: Record<string, { description: string; icon: LucideIconData; actionLabel: string; actionRoute: string }> = {
    'profile': {
      description: 'Complete seu perfil com informações da empresa para personalizar sua experiência.',
      icon: User,
      actionLabel: 'Ir para Perfil',
      actionRoute: '/perfil',
    },
    'connect_ml': {
      description: 'Conecte sua conta do Mercado Livre para sincronizar produtos, pedidos e estoque.',
      icon: Link,
      actionLabel: 'Conectar Mercado Livre',
      actionRoute: '/configuracoes',
    },
    'import_products': {
      description: 'Importe seus produtos do marketplace para gerenciá-los em um só lugar.',
      icon: Package,
      actionLabel: 'Importar Produtos',
      actionRoute: '/produtos',
    },
    'set_costs': {
      description: 'Defina o custo de cada produto para calcular a lucratividade real por venda.',
      icon: DollarSign,
      actionLabel: 'Definir Custos',
      actionRoute: '/produtos',
    },
    'view_profitability': {
      description: 'Veja o lucro real de cada venda com todos os custos decompostos automaticamente.',
      icon: TrendingUp,
      actionLabel: 'Ver Lucratividade',
      actionRoute: '/financeiro',
    },
  };

  readonly completedCount = computed(() => this.steps().filter(s => s.completed).length);
  readonly totalSteps = computed(() => this.steps().length);
  readonly progressPercent = computed(() => {
    const total = this.totalSteps();
    if (total === 0) return 0;
    return Math.round((this.completedCount() / total) * 100);
  });

  async ngOnInit(): Promise<void> {
    await this.loadProgress();
  }

  private async loadProgress(): Promise<void> {
    try {
      const progress = await firstValueFrom(this.onboardingService.getProgress());
      const wizardSteps = progress.steps.map((s: OnboardingStep) => {
        const meta = this.stepMeta[s.key];
        return {
          key: s.key,
          label: s.label,
          description: meta?.description ?? '',
          icon: meta?.icon ?? User,
          actionLabel: meta?.actionLabel ?? 'Ir',
          actionRoute: meta?.actionRoute ?? '/dashboard',
          completed: s.completed,
        };
      });
      this.steps.set(wizardSteps);
      this.allCompleted.set(progress.isCompleted);

      // Set active step to first incomplete, or last step if all done
      const firstIncomplete = wizardSteps.findIndex((s: WizardStep) => !s.completed);
      this.activeStepIndex.set(firstIncomplete >= 0 ? firstIncomplete : wizardSteps.length - 1);

      if (progress.isCompleted) {
        this.triggerConfetti();
      }
    } finally {
      this.loading.set(false);
    }
  }

  async completeStep(stepKey: string): Promise<void> {
    const progress = await firstValueFrom(this.onboardingService.completeStep(stepKey));
    const updated = this.steps().map(s => {
      const apiStep = progress.steps.find((as: OnboardingStep) => as.key === s.key);
      return { ...s, completed: apiStep?.completed ?? s.completed };
    });
    this.steps.set(updated);
    this.allCompleted.set(progress.isCompleted);

    // Move to next incomplete step
    const nextIncomplete = updated.findIndex(s => !s.completed);
    if (nextIncomplete >= 0) {
      this.activeStepIndex.set(nextIncomplete);
    } else {
      // All steps completed — show celebration
      this.activeStepIndex.set(updated.length - 1);
      this.triggerConfetti();
    }
  }

  selectStep(index: number): void {
    this.activeStepIndex.set(index);
  }

  navigateToStep(route: string): void {
    this.router.navigate([route]);
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  dismiss(): void {
    this.dismissing.set(true);
    localStorage.setItem('psh_onboarding_dismissed', 'true');
    this.router.navigate(['/dashboard']);
  }

  private triggerConfetti(): void {
    this.showConfetti.set(true);
    setTimeout(() => this.showConfetti.set(false), 5000);
  }
}
