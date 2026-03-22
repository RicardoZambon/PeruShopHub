import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'relativeDate',
  standalone: true,
})
export class RelativeDatePipe implements PipeTransform {
  transform(value: Date | string | null | undefined): string {
    if (value == null) return '';

    const date = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(date.getTime())) return '';

    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);
    const diffWeeks = Math.floor(diffDays / 7);

    const SEVEN_DAYS = 7;

    if (diffDays < SEVEN_DAYS) {
      if (diffMinutes < 1) return 'agora';
      if (diffMinutes < 60) return `ha ${diffMinutes}min`;
      if (diffHours < 24) return `ha ${diffHours}h`;
      if (diffDays === 1) return 'ha 1d';
      return `ha ${diffDays}d`;
    }

    if (diffDays < 30) {
      return `ha ${diffWeeks}sem`;
    }

    return date.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}
