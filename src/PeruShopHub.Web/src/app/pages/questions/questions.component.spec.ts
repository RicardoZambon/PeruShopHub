import '../../../test-setup';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError, Subject } from 'rxjs';
import { QuestionsComponent } from './questions.component';
import { QuestionService, type QuestionListItem, type QuestionListResponse } from '../../services/question.service';
import { ResponseTemplateService, type ResponseTemplate } from '../../services/response-template.service';
import { SignalRService, type DataChangeEvent } from '../../services/signalr.service';
import { ToastService } from '../../services/toast.service';

const makeQuestion = (overrides: Partial<QuestionListItem> = {}): QuestionListItem => ({
  id: 'q-1',
  externalId: 'ext-1',
  externalItemId: 'MLB-123',
  productId: 'p-1',
  buyerName: 'João',
  questionText: 'Tem disponível?',
  answerText: null,
  status: 'UNANSWERED',
  questionDate: '2026-03-28T10:00:00Z',
  answerDate: null,
  ...overrides,
});

const mockQuestions: QuestionListItem[] = [
  makeQuestion({ id: 'q-1', buyerName: 'João', status: 'UNANSWERED', questionDate: '2026-03-28T10:00:00Z' }),
  makeQuestion({ id: 'q-2', buyerName: 'Maria', status: 'ANSWERED', answerText: 'Sim!', questionDate: '2026-03-27T10:00:00Z' }),
  makeQuestion({ id: 'q-3', buyerName: 'Pedro', status: 'UNANSWERED', questionDate: '2026-03-26T10:00:00Z' }),
];

const mockResponse: QuestionListResponse = {
  items: mockQuestions,
  totalCount: 3,
  page: 1,
  pageSize: 100,
};

const mockTemplates: ResponseTemplate[] = [
  { id: 't-1', name: 'Disponibilidade', category: 'geral', body: 'Sim, está disponível!', placeholders: null, usageCount: 5, order: 1, isActive: true },
  { id: 't-2', name: 'Inativo', category: 'geral', body: 'Não disponível', placeholders: null, usageCount: 0, order: 2, isActive: false },
];

