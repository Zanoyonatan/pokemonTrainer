import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TopNav } from './top-nav';
@Component({ selector: 'app-app-shell', imports: [RouterOutlet, TopNav], templateUrl: './app-shell.html', styleUrl: './app-shell.scss' })
export class AppShell {}
