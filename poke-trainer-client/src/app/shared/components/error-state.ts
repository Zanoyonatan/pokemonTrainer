import { Component, EventEmitter, Input, Output } from '@angular/core';
@Component({ selector: 'app-error-state', templateUrl: './error-state.html', styleUrl: './error-state.scss' })
export class ErrorState { @Input() title = 'Something went wrong'; @Input() message = 'Please try again later.'; @Input() retryText = 'Retry'; @Output() retry = new EventEmitter<void>(); }
