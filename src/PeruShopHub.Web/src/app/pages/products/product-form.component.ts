import { Component, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Save, X, ChevronDown, ChevronUp } from 'lucide-angular';
import { MediaGalleryComponent, GalleryImage } from './media-gallery.component';
import { VariantManagerComponent } from './variant-manager.component';

type TabId = 'basicas' | 'preco' | 'dimensoes' | 'variacoes';

interface Tab {
  id: TabId;
  label: string;
}

const TABS: Tab[] = [
  { id: 'basicas', label: 'Informações Básicas' },
  { id: 'preco', label: 'Preço e Custos' },
  { id: 'dimensoes', label: 'Dimensões' },
  { id: 'variacoes', label: 'Variações' },
];

const CATEGORIAS = [
  'Eletrônicos', 'Celulares e Telefones', 'Informática', 'Games',
  'Áudio', 'TV e Vídeo', 'Câmeras', 'Acessórios para Veículos',
  'Casa e Decoração', 'Esportes', 'Moda', 'Beleza e Saúde',
];

// Mock product data for edit mode
const MOCK_PRODUCT = {
  sku: 'TWS-PRO-001',
  titulo: 'Fone Bluetooth TWS Pro Max',
  descricao: 'Fone de ouvido bluetooth sem fio com cancelamento de ruído ativo, driver de 13mm, autonomia de 30h com estojo de carga.',
  categoria: 'Áudio',
  fornecedor: 'ShenzhenTech Imports',
  precoVenda: 189.90,
  custoAquisicao: 62.00,
  peso: 0.25,
  altura: 8,
  largura: 12,
  comprimento: 15,
};

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, MediaGalleryComponent, VariantManagerComponent],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent {
  readonly arrowLeftIcon = ArrowLeft;
  readonly saveIcon = Save;
  readonly closeIcon = X;
  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;

  readonly tabs = TABS;
  readonly categorias = CATEGORIAS;

  activeTab = signal<TabId>('basicas');
  isEditMode = signal(false);
  productName = signal('');
  productId = signal('');
  loading = signal(false);
  categoriaDropdownOpen = signal(false);
  categoriaFilter = signal('');

  galleryImages = signal<GalleryImage[]>([]);
  galleryVideoUrl = signal<string | null>(null);

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

    if (preco <= 0) return null;

    return ((preco - custoAquisicao) / preco) * 100;
  });

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
  ) {
    this.form = this.fb.group({
      sku: [''],
      titulo: ['', [Validators.required, Validators.maxLength(60)]],
      descricao: [''],
      categoria: ['', [Validators.required]],
      fornecedor: [''],
      precoVenda: [null as number | null, [Validators.required, Validators.min(0.01)]],
      custoAquisicao: [null as number | null, [Validators.min(0)]],
      peso: [null as number | null, [Validators.min(0.01)]],
      altura: [null as number | null, [Validators.min(1)]],
      largura: [null as number | null, [Validators.min(1)]],
      comprimento: [null as number | null, [Validators.min(1)]],
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.productId.set(id);
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
      sku: MOCK_PRODUCT.sku,
      titulo: MOCK_PRODUCT.titulo,
      descricao: MOCK_PRODUCT.descricao,
      categoria: MOCK_PRODUCT.categoria,
      fornecedor: MOCK_PRODUCT.fornecedor,
      precoVenda: MOCK_PRODUCT.precoVenda,
      custoAquisicao: MOCK_PRODUCT.custoAquisicao,
      peso: MOCK_PRODUCT.peso,
      altura: MOCK_PRODUCT.altura,
      largura: MOCK_PRODUCT.largura,
      comprimento: MOCK_PRODUCT.comprimento,
    });

    this.galleryImages.set([
      { id: '1', color: '#5C6BC0', order: 0 },
      { id: '2', color: '#42A5F5', order: 1 },
      { id: '3', color: '#66BB6A', order: 2 },
    ]);
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

  onGalleryImagesChange(images: GalleryImage[]): void {
    this.galleryImages.set(images);
  }

  onGalleryVideoChange(url: string | null): void {
    this.galleryVideoUrl.set(url);
  }

  onCancel(): void {
    if (this.form.dirty && !confirm('Você tem alterações não salvas. Deseja sair?')) {
      return;
    }
    this.router.navigate(['/produtos']);
  }

  onSave(): void {
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
      // Toast would be shown here
    }, 800);
  }

  canDeactivate(): boolean {
    if (!this.form.dirty) return true;
    return confirm('Você tem alterações não salvas. Deseja sair?');
  }
}
