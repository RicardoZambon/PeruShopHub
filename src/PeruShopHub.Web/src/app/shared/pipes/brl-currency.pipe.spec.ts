import { BrlCurrencyPipe } from './brl-currency.pipe';

describe('BrlCurrencyPipe', () => {
  let pipe: BrlCurrencyPipe;

  beforeEach(() => {
    pipe = new BrlCurrencyPipe();
  });

  it('should create', () => {
    expect(pipe).toBeTruthy();
  });

  describe('full mode (default)', () => {
    it('should format positive value as BRL currency', () => {
      const result = pipe.transform(1234.56);
      // pt-BR format: R$ 1.234,56 (with possible non-breaking space)
      expect(result).toContain('R$');
      expect(result).toContain('1.234');
      expect(result).toContain('56');
    });

    it('should format zero as BRL currency', () => {
      const result = pipe.transform(0);
      expect(result).toContain('R$');
      expect(result).toContain('0,00');
    });

    it('should format negative value as BRL currency', () => {
      const result = pipe.transform(-500.99);
      expect(result).toContain('R$');
      expect(result).toContain('500');
      expect(result).toContain('99');
    });

    it('should return empty string for null', () => {
      expect(pipe.transform(null)).toBe('');
    });

    it('should return empty string for undefined', () => {
      expect(pipe.transform(undefined)).toBe('');
    });

    it('should format with exactly 2 decimal places', () => {
      const result = pipe.transform(10);
      expect(result).toContain('10,00');
    });
  });

  describe('compact mode', () => {
    it('should format millions with M suffix', () => {
      const result = pipe.transform(1500000, 'compact');
      expect(result).toContain('R$');
      expect(result).toContain('1,5M');
    });

    it('should format thousands with k suffix', () => {
      const result = pipe.transform(5400, 'compact');
      expect(result).toContain('R$');
      expect(result).toContain('5,4k');
    });

    it('should fall back to full format for values under 1000', () => {
      const result = pipe.transform(500, 'compact');
      expect(result).toContain('R$');
      expect(result).toContain('500,00');
    });

    it('should handle negative millions', () => {
      const result = pipe.transform(-2000000, 'compact');
      expect(result).toContain('-');
      expect(result).toContain('R$');
      expect(result).toContain('2M');
    });

    it('should handle negative thousands', () => {
      const result = pipe.transform(-3500, 'compact');
      expect(result).toContain('-');
      expect(result).toContain('R$');
      expect(result).toContain('3,5k');
    });

    it('should return empty string for null in compact mode', () => {
      expect(pipe.transform(null, 'compact')).toBe('');
    });
  });
});
