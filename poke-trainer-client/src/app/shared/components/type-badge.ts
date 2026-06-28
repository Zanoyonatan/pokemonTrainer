import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-type-badge',
  templateUrl: './type-badge.html',
  styleUrl: './type-badge.scss'
})
export class TypeBadge {
  @Input({ required: true }) type!: string;

  get typeClass(): string {
    return `type-${this.type.toLowerCase()}`;
  }
}
