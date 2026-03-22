import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Save, Send, X, ChevronDown, ChevronUp } from 'lucide-angular';
import { TreeSelectComponent } from './tree-select.component';
import { VariantManagerComponent } from './variant-manager.component';
import { CategoryService } from '../../services/category.service';
import { ProductVariantService } from '../../services/product-variant.service';
import { BrlCurrencyPipe } from '../../shared/pipes/brl-currency.pipe';

type TabId = 'basicas' | 'preco' | 'envio' | 'variacoes';

interface Tab {
  id: TabId;
  label: string;
}

const TABS: Tab[] = [
  { id: 'basicas', label: 'Informações Básicas' },
  { id: 'preco', label: 'Preço e Custos' },
  { id: 'envio', label: 'Envio' },
  { id: 'variacoes', label: 'Variações' },
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
  sku: 'FN-BT-001',
  descricao: 'Fone de ouvido bluetooth sem fio com cancelamento de ruído ativo, driver de 13mm, autonomia de 30h com estojo de carga.',
  categoria: 'cat-fones',
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
  selector: 'app-produto-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, TreeSelectComponent, VariantManagerComponent, BrlCurrencyPipe],
  templateUrl: './produto-form.component.html',
  styleUrl: './produto-form.component.scss',
})
export class ProdutoFormComponent {
  readonly arrowLeftIcon = ArrowLeft;
  readonly saveIcon = Save;
  readonly sendIcon = Send;
  readonly closeIcon = X;
  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;

  readonly tabs = TABS;
  readonly tiposAnuncio = TIPOS_ANUNCIO;

  activeTab = signal<TabId>('basicas');
  isEditMode = signal(false);
  productId = signal('');
  productName = signal('');
  loading = signal(false);
  showCategoryChangeDialog = signal(false);
  pendingCategoryId = signal<string | null>(null);
  previousCategoryId = signal<string | null>(null);

  // Mobile accordion: track which tabs are open
  openAccordions = signal<Set<TabId>>(new Set(['basicas']));

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    public categoryService: CategoryService,
    private productVariantService: ProductVariantService,
  ) {
    this.form = this.fb.group({
      titulo: ['', [Validators.required, Validators.maxLength(60)]],
      sku: ['', [Validators.required]],
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
      this.productId.set(id);
      this.loadProduct();
    }

    // Check for tab query param
    const tabParam = this.route.snapshot.queryParamMap.get('tab');
    if (tabParam && TABS.some(t => t.id === tabParam)) {
      this.activeTab.set(tabParam as TabId);
    }
  }

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

  get titulo() { return this.form.get('titulo')!; }
  get sku() { return this.form.get('sku')!; }
  get categoria() { return this.form.get('categoria')!; }
  get precoVenda() { return this.form.get('precoVenda')!; }

  tituloLength(): number {
    return (this.form.get('titulo')?.value || '').length;
  }

  private loadProduct(): void {
    this.productName.set(MOCK_PRODUCT.titulo);
    this.form.patchValue({
      titulo: MOCK_PRODUCT.titulo,
      sku: MOCK_PRODUCT.sku,
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
    this.previousCategoryId.set(MOCK_PRODUCT.categoria);
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

  onCategoryChange(categoryId: string): void {
    const currentCategoryId = this.form.get('categoria')?.value;
    const hasVariants = this.productId()
      ? this.productVariantService.getByProductId(this.productId()).length > 0
      : false;

    if (hasVariants && currentCategoryId && currentCategoryId !== categoryId) {
      // Check if the new category has different variation fields
      const oldFields = this.categoryService.getAllVariationFieldsForCategory(currentCategoryId);
      const newFields = this.categoryService.getAllVariationFieldsForCategory(categoryId);
      const oldFieldNames = new Set(oldFields.map(f => f.name));
      const newFieldNames = new Set(newFields.map(f => f.name));
      const hasDifference = oldFields.some(f => !newFieldNames.has(f.name)) ||
                            newFields.some(f => !oldFieldNames.has(f.name));

      if (hasDifference) {
        this.pendingCategoryId.set(categoryId);
        this.showCategoryChangeDialog.set(true);
        return;
      }
    }

    this.applyCategoryChange(categoryId);
  }

  confirmCategoryChange(): void {
    const newId = this.pendingCategoryId();
    if (newId) {
      // Clear variants for this product
      if (this.productId()) {
        this.productVariantService.deleteByProductId(this.productId());
      }
      this.applyCategoryChange(newId);
    }
    this.showCategoryChangeDialog.set(false);
    this.pendingCategoryId.set(null);
  }

  cancelCategoryChange(): void {
    this.showCategoryChangeDialog.set(false);
    this.pendingCategoryId.set(null);
  }

  private applyCategoryChange(categoryId: string): void {
    this.form.patchValue({ categoria: categoryId });
    this.previousCategoryId.set(categoryId);
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
      // Clear review flags on save
      if (this.productId()) {
        this.productVariantService.clearReviewFlag(this.productId());
      }
    }, 800);
  }

  onPublish(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (this.titulo.invalid || this.sku.invalid || this.categoria.invalid) {
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
      // Clear review flags on save
      if (this.productId()) {
        this.productVariantService.clearReviewFlag(this.productId());
      }
      this.router.navigate(['/produtos']);
    }, 1000);
  }

  canDeactivate(): boolean {
    if (!this.form.dirty) return true;
    return confirm('Você tem alterações não salvas. Deseja sair?');
  }
}
