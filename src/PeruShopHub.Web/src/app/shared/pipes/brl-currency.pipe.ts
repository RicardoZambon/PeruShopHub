import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'brlCurrency',
  standalone: true,
})
export class BrlCurrencyPipe implements PipeTransform {
  transform(value: number | null | undefined, mode: 'full' | 'compact' = 'full'): string {
    if (value == null) return '';

    if (mode === 'compact') {
      return this.formatCompact(value);
    }

    return value.toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }

  private formatCompact(value: number): string {
    const abs = Math.abs(value);
    const sign = value < 0 ? '-' : '';

    if (abs >= 1_000_000) {
      return `${sign}R$ ${(abs / 1_000_000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}M`;
    }
    if (abs >= 1_000) {
      return `${sign}R$ ${(abs / 1_000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}k`;
    }
    return this.transform(value, 'full');
  }
}
