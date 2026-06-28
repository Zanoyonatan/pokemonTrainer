import { Component, EventEmitter, Input, Output } from '@angular/core';
@Component({ selector: 'app-empty-state', templateUrl: './empty-state.html', styleUrl: './empty-state.scss' })
export class EmptyState { @Input() title = 'No data found'; @Input() message = 'There is nothing to show yet.'; @Input() actionText?: string; @Output() actionClick = new EventEmitter<void>(); }
