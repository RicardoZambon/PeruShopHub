import { Component, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Save, Send, X, ChevronDown, ChevronUp } from 'lucide-angular';

type TabId = 'basicas' | 'preco' | 'envio';

interface Tab {
  id: TabId;
  label: string;
}

const TABS: Tab[] = [
  { id: 'basicas', label: 'Informações Básicas' },
  { id: 'preco', label: 'Preço e Custos' },
  { id: 'envio', label: 'Envio' },
];

const CATEGORIAS = [
  'Eletrônicos', 'Celulares e Telefones', 'Informática', 'Games',
  'Áudio', 'TV e Vídeo', 'Câmeras', 'Acessórios para Veículos',
  'Casa e Decoração', 'Esportes', 'Moda', 'Beleza e Saúde',
];

const TIPOS_ANUNCIO = [
  { value: 'gratis', label: 'Grátis (sem comissão, menor visibilidade)' },
  { value: 'classico', label: 'Clássico (comissão padrão)' },
  { value: 'premium', label: 'Premium (maior comissão, máxima visibilidade)' },
];

const COMISSAO_MAP: Record<string, number> = {
  gratis: 0,
  classico: 0.11,
  premium: 0.16,
};

// Mock product data for edit mode
const MOCK_PRODUCT = {
  titulo: 'Fone Bluetooth TWS Pro Max',
  descricao: 'Fone de ouvido bluetooth sem fio com cancelamento de ruído ativo, driver de 13mm, autonomia de 30h com estojo de carga.',
  categoria: 'Áudio',
  condicao: 'novo',
  precoVenda: 189.90,
  custoAquisicao: 62.00,
  custoEmbalagem: 3.50,
  fornecedor: 'ShenzhenTech Imports',
  tipoAnuncio: 'classico',
  peso: 0.25,
  altura: 8,
  largura: 12,
  comprimento: 15,
  freteGratis: true,
};

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent {
  readonly arrowLeftIcon = ArrowLeft;
  readonly saveIcon = Save;
  readonly sendIcon = Send;
  readonly closeIcon = X;
  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;

  readonly tabs = TABS;
  readonly categorias = CATEGORIAS;
  readonly tiposAnuncio = TIPOS_ANUNCIO;

  activeTab = signal<TabId>('basicas');
  isEditMode = signal(false);
  productName = signal('');
  loading = signal(false);
  categoriaDropdownOpen = signal(false);
  categoriaFilter = signal('');

  // Mobile accordion: track which tabs are open
  openAccordions = signal<Set<TabId>>(new Set(['basicas']));

  form: FormGroup;

  filteredCategorias = computed(() => {
    const filter = this.categoriaFilter().toLowerCase();
    if (!filter) return this.categorias;
    return this.categorias.filter(c => c.toLowerCase().includes(filter));
  });

  margemEstimada = computed(() => {
    const preco = this.form?.get('precoVenda')?.value || 0;
    const custoAquisicao = this.form?.get('custoAquisicao')?.value || 0;
    const custoEmbalagem = this.form?.get('custoEmbalagem')?.value || 0;
    const tipoAnuncio = this.form?.get('tipoAnuncio')?.value || 'classico';

    if (preco <= 0) return null;

    const comissao = preco * (COMISSAO_MAP[tipoAnuncio] || 0);
    const custoTotal = custoAquisicao + custoEmbalagem + comissao;
    const lucro = preco - custoTotal;
    return (lucro / preco) * 100;
  });

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
  ) {
    this.form = this.fb.group({
      titulo: ['', [Validators.required, Validators.maxLength(60)]],
      descricao: [''],
      categoria: ['', [Validators.required]],
      condicao: ['novo', [Validators.required]],
      precoVenda: [null as number | null, [Validators.required, Validators.min(0.01)]],
      custoAquisicao: [null as number | null, [Validators.min(0)]],
      custoEmbalagem: [null as number | null, [Validators.min(0)]],
      fornecedor: [''],
      tipoAnuncio: ['classico', [Validators.required]],
      peso: [null as number | null, [Validators.min(0.01)]],
      altura: [null as number | null, [Validators.min(1)]],
      largura: [null as number | null, [Validators.min(1)]],
      comprimento: [null as number | null, [Validators.min(1)]],
      freteGratis: [false],
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.loadProduct();
    }
  }

  get titulo() { return this.form.get('titulo')!; }
  get categoria() { return this.form.get('categoria')!; }
  get precoVenda() { return this.form.get('precoVenda')!; }

  tituloLength(): number {
    return (this.form.get('titulo')?.value || '').length;
  }

  private loadProduct(): void {
    this.productName.set(MOCK_PRODUCT.titulo);
    this.form.patchValue({
      titulo: MOCK_PRODUCT.titulo,
      descricao: MOCK_PRODUCT.descricao,
      categoria: MOCK_PRODUCT.categoria,
      condicao: MOCK_PRODUCT.condicao,
      precoVenda: MOCK_PRODUCT.precoVenda,
      custoAquisicao: MOCK_PRODUCT.custoAquisicao,
      custoEmbalagem: MOCK_PRODUCT.custoEmbalagem,
      fornecedor: MOCK_PRODUCT.fornecedor,
      tipoAnuncio: MOCK_PRODUCT.tipoAnuncio,
      peso: MOCK_PRODUCT.peso,
      altura: MOCK_PRODUCT.altura,
      largura: MOCK_PRODUCT.largura,
      comprimento: MOCK_PRODUCT.comprimento,
      freteGratis: MOCK_PRODUCT.freteGratis,
    });
  }

  setActiveTab(tabId: TabId): void {
    this.activeTab.set(tabId);
  }

  toggleAccordion(tabId: TabId): void {
    const current = new Set(this.openAccordions());
    if (current.has(tabId)) {
      current.delete(tabId);
    } else {
      current.add(tabId);
    }
    this.openAccordions.set(current);
  }

  isAccordionOpen(tabId: TabId): boolean {
    return this.openAccordions().has(tabId);
  }

  selectCategoria(cat: string): void {
    this.form.patchValue({ categoria: cat });
    this.categoriaDropdownOpen.set(false);
    this.categoriaFilter.set('');
  }

  onCategoriaInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.categoriaFilter.set(value);
    if (!this.categoriaDropdownOpen()) {
      this.categoriaDropdownOpen.set(true);
    }
  }

  toggleCategoriaDropdown(): void {
    this.categoriaDropdownOpen.set(!this.categoriaDropdownOpen());
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.categoria-dropdown')) {
      this.categoriaDropdownOpen.set(false);
    }
  }

  getMargemColor(): string {
    const margem = this.margemEstimada();
    if (margem === null) return 'var(--neutral-500)';
    if (margem >= 20) return 'var(--success)';
    if (margem >= 10) return 'var(--warning)';
    return 'var(--danger)';
  }

  onCancel(): void {
    if (this.form.dirty && !confirm('Você tem alterações não salvas. Deseja sair?')) {
      return;
    }
    this.router.navigate(['/produtos']);
  }

  onSaveDraft(): void {
    this.loading.set(true);
    setTimeout(() => {
      this.loading.set(false);
      this.form.markAsPristine();
      // Toast would be shown here
    }, 800);
  }

  onPublish(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      // Switch to tab with first error
      if (this.titulo.invalid || this.categoria.invalid) {
        this.activeTab.set('basicas');
      } else if (this.precoVenda.invalid) {
        this.activeTab.set('preco');
      }
      return;
    }

    this.loading.set(true);
    setTimeout(() => {
      this.loading.set(false);
      this.form.markAsPristine();
      this.router.navigate(['/produtos']);
    }, 1000);
  }

  canDeactivate(): boolean {
    if (!this.form.dirty) return true;
    return confirm('Você tem alterações não salvas. Deseja sair?');
  }
}
