import '../../../../test-setup';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FormFieldComponent } from './form-field.component';

describe('FormFieldComponent', () => {
  let component: FormFieldComponent;
  let fixture: ComponentFixture<FormFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormFieldComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FormFieldComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('label', () => {
    it('should render label when provided', () => {
      component.label = 'Nome do produto';
      fixture.detectChanges();

      const label = fixture.debugElement.query(By.css('.form-field__label'));
      expect(label).toBeTruthy();
      expect(label.nativeElement.textContent).toContain('Nome do produto');
    });

    it('should not render label when not provided', () => {
      fixture.detectChanges();

      const label = fixture.debugElement.query(By.css('.form-field__label'));
      expect(label).toBeFalsy();
    });

    it('should show required asterisk when required is true', () => {
      component.label = 'Email';
      component.required = true;
      fixture.detectChanges();

      const asterisk = fixture.debugElement.query(By.css('.form-field__required'));
      expect(asterisk).toBeTruthy();
      expect(asterisk.nativeElement.textContent).toContain('*');
    });

    it('should not show required asterisk when required is false', () => {
      component.label = 'Email';
      component.required = false;
      fixture.detectChanges();

      const asterisk = fixture.debugElement.query(By.css('.form-field__required'));
      expect(asterisk).toBeFalsy();
    });
  });

  describe('hint', () => {
    it('should show hint when provided and no error', () => {
      component.hint = 'Máximo 100 caracteres';
      fixture.detectChanges();

      const hint = fixture.debugElement.query(By.css('.form-field__hint'));
      expect(hint).toBeTruthy();
      expect(hint.nativeElement.textContent).toContain('Máximo 100 caracteres');
    });

    it('should hide hint when error is present', () => {
      component.hint = 'Some hint';
      component.error = 'Field is required';
      fixture.detectChanges();

      const hint = fixture.debugElement.query(By.css('.form-field__hint'));
      expect(hint).toBeFalsy();
    });
  });

  describe('error', () => {
    it('should show error message when error is set', () => {
      component.error = 'Nome é obrigatório';
      fixture.detectChanges();

      const error = fixture.debugElement.query(By.css('.form-field__error'));
      expect(error).toBeTruthy();
      expect(error.nativeElement.textContent).toContain('Nome é obrigatório');
    });

    it('should not show error when no error', () => {
      fixture.detectChanges();

      const error = fixture.debugElement.query(By.css('.form-field__error'));
      expect(error).toBeFalsy();
    });

    it('should replace hint with error when both are set', () => {
      component.hint = 'Helper text';
      component.error = 'Validation error';
      fixture.detectChanges();

      const hint = fixture.debugElement.query(By.css('.form-field__hint'));
      const error = fixture.debugElement.query(By.css('.form-field__error'));
      expect(hint).toBeFalsy();
      expect(error).toBeTruthy();
      expect(error.nativeElement.textContent).toContain('Validation error');
    });
  });

  describe('content projection', () => {
    it('should have a control wrapper for projected content', () => {
      fixture.detectChanges();

      const control = fixture.debugElement.query(By.css('.form-field__control'));
      expect(control).toBeTruthy();
    });
  });
});
