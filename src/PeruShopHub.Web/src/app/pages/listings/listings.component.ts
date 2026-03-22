import { Component } from '@angular/core';
import { LucideAngularModule, Megaphone } from 'lucide-angular';

@Component({
  selector: 'app-listings',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './listings.component.html',
  styleUrl: './listings.component.scss',
})
export class ListingsComponent {
  readonly megaphoneIcon = Megaphone;
}
