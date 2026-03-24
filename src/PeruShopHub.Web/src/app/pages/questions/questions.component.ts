import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, MessageCircle, Clock, Package, Send, X } from 'lucide-angular';
import { BadgeComponent, type BadgeVariant } from '../../shared/components';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TabBarComponent, type TabItem } from '../../shared/components/tab-bar/tab-bar.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { RelativeDatePipe } from '../../shared/pipes';
import { ToastService } from '../../services/toast.service';

type QuestionStatus = 'unanswered' | 'answered';
type TabFilter = 'unanswered' | 'answered' | 'all';

interface Question {
  id: number;
  productName: string;
  productId: string;
  buyerNickname: string;
  text: string;
  answer?: string;
  status: QuestionStatus;
  createdAt: string;
  answeredAt?: string;
}

@Component({
  selector: 'app-questions',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, RelativeDatePipe, PageHeaderComponent, TabBarComponent, ButtonComponent],
  templateUrl: './questions.component.html',
  styleUrl: './questions.component.scss',
})
export class QuestionsComponent {
  readonly messageCircleIcon = MessageCircle;
  readonly clockIcon = Clock;
  readonly packageIcon = Package;
  readonly sendIcon = Send;
  readonly xIcon = X;

  readonly tabItems: TabItem[] = [
    { key: 'unanswered', label: 'Sem Resposta' },
    { key: 'answered', label: 'Respondidas' },
    { key: 'all', label: 'Todas' },
  ];

  readonly activeTab = signal<TabFilter>('unanswered');
  readonly loading = signal(true);
  readonly replyingTo = signal<number | null>(null);
  readonly replyText = signal('');

  readonly questions = signal<Question[]>([
    {
      id: 1,
      productName: 'Fone de Ouvido Bluetooth TWS Pro',
      productId: 'MLB-001',
      buyerNickname: 'COMPRADOR_TECH',
      text: 'Boa tarde! Esse fone é compatível com iPhone 15? Funciona bem pra fazer ligações?',
      status: 'unanswered',
      createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 2,
      productName: 'Capa Protetora Samsung Galaxy S24',
      productId: 'MLB-003',
      buyerNickname: 'MARIA_SILVA2024',
      text: 'Essa capa serve no modelo S24 Ultra também ou só no S24 normal?',
      status: 'unanswered',
      createdAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 3,
      productName: 'Carregador Turbo USB-C 65W',
      productId: 'MLB-005',
      buyerNickname: 'JOAO_ELETRO',
      text: 'Pode ser usado para carregar notebook? Qual a voltagem de saída?',
      status: 'unanswered',
      createdAt: new Date(Date.now() - 18 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 4,
      productName: 'Película de Vidro iPhone 15 Pro Max',
      productId: 'MLB-007',
      buyerNickname: 'LUCAS_SP',
      text: 'Vem com kit de aplicação? Quantas películas vem na embalagem?',
      status: 'unanswered',
      createdAt: new Date(Date.now() - 26 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 5,
      productName: 'Mouse Gamer RGB 12000 DPI',
      productId: 'MLB-009',
      buyerNickname: 'GAMER_PRO_BR',
      text: 'Qual o peso do mouse sem o cabo? Tem software próprio para configurar os botões?',
      status: 'unanswered',
      createdAt: new Date(Date.now() - 30 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 6,
      productName: 'Fone de Ouvido Bluetooth TWS Pro',
      productId: 'MLB-001',
      buyerNickname: 'ANA_MUSIC',
      text: 'Quanto tempo dura a bateria com o estojo de carregamento?',
      answer: 'Olá! A bateria dos fones dura até 6 horas de uso contínuo, e com o estojo de carregamento você tem até 30 horas no total. Obrigado pela pergunta!',
      status: 'answered',
      createdAt: new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString(),
      answeredAt: new Date(Date.now() - 46 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 7,
      productName: 'Capa Protetora Samsung Galaxy S24',
      productId: 'MLB-003',
      buyerNickname: 'PEDRO_RJ',
      text: 'A capa é transparente ou fica amarelada com o tempo?',
      answer: 'Olá! Nossa capa é feita com material anti-amarelamento. Garantimos que mantém a transparência por no mínimo 12 meses de uso normal. Qualquer dúvida estamos à disposição!',
      status: 'answered',
      createdAt: new Date(Date.now() - 72 * 60 * 60 * 1000).toISOString(),
      answeredAt: new Date(Date.now() - 70 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: 8,
      productName: 'Carregador Turbo USB-C 65W',
      productId: 'MLB-005',
      buyerNickname: 'CARLOS_DEV',
      text: 'Vem com cabo incluído ou preciso comprar separado?',
      answer: 'Olá! O carregador acompanha um cabo USB-C de 1 metro. Se precisar de um cabo mais longo, temos opções de 2m disponíveis em nossa loja. Obrigado!',
      status: 'answered',
      createdAt: new Date(Date.now() - 96 * 60 * 60 * 1000).toISOString(),
      answeredAt: new Date(Date.now() - 94 * 60 * 60 * 1000).toISOString(),
    },
  ]);

  readonly unansweredCount = computed(() =>
    this.questions().filter(q => q.status === 'unanswered').length
  );

  readonly filteredQuestions = computed(() => {
    const tab = this.activeTab();
    const qs = this.questions();
    let filtered: Question[];
    if (tab === 'all') {
      filtered = qs;
    } else {
      filtered = qs.filter(q => q.status === tab);
    }
    // Unanswered sorted oldest first
    return filtered.sort((a, b) => {
      if (a.status === 'unanswered' && b.status === 'unanswered') {
        return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      }
      if (a.status === 'unanswered') return -1;
      if (b.status === 'unanswered') return 1;
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });
  });

  constructor(private toastService: ToastService) {
    // Simulate loading
    setTimeout(() => this.loading.set(false), 600);
  }

  setTab(tab: TabFilter): void {
    this.activeTab.set(tab);
    this.cancelReply();
  }

  isOld(createdAt: string): boolean {
    const hours = (Date.now() - new Date(createdAt).getTime()) / (1000 * 60 * 60);
    return hours > 24;
  }

  startReply(questionId: number): void {
    this.replyingTo.set(questionId);
    this.replyText.set('');
  }

  cancelReply(): void {
    this.replyingTo.set(null);
    this.replyText.set('');
  }

  submitReply(question: Question): void {
    const text = this.replyText().trim();
    if (!text) return;

    this.questions.update(qs =>
      qs.map(q =>
        q.id === question.id
          ? { ...q, status: 'answered' as QuestionStatus, answer: text, answeredAt: new Date().toISOString() }
          : q
      )
    );
    this.replyingTo.set(null);
    this.replyText.set('');
    this.toastService.show('Resposta enviada com sucesso!', 'success');
  }

  getStatusVariant(status: QuestionStatus): BadgeVariant {
    return status === 'answered' ? 'success' : 'accent';
  }
}
