import '../../../../test-setup';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DialogComponent } from './dialog.component';

describe('DialogComponent', () => {
  let component: DialogComponent;
  let fixture: ComponentFixture<DialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DialogComponent);
    component = fixture.componentInstance;
    component.title = 'Test Dialog';
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not render overlay when closed', () => {
    component.open = false;
    fixture.detectChanges();

    const overlay = fixture.debugElement.query(By.css('.dialog-overlay'));
    expect(overlay).toBeFalsy();
  });

  it('should render overlay and title when open', () => {
    component.open = true;
    fixture.detectChanges();

    const overlay = fixture.debugElement.query(By.css('.dialog-overlay'));
    expect(overlay).toBeTruthy();

    const title = fixture.debugElement.query(By.css('.dialog__title'));
    expect(title.nativeElement.textContent).toContain('Test Dialog');
  });

  it('should apply size class', () => {
    component.open = true;
    component.size = 'lg';
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('.dialog'));
    expect(dialog.nativeElement.classList).toContain('dialog--lg');
  });

  it('should emit closed when close button is clicked', () => {
    component.open = true;
    fixture.detectChanges();

    const spy = vi.fn();
    component.closed.subscribe(spy);

    const closeBtn = fixture.debugElement.query(By.css('.dialog__close'));
    closeBtn.triggerEventHandler('click', null);

    expect(spy).toHaveBeenCalled();
  });

  it('should emit closed on Escape key', () => {
    component.open = true;
    fixture.detectChanges();

    const spy = vi.fn();
    component.closed.subscribe(spy);

    component.onEscape();

    expect(spy).toHaveBeenCalled();
  });

  it('should NOT emit closed on Escape when not open', () => {
    component.open = false;
    fixture.detectChanges();

    const spy = vi.fn();
    component.closed.subscribe(spy);

    component.onEscape();

    expect(spy).not.toHaveBeenCalled();
  });

  it('should emit closed on backdrop click', () => {
    component.open = true;
    fixture.detectChanges();

    const spy = vi.fn();
    component.closed.subscribe(spy);

    const overlayEl = document.createElement('div');
    overlayEl.classList.add('dialog-overlay');
    component.onBackdropClick({ target: overlayEl } as unknown as MouseEvent);

    expect(spy).toHaveBeenCalled();
  });

  it('should NOT emit closed when clicking inside dialog', () => {
    component.open = true;
    fixture.detectChanges();

    const spy = vi.fn();
    component.closed.subscribe(spy);

    const dialogEl = document.createElement('div');
    dialogEl.classList.add('dialog');
    component.onBackdropClick({ target: dialogEl } as unknown as MouseEvent);

    expect(spy).not.toHaveBeenCalled();
  });

  it('should have aria-modal and role attributes', () => {
    component.open = true;
    fixture.detectChanges();

    const dialog = fixture.debugElement.query(By.css('[role="dialog"]'));
    expect(dialog).toBeTruthy();
    expect(dialog.attributes['aria-modal']).toBe('true');
  });
});
