import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-venda-detail',
  standalone: true,
  template: `<h1>Pedido #{{ orderId }}</h1>`,
})
export class VendaDetailComponent {
  orderId = '';

  constructor(private route: ActivatedRoute) {
    this.orderId = this.route.snapshot.paramMap.get('id') ?? '';
  }
}
