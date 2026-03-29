import '../../../test-setup';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { ProductFormComponent } from './product-form.component';
import { ProductService } from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import { ToastService } from '../../services/toast.service';
import { FileUploadService } from '../../services/file-upload.service';

describe('ProductFormComponent', () => {
  let component: ProductFormComponent;
  let productService: any;
  let toastService: any;
  let router: Router;

  beforeEach(async () => {
    productService = {
      getById: vi.fn().mockResolvedValue({}),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      getNextSku: vi.fn().mockResolvedValue('SKU-001'),
    };

    const categoryService = {
      getTree: vi.fn().mockResolvedValue([]),
    };

    toastService = {
      show: vi.fn(),
    };

    const fileUploadService = {
      getFiles: vi.fn().mockReturnValue({ subscribe: () => {} }),
    };

    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        FormBuilder,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } },
        },
        { provide: ProductService, useValue: productService },
        { provide: CategoryService, useValue: categoryService },
        { provide: ToastService, useValue: toastService },
        { provide: FileUploadService, useValue: fileUploadService },
        ConfirmDialogService,
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    // Instantiate class directly in injection context (avoids template rendering)
    component = TestBed.runInInjectionContext(() => new ProductFormComponent(
      TestBed.inject(FormBuilder),
      TestBed.inject(ActivatedRoute),
      router,
    ));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should be in create mode when no route id', () => {
    expect(component.isEditMode()).toBe(false);
  });

  it('should have required titulo field', () => {
    component.form.get('titulo')!.setValue('');
    expect(component.form.get('titulo')!.hasError('required')).toBe(true);
  });

  it('should enforce titulo maxLength', () => {
    component.form.get('titulo')!.setValue('A'.repeat(61));
    expect(component.form.get('titulo')!.hasError('maxlength')).toBe(true);
  });

  it('should have required categoria field', () => {
    component.form.get('categoria')!.setValue('');
    expect(component.form.get('categoria')!.hasError('required')).toBe(true);
  });

  it('should have required precoVenda field', () => {
    component.form.get('precoVenda')!.setValue(null);
    expect(component.form.get('precoVenda')!.hasError('required')).toBe(true);
  });

  it('should reject negative price', () => {
    component.form.get('precoVenda')!.setValue(-5);
    expect(component.form.get('precoVenda')!.hasError('min')).toBe(true);
  });

  it('should compute estimated margin from form values', () => {
    // Set values before first read of computed (lazy evaluation)
    component.form.patchValue({
      precoVenda: 100,
      custoAquisicao: 40,
      custoEmbalagem: 10,
    });
    // First read evaluates with current form values
    expect(component.margemEstimada()).toBe(50);
  });

  it('should return null margin when price is zero', () => {
    component.form.patchValue({ precoVenda: 0 });
    // A new component instance where margemEstimada hasn't been read yet
    const freshComponent = TestBed.runInInjectionContext(() => new ProductFormComponent(
      TestBed.inject(FormBuilder),
      TestBed.inject(ActivatedRoute),
      router,
    ));
    freshComponent.form.patchValue({ precoVenda: 0 });
    expect(freshComponent.margemEstimada()).toBeNull();
  });

  it('should not save when form is invalid', async () => {
    component.form.setValue({
      sku: '', titulo: '', descricao: '', categoria: '',
      fornecedor: '', precoVenda: null, custoAquisicao: null,
      custoEmbalagem: null, custoArmazenagemDiario: null,
      estoqueMinimo: null, estoqueMaximo: null,
      peso: null, altura: null, largura: null, comprimento: null,
    });
    await component.onSave();

    expect(productService.create).not.toHaveBeenCalled();
    expect(toastService.show).toHaveBeenCalledWith(
      'Preencha os campos obrigatórios destacados',
      'warning',
    );
  });

  it('should switch to basicas tab when titulo is invalid', async () => {
    component.activeTab.set('dimensoes');
    component.form.patchValue({ titulo: '', precoVenda: 50, categoria: 'cat1' });
    await component.onSave();
    expect(component.activeTab()).toBe('basicas');
  });

  it('should switch to preco tab when precoVenda is invalid', async () => {
    component.activeTab.set('dimensoes');
    component.form.patchValue({ titulo: 'Test', categoria: 'cat1', precoVenda: null });
    await component.onSave();
    expect(component.activeTab()).toBe('preco');
  });

  it('should create product on valid form submit', async () => {
    productService.create.mockResolvedValue({ id: 'new-id' });

    component.form.patchValue({
      titulo: 'Test Product',
      categoria: 'cat-1',
      precoVenda: 99.90,
    });

    await component.onSave();

    expect(productService.create).toHaveBeenCalled();
    expect(toastService.show).toHaveBeenCalledWith('Produto salvo com sucesso', 'success');
    expect(router.navigate).toHaveBeenCalledWith(['/produtos', 'new-id']);
  });

  it('should show error toast on save failure', async () => {
    productService.create.mockRejectedValue(new Error('fail'));

    component.form.patchValue({
      titulo: 'Test Product',
      categoria: 'cat-1',
      precoVenda: 99.90,
    });

    await component.onSave();

    expect(toastService.show).toHaveBeenCalledWith('Erro ao salvar produto', 'danger');
    expect(component.loading()).toBe(false);
  });

  it('should return margin color based on value', () => {
    // Test getMargemColor with different margin values by creating fresh instances
    // >= 20 → success
    const comp1 = TestBed.runInInjectionContext(() => new ProductFormComponent(
      TestBed.inject(FormBuilder), TestBed.inject(ActivatedRoute), router,
    ));
    comp1.form.patchValue({ precoVenda: 100, custoAquisicao: 30, custoEmbalagem: 0 });
    expect(comp1.getMargemColor()).toBe('var(--success)');

    // 10-19 → warning
    const comp2 = TestBed.runInInjectionContext(() => new ProductFormComponent(
      TestBed.inject(FormBuilder), TestBed.inject(ActivatedRoute), router,
    ));
    comp2.form.patchValue({ precoVenda: 100, custoAquisicao: 85, custoEmbalagem: 0 });
    expect(comp2.getMargemColor()).toBe('var(--warning)');

    // < 10 → danger
    const comp3 = TestBed.runInInjectionContext(() => new ProductFormComponent(
      TestBed.inject(FormBuilder), TestBed.inject(ActivatedRoute), router,
    ));
    comp3.form.patchValue({ precoVenda: 100, custoAquisicao: 95, custoEmbalagem: 0 });
    expect(comp3.getMargemColor()).toBe('var(--danger)');
  });

  it('should track titulo length', () => {
    component.form.get('titulo')!.setValue('Hello');
    expect(component.tituloLength()).toBe(5);
  });

  it('should have 4 tabs defined', () => {
    expect(component.tabs.length).toBe(4);
    expect(component.tabs.map(t => t.id)).toEqual(['basicas', 'preco', 'dimensoes', 'variacoes']);
  });

  it('should set active tab', () => {
    component.setActiveTab('preco');
    expect(component.activeTab()).toBe('preco');
  });
});
