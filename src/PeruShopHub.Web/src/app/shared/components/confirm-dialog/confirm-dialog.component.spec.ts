import '../../../../test-setup';
import { TestBed } from '@angular/core/testing';
import { ConfirmDialogService, type ConfirmOptions } from './confirm-dialog.service';

describe('ConfirmDialogService', () => {
  let service: ConfirmDialogService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ConfirmDialogService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should open dialog with string message', () => {
    service.confirm('Are you sure?');

    expect(service.open()).toBe(true);
    expect(service.options().message).toBe('Are you sure?');
    expect(service.options().title).toBe('Confirmar');
    expect(service.options().variant).toBe('danger');
  });

  it('should open dialog with full options', () => {
    const opts: ConfirmOptions = {
      title: 'Excluir',
      message: 'Deseja excluir?',
      confirmLabel: 'Sim',
      cancelLabel: 'Não',
      variant: 'warning',
    };
    service.confirm(opts);

    expect(service.open()).toBe(true);
    expect(service.options().title).toBe('Excluir');
    expect(service.options().confirmLabel).toBe('Sim');
    expect(service.options().cancelLabel).toBe('Não');
    expect(service.options().variant).toBe('warning');
  });

  it('should resolve true on accept', async () => {
    const promise = service.confirm('Test');
    service.accept();

    expect(service.processing()).toBe(true);
    await expect(promise).resolves.toBe(true);
  });

  it('should resolve false on cancel', async () => {
    const promise = service.confirm('Test');
    service.cancel();

    expect(service.open()).toBe(false);
    expect(service.processing()).toBe(false);
    await expect(promise).resolves.toBe(false);
  });

  it('should not cancel while processing', async () => {
    const promise = service.confirm('Test');
    service.accept();

    expect(service.processing()).toBe(true);

    // Try to cancel while processing — should be blocked
    service.cancel();
    expect(service.processing()).toBe(true);
    // Dialog should still be open
    expect(service.open()).toBe(true);

    await promise; // resolve to avoid unhandled promise
  });

  it('should close dialog on done', async () => {
    service.confirm('Test');
    service.accept();
    service.done();

    expect(service.open()).toBe(false);
    expect(service.processing()).toBe(false);
  });

  it('should reset processing state on new confirm', async () => {
    const p1 = service.confirm('First');
    service.accept();
    service.done();

    const p2 = service.confirm('Second');
    expect(service.processing()).toBe(false);
    expect(service.open()).toBe(true);

    service.cancel();
    await p1;
    await p2;
  });
});
