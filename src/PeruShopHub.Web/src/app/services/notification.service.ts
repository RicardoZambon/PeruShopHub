import { Injectable, signal, computed } from '@angular/core';

export interface Notification {
  id: string;
  type: 'sale' | 'question' | 'stock' | 'margin' | 'connection';
  title: string;
  description: string;
  timestamp: Date;
  read: boolean;
  navigationTarget?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly notifications = signal<Notification[]>(MOCK_NOTIFICATIONS);
  readonly unreadCount = computed(() => this.notifications().filter(n => !n.read).length);

  markAsRead(id: string): void {
    this.notifications.update(list =>
      list.map(n => n.id === id ? { ...n, read: true } : n)
    );
  }

  markAllAsRead(): void {
    this.notifications.update(list =>
      list.map(n => ({ ...n, read: true }))
    );
  }
}

const now = new Date();

function hoursAgo(h: number): Date {
  return new Date(now.getTime() - h * 60 * 60 * 1000);
}

const MOCK_NOTIFICATIONS: Notification[] = [
  {
    id: 'n1',
    type: 'sale',
    title: 'Nova venda realizada',
    description: 'Pedido #2087654328 - Fone Bluetooth JBL Tune 510BT - R$ 219,90',
    timestamp: hoursAgo(0.5),
    read: false,
    navigationTarget: '/vendas/2087654328',
  },
  {
    id: 'n2',
    type: 'question',
    title: 'Nova pergunta recebida',
    description: '"Esse produto vem com carregador incluso?" - Produto: Carregador USB-C 20W',
    timestamp: hoursAgo(1.2),
    read: false,
    navigationTarget: '/perguntas',
  },
  {
    id: 'n3',
    type: 'stock',
    title: 'Alerta de estoque baixo',
    description: 'Capa iPhone 15 Pro Max Silicone - Apenas 3 unidades restantes',
    timestamp: hoursAgo(2),
    read: false,
    navigationTarget: '/estoque',
  },
  {
    id: 'n4',
    type: 'margin',
    title: 'Alerta de margem baixa',
    description: 'Película Vidro Samsung Galaxy S24 - Margem caiu para 4.2%',
    timestamp: hoursAgo(3.5),
    read: false,
    navigationTarget: '/financeiro',
  },
  {
    id: 'n5',
    type: 'sale',
    title: 'Nova venda realizada',
    description: 'Pedido #2087654325 - Kit Cabos USB-C (3 unidades) - R$ 49,90',
    timestamp: hoursAgo(5),
    read: false,
    navigationTarget: '/vendas/2087654325',
  },
  {
    id: 'n6',
    type: 'connection',
    title: 'Erro de conexão resolvido',
    description: 'Conexão com Mercado Livre restabelecida após 15 minutos',
    timestamp: hoursAgo(8),
    read: true,
    navigationTarget: '/configuracoes',
  },
  {
    id: 'n7',
    type: 'question',
    title: 'Nova pergunta recebida',
    description: '"Qual o prazo de entrega para SP capital?" - Produto: Mousepad Gamer RGB',
    timestamp: hoursAgo(12),
    read: true,
    navigationTarget: '/perguntas',
  },
  {
    id: 'n8',
    type: 'stock',
    title: 'Alerta de estoque baixo',
    description: 'Suporte Notebook Alumínio Ajustável - Apenas 2 unidades restantes',
    timestamp: hoursAgo(24),
    read: true,
    navigationTarget: '/estoque',
  },
];
