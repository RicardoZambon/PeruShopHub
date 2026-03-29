import { Injectable, inject, signal, effect } from '@angular/core';
import { Subscription } from 'rxjs';
import { QuestionService } from './question.service';
import { MessageService } from './message.service';
import { ClaimService } from './claim.service';
import { SignalRService } from './signalr.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SidebarBadgeService {
  private readonly questionService = inject(QuestionService);
  private readonly messageService = inject(MessageService);
  private readonly claimService = inject(ClaimService);
  private readonly signalR = inject(SignalRService);
  private readonly auth = inject(AuthService);
  private signalRSub: Subscription | null = null;

  readonly unansweredQuestions = signal(0);
  readonly unreadMessages = signal(0);
  readonly openClaims = signal(0);

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();
      if (user) {
        this.refreshQuestionCount();
        this.refreshUnreadMessages();
        this.refreshOpenClaims();
        this.subscribeToChanges();
      } else {
        this.unansweredQuestions.set(0);
        this.unreadMessages.set(0);
        this.openClaims.set(0);
        this.unsubscribe();
      }
    });
  }

  refreshQuestionCount(): void {
    this.questionService.list({ status: 'UNANSWERED', pageSize: 1 }).subscribe({
      next: (res) => this.unansweredQuestions.set(res.totalCount),
      error: () => {},
    });
  }

  refreshUnreadMessages(): void {
    this.messageService.getUnreadCount().subscribe({
      next: (res) => this.unreadMessages.set(res.unreadCount),
      error: () => {},
    });
  }

  refreshOpenClaims(): void {
    this.claimService.getSummary().subscribe({
      next: (res) => this.openClaims.set(res.openCount),
      error: () => {},
    });
  }

  private subscribeToChanges(): void {
    this.unsubscribe();
    this.signalRSub = this.signalR.dataChanged$.subscribe(event => {
      if (event.entity === 'question') {
        this.refreshQuestionCount();
      }
      if (event.entity === 'message') {
        this.refreshUnreadMessages();
      }
      if (event.entity === 'claim') {
        this.refreshOpenClaims();
      }
    });
  }

  private unsubscribe(): void {
    this.signalRSub?.unsubscribe();
    this.signalRSub = null;
  }
}
