/**
 * Shared formatting utilities.
 * Use these instead of duplicating formatBrl/formatDate in every component.
 */

/** Format a number as BRL currency: R$ 1.234,56 */
export function formatBrl(value: number | null | undefined): string {
  if (value == null) return '';
  return value.toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

/**
 * Format a date string for display.
 * @param dateStr ISO date string
 * @param includeTime Whether to include hours/minutes (default: false)
 */
export function formatDate(dateStr: string | null | undefined, includeTime = false): string {
  if (!dateStr) return '-';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '-';

  const options: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  };

  if (includeTime) {
    options.hour = '2-digit';
    options.minute = '2-digit';
  }

  return date.toLocaleDateString('pt-BR', options);
}

/** Format a date string with short month name: 22 mar. 2026 */
export function formatDateShort(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr.includes('T') ? dateStr : dateStr + 'T00:00:00');
  if (isNaN(d.getTime())) return '-';
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
}
