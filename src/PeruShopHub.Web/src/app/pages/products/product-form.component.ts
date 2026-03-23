import { Component, signal, computed, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Save, X, ChevronDown, ChevronUp } from 'lucide-angular';
import { MediaGalleryComponent, GalleryImage } from './media-gallery.component';
import { VariantManagerComponent } from './variant-manager.component';
import { ProductService } from '../../services/product.service';

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

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, MediaGalleryComponent, VariantManagerComponent],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent {
  private readonly productService = inject(ProductService);

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
      this.loadProduct(id);
    }
  }

  get titulo() { return this.form.get('titulo')!; }
  get categoria() { return this.form.get('categoria')!; }
  get precoVenda() { return this.form.get('precoVenda')!; }

  tituloLength(): number {
    return (this.form.get('titulo')?.value || '').length;
  }

  private async loadProduct(id: string): Promise<void> {
    this.loading.set(true);
    try {
      const product = await this.productService.getById(id);
      this.productName.set(product.name);
      this.form.patchValue({
        sku: product.sku,
        titulo: product.name,
        descricao: product.description ?? '',
        categoria: product.categoryId ?? '',
        fornecedor: product.supplier ?? '',
        precoVenda: product.price,
        custoAquisicao: product.acquisitionCost,
        peso: product.weight,
        altura: product.height,
        largura: product.width,
        comprimento: product.length,
      });
    } catch {
      // Product not found or API error — stay on form with empty values
    } finally {
      this.loading.set(false);
    }
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

  async onSave(): Promise<void> {
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
    try {
      const formValue = this.form.value;
      const dto = {
        name: formValue.titulo,
        sku: formValue.sku || undefined,
        description: formValue.descricao || undefined,
        categoryId: formValue.categoria || undefined,
        supplier: formValue.fornecedor || undefined,
        price: formValue.precoVenda,
        acquisitionCost: formValue.custoAquisicao ?? undefined,
        weight: formValue.peso ?? undefined,
        height: formValue.altura ?? undefined,
        width: formValue.largura ?? undefined,
        length: formValue.comprimento ?? undefined,
      };

      if (this.isEditMode()) {
        await this.productService.update(this.productId(), dto);
      } else {
        const created = await this.productService.create(dto);
        this.router.navigate(['/produtos', created.id, 'editar']);
      }
      this.form.markAsPristine();
    } catch {
      // Error handling — toast would be shown here
    } finally {
      this.loading.set(false);
    }
  }

  canDeactivate(): boolean {
    if (!this.form.dirty) return true;
    return confirm('Você tem alterações não salvas. Deseja sair?');
  }
}
