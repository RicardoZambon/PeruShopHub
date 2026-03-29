import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { LucideAngularModule, MessageCircle, Clock, Package, Send, X, Search } from 'lucide-angular';
import { BadgeComponent, type BadgeVariant } from '../../shared/components';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TabBarComponent, type TabItem } from '../../shared/components/tab-bar/tab-bar.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { RelativeDatePipe } from '../../shared/pipes';
import { ToastService } from '../../services/toast.service';
import { QuestionService, type QuestionListItem } from '../../services/question.service';
import { SignalRService } from '../../services/signalr.service';

type TabFilter = 'unanswered' | 'answered' | 'all';

@Component({
  selector: 'app-questions',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, RelativeDatePipe, PageHeaderComponent, TabBarComponent, ButtonComponent],
  templateUrl: './questions.component.html',
  styleUrl: './questions.component.scss',
})
export class QuestionsComponent implements OnInit, OnDestroy {
  private readonly questionService = inject(QuestionService);
  private readonly signalR = inject(SignalRService);
  private readonly toastService = inject(ToastService);
  private subs = new Subscription();

  readonly messageCircleIcon = MessageCircle;
  readonly clockIcon = Clock;
  readonly packageIcon = Package;
  readonly sendIcon = Send;
  readonly xIcon = X;
  readonly searchIcon = Search;

  readonly tabItems: TabItem[] = [
    { key: 'unanswered', label: 'Sem Resposta' },
    { key: 'answered', label: 'Respondidas' },
    { key: 'all', label: 'Todas' },
  ];

  readonly activeTab = signal<TabFilter>('unanswered');
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly replyingTo = signal<string | null>(null);
  readonly replyText = signal('');
  readonly searchQuery = signal('');

  readonly questions = signal<QuestionListItem[]>([]);

  readonly unansweredCount = computed(() =>
    this.questions().filter(q => q.status === 'UNANSWERED').length
  );

  readonly filteredQuestions = computed(() => {
    const tab = this.activeTab();
    const search = this.searchQuery().toLowerCase().trim();
    let qs = this.questions();

    // Filter by tab
    if (tab === 'unanswered') {
      qs = qs.filter(q => q.status === 'UNANSWERED');
    } else if (tab === 'answered') {
      qs = qs.filter(q => q.status === 'ANSWERED');
    }

    // Filter by search (product name/id or buyer)
    if (search) {
      qs = qs.filter(q =>
        q.externalItemId.toLowerCase().includes(search) ||
        q.buyerName.toLowerCase().includes(search) ||
        q.questionText.toLowerCase().includes(search)
      );
    }

    // Sort: unanswered oldest first (urgency), answered newest first
    return [...qs].sort((a, b) => {
      if (a.status === 'UNANSWERED' && b.status === 'UNANSWERED') {
        return new Date(a.questionDate).getTime() - new Date(b.questionDate).getTime();
      }
      if (a.status === 'UNANSWERED') return -1;
      if (b.status === 'UNANSWERED') return 1;
      return new Date(b.questionDate).getTime() - new Date(a.questionDate).getTime();
    });
  });

  ngOnInit(): void {
    this.loadQuestions();

    // Listen for real-time question updates
    this.subs.add(
      this.signalR.dataChanged$.subscribe(event => {
        if (event.entity === 'question') {
          this.loadQuestions();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  setTab(tab: TabFilter): void {
    this.activeTab.set(tab);
    this.cancelReply();
  }

  isOld(date: string): boolean {
    const hours = (Date.now() - new Date(date).getTime()) / (1000 * 60 * 60);
    return hours > 24;
  }

  startReply(questionId: string): void {
    this.replyingTo.set(questionId);
    this.replyText.set('');
  }

  cancelReply(): void {
    this.replyingTo.set(null);
    this.replyText.set('');
  }

  submitReply(question: QuestionListItem): void {
    const text = this.replyText().trim();
    if (!text || this.submitting()) return;

    this.submitting.set(true);
    this.questionService.answer(question.id, text).subscribe({
      next: (updated) => {
        // Update local state with response
        this.questions.update(qs =>
          qs.map(q => q.id === updated.id ? updated : q)
        );
        this.replyingTo.set(null);
        this.replyText.set('');
        this.submitting.set(false);
        this.toastService.show('Resposta enviada com sucesso!', 'success');
      },
      error: () => {
        this.submitting.set(false);
        this.toastService.show('Erro ao enviar resposta. Tente novamente.', 'danger');
      },
    });
  }

  getStatusVariant(status: string): BadgeVariant {
    return status === 'ANSWERED' ? 'success' : 'accent';
  }

  getStatusLabel(status: string): string {
    return status === 'ANSWERED' ? 'Respondida' : 'Pendente';
  }

  private loadQuestions(): void {
    this.loading.set(true);
    // Load all questions (no server-side status filter — we filter client-side for tabs)
    this.questionService.list({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.questions.set(res.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.show('Erro ao carregar perguntas.', 'danger');
      },
    });
  }
}
