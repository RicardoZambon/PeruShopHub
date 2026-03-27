/**
 * Shared status-to-badge-variant mappings.
 * Eliminates duplicated getStatusVariant() switch statements across components.
 */
import type { BadgeVariant } from '../components/badge/badge.component';

/** Order/sale statuses: Pago, Enviado, Entregue, Cancelado, Devolvido */
const ORDER_STATUS_MAP: Record<string, BadgeVariant> = {
  'Pago': 'primary',
  'Enviado': 'warning',
  'Entregue': 'success',
  'Cancelado': 'danger',
  'Devolvido': 'neutral',
};

/** Purchase order statuses: Rascunho, Recebido, Cancelado */
const PO_STATUS_MAP: Record<string, BadgeVariant> = {
  'Rascunho': 'neutral',
  'Recebido': 'success',
  'Cancelado': 'danger',
};

/** Product statuses: Ativo, Pausado, Encerrado */
const PRODUCT_STATUS_MAP: Record<string, BadgeVariant> = {
  'Ativo': 'success',
  'Pausado': 'warning',
  'Encerrado': 'danger',
};

export function getOrderStatusVariant(status: string): BadgeVariant {
  return ORDER_STATUS_MAP[status] ?? 'neutral';
}

export function getPurchaseOrderStatusVariant(status: string): BadgeVariant {
  return PO_STATUS_MAP[status] ?? 'neutral';
}

export function getProductStatusVariant(status: string): BadgeVariant {
  return PRODUCT_STATUS_MAP[status] ?? 'neutral';
}
