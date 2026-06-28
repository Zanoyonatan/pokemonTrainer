import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { TopNav } from './top-nav';

@Component({
  selector: 'app-app-shell',
  imports: [RouterOutlet, TopNav],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss'
})
export class AppShell implements OnInit {
  private readonly authService = inject(AuthService);

  ngOnInit(): void {
    this.authService.me().subscribe({
      next: () => {},
      error: () => {}
    });
  }
}