describe('QuestionsComponent', () => {
  let component: QuestionsComponent;
  let fixture: ComponentFixture<QuestionsComponent>;
  let questionService: any;
  let templateService: any;
  let toastService: any;
  let dataChanged$: Subject<DataChangeEvent>;

  beforeEach(async () => {
    dataChanged$ = new Subject<DataChangeEvent>();

    questionService = {
      list: vi.fn().mockReturnValue(of(mockResponse)),
      answer: vi.fn(),
    };

    templateService = {
      list: vi.fn().mockReturnValue(of(mockTemplates)),
      incrementUsage: vi.fn().mockReturnValue(of(undefined)),
    };

    toastService = {
      show: vi.fn(),
    };

    const signalRService = {
      dataChanged$: dataChanged$.asObservable(),
    };

    await TestBed.configureTestingModule({
      imports: [QuestionsComponent],
      providers: [
        provideHttpClient(),
        { provide: QuestionService, useValue: questionService },
        { provide: ResponseTemplateService, useValue: templateService },
        { provide: SignalRService, useValue: signalRService },
        { provide: ToastService, useValue: toastService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(QuestionsComponent);
    component = fixture.componentInstance;
    component.ngOnInit();
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load questions on init', () => {
    expect(questionService.list).toHaveBeenCalled();
    expect(component.questions().length).toBe(3);
  });

  it('should load only active templates', () => {
    expect(component.responseTemplates().length).toBe(1);
    expect(component.responseTemplates()[0].name).toBe('Disponibilidade');
  });

  it('should count unanswered questions', () => {
    expect(component.unansweredCount()).toBe(2);
  });

  it('should filter by unanswered tab', () => {
    component.setTab('unanswered');
    const filtered = component.filteredQuestions();
    expect(filtered.every(q => q.status === 'UNANSWERED')).toBe(true);
    expect(filtered.length).toBe(2);
  });

  it('should filter by answered tab', () => {
    component.setTab('answered');
    const filtered = component.filteredQuestions();
    expect(filtered.every(q => q.status === 'ANSWERED')).toBe(true);
    expect(filtered.length).toBe(1);
  });

  it('should show all on all tab', () => {
    component.setTab('all');
    expect(component.filteredQuestions().length).toBe(3);
  });

  it('should sort unanswered oldest first', () => {
    component.setTab('unanswered');
    const filtered = component.filteredQuestions();
    // Pedro (26th) should come before João (28th)
    expect(filtered[0].buyerName).toBe('Pedro');
    expect(filtered[1].buyerName).toBe('João');
  });

  it('should filter by search query', () => {
    component.searchQuery.set('Maria');
    component.setTab('all');
    expect(component.filteredQuestions().length).toBe(1);
    expect(component.filteredQuestions()[0].buyerName).toBe('Maria');
  });

  it('should start reply', () => {
    component.startReply('q-1');
    expect(component.replyingTo()).toBe('q-1');
    expect(component.replyText()).toBe('');
  });

  it('should cancel reply', () => {
    component.startReply('q-1');
    component.replyText.set('Draft answer');
    component.cancelReply();

    expect(component.replyingTo()).toBeNull();
    expect(component.replyText()).toBe('');
  });

  it('should apply template to reply text', () => {
    component.startReply('q-1');
    component.applyTemplate(mockTemplates[0]);

    expect(component.replyText()).toBe('Sim, está disponível!');
    expect(component.showTemplateDropdown()).toBe(false);
    expect(templateService.incrementUsage).toHaveBeenCalledWith('t-1');
  });

  it('should toggle template dropdown', () => {
    expect(component.showTemplateDropdown()).toBe(false);
    component.toggleTemplateDropdown();
    expect(component.showTemplateDropdown()).toBe(true);
    component.toggleTemplateDropdown();
    expect(component.showTemplateDropdown()).toBe(false);
  });

  it('should submit reply successfully', () => {
    const answeredQ = makeQuestion({ id: 'q-1', status: 'ANSWERED', answerText: 'Yes!' });
    questionService.answer.mockReturnValue(of(answeredQ));

    component.startReply('q-1');
    component.replyText.set('Yes!');
    component.submitReply(mockQuestions[0]);

    expect(questionService.answer).toHaveBeenCalledWith('q-1', 'Yes!');
    expect(component.replyingTo()).toBeNull();
    expect(toastService.show).toHaveBeenCalledWith('Resposta enviada com sucesso!', 'success');
  });

  it('should show error on failed reply', () => {
    questionService.answer.mockReturnValue(throwError(() => new Error('fail')));

    component.startReply('q-1');
    component.replyText.set('Answer');
    component.submitReply(mockQuestions[0]);

    expect(component.submitting()).toBe(false);
    expect(toastService.show).toHaveBeenCalledWith('Erro ao enviar resposta. Tente novamente.', 'danger');
  });

  it('should not submit empty reply', () => {
    component.startReply('q-1');
    component.replyText.set('   ');
    component.submitReply(mockQuestions[0]);

    expect(questionService.answer).not.toHaveBeenCalled();
  });

  it('should not submit while already submitting', () => {
    component.submitting.set(true);
    component.replyText.set('Answer');
    component.submitReply(mockQuestions[0]);

    expect(questionService.answer).not.toHaveBeenCalled();
  });

  it('should reload questions on SignalR event', () => {
    vi.clearAllMocks();
    questionService.list.mockReturnValue(of(mockResponse));

    dataChanged$.next({ entity: 'question', action: 'created' });

    expect(questionService.list).toHaveBeenCalled();
  });

  it('should detect old questions (>24h)', () => {
    const oldDate = new Date(Date.now() - 25 * 60 * 60 * 1000).toISOString();
    const recentDate = new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString();

    expect(component.isOld(oldDate)).toBe(true);
    expect(component.isOld(recentDate)).toBe(false);
  });

  it('should return correct status variant', () => {
    expect(component.getStatusVariant('ANSWERED')).toBe('success');
    expect(component.getStatusVariant('UNANSWERED')).toBe('accent');
  });

  it('should return correct status label', () => {
    expect(component.getStatusLabel('ANSWERED')).toBe('Respondida');
    expect(component.getStatusLabel('UNANSWERED')).toBe('Pendente');
  });

  it('should cancel reply when switching tabs', () => {
    component.startReply('q-1');
    component.replyText.set('Draft');

    component.setTab('answered');

    expect(component.replyingTo()).toBeNull();
    expect(component.activeTab()).toBe('answered');
  });
});
