import '../../test-setup';
import { TestBed } from '@angular/core/testing';
import { ToastService, type ToastType } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should add a toast', () => {
    service.show('Hello', 'success');
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].message).toBe('Hello');
    expect(service.toasts()[0].type).toBe('success');
  });

  it('should default to info type', () => {
    service.show('Info message');
    expect(service.toasts()[0].type).toBe('info');
  });

  it('should include description', () => {
    service.show('Title', 'warning', 5000, 'Details here');
    expect(service.toasts()[0].description).toBe('Details here');
  });

  it('should auto-dismiss after duration', () => {
    service.show('Temp', 'success', 3000);
    expect(service.toasts().length).toBe(1);

    vi.advanceTimersByTime(3000);
    expect(service.toasts().length).toBe(0);
  });

  it('should dismiss a toast manually', () => {
    service.show('First', 'info');
    service.show('Second', 'info');

    const firstId = service.toasts()[0].id;
    service.dismiss(firstId);

    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].message).toBe('Second');
  });

  it('should keep max 3 toasts visible', () => {
    service.show('One', 'info');
    service.show('Two', 'info');
    service.show('Three', 'info');
    service.show('Four', 'info');

    expect(service.toasts().length).toBe(3);
    expect(service.toasts()[0].message).toBe('Two');
    expect(service.toasts()[2].message).toBe('Four');
  });

  it('should assign unique IDs', () => {
    service.show('A', 'info');
    service.show('B', 'info');

    const ids = service.toasts().map(t => t.id);
    expect(new Set(ids).size).toBe(2);
  });

  it('should not auto-dismiss if duration is 0', () => {
    service.show('Persistent', 'info', 0);
    vi.advanceTimersByTime(60000);
    expect(service.toasts().length).toBe(1);
  });

  it('should support all toast types', () => {
    const types: ToastType[] = ['success', 'warning', 'danger', 'info'];
    types.forEach(type => {
      service.show(`Toast ${type}`, type);
    });

    // Max 3 visible, so 4th push causes oldest to be removed
    expect(service.toasts().length).toBe(3);
  });
});
