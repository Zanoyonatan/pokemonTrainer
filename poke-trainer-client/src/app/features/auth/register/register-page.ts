import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { getUserFriendlyErrorMessage } from '../../../core/errors/user-friendly-error-message';
import { PokeballLoader } from '../../../shared/components/pokeball-loader';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, PokeballLoader],
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss'
})
export class RegisterPage {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });

  get passwordsMismatch(): boolean {
    const value = this.form.getRawValue();
    return !!value.password && !!value.confirmPassword && value.password !== value.confirmPassword;
  }

  submit(): void {
    this.errorMessage.set(null);

    if (this.form.invalid || this.passwordsMismatch) {
      this.form.markAllAsTouched();
      return;
    }

    const { confirmPassword, ...request } = this.form.getRawValue();

    this.isLoading.set(true);

    this.authService.register(request).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/app/dashboard']);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(getUserFriendlyErrorMessage(error, 'We could not create your account. Please try again.'));
      }
    });
  }
}